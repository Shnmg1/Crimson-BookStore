using System.Data;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using CrimsonBookStore.Models;

namespace CrimsonBookStore.Services;

public class OrderService : IOrderService
{
    private readonly IDatabaseService _databaseService;
    private readonly string _connectionString;

    public OrderService(IDatabaseService databaseService, IConfiguration configuration)
    {
        _databaseService = databaseService;
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    public async Task<OrderCreateResponse> CreateOrderAsync(int userId, int? paymentMethodId)
    {
        // First, validate cart is not empty and all items are still available
        var cart = await _databaseService.ExecuteQueryAsync(
            @"SELECT sc.BookID, b.SellingPrice, b.Status, b.Title
              FROM ShoppingCart sc
              JOIN Book b ON sc.BookID = b.BookID
              WHERE sc.UserID = @UserID",
            new Dictionary<string, object> { { "@UserID", userId } }
        );

        if (cart.Rows.Count == 0)
        {
            throw new InvalidOperationException("Cart is empty");
        }

        // Check if all items are still available
        var unavailableItems = cart.Rows.Cast<DataRow>()
            .Where(row => row["Status"].ToString() != "Available")
            .ToList();

        if (unavailableItems.Count > 0)
        {
            throw new InvalidOperationException("Some items in your cart are no longer available");
        }

        // Start transaction
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // 1. Create PurchaseOrder record
            var createOrderQuery = @"
                INSERT INTO PurchaseOrder (UserID, Status, TotalAmount, OrderDate)
                VALUES (@UserID, 'New', 0, CURRENT_TIMESTAMP)";

            await using var createOrderCmd = new MySqlCommand(createOrderQuery, connection, transaction);
            createOrderCmd.Parameters.AddWithValue("@UserID", userId);
            await createOrderCmd.ExecuteNonQueryAsync();

            // Get the OrderID that was just inserted
            var orderIdQuery = "SELECT LAST_INSERT_ID()";
            await using var orderIdCmd = new MySqlCommand(orderIdQuery, connection, transaction);
            var orderIdObj = await orderIdCmd.ExecuteScalarAsync();
            var orderId = Convert.ToInt32(orderIdObj);

            // 2. Create OrderLineItems and update Book statuses
            var createLineItemsQuery = @"
                INSERT INTO OrderLineItem (OrderID, BookID, PriceAtSale)
                SELECT @OrderID, sc.BookID, b.SellingPrice
                FROM ShoppingCart sc
                JOIN Book b ON sc.BookID = b.BookID
                WHERE sc.UserID = @UserID AND b.Status = 'Available'";

            await using var lineItemsCmd = new MySqlCommand(createLineItemsQuery, connection, transaction);
            lineItemsCmd.Parameters.AddWithValue("@OrderID", orderId);
            lineItemsCmd.Parameters.AddWithValue("@UserID", userId);
            await lineItemsCmd.ExecuteNonQueryAsync();

            // 3. Update Book.Status = 'Sold' for all books in the order
            var updateBooksQuery = @"
                UPDATE Book
                SET Status = 'Sold'
                WHERE BookID IN (
                    SELECT BookID FROM ShoppingCart WHERE UserID = @UserID
                )";

            await using var updateBooksCmd = new MySqlCommand(updateBooksQuery, connection, transaction);
            updateBooksCmd.Parameters.AddWithValue("@UserID", userId);
            await updateBooksCmd.ExecuteNonQueryAsync();

            // 4. Calculate and update TotalAmount
            var updateTotalQuery = @"
                UPDATE PurchaseOrder
                SET TotalAmount = (
                    SELECT COALESCE(SUM(PriceAtSale), 0)
                    FROM OrderLineItem
                    WHERE OrderID = @OrderID
                )
                WHERE OrderID = @OrderID";

            await using var updateTotalCmd = new MySqlCommand(updateTotalQuery, connection, transaction);
            updateTotalCmd.Parameters.AddWithValue("@OrderID", orderId);
            await updateTotalCmd.ExecuteNonQueryAsync();

            // Get the total amount
            var getTotalQuery = "SELECT TotalAmount FROM PurchaseOrder WHERE OrderID = @OrderID";
            await using var getTotalCmd = new MySqlCommand(getTotalQuery, connection, transaction);
            getTotalCmd.Parameters.AddWithValue("@OrderID", orderId);
            var totalAmountObj = await getTotalCmd.ExecuteScalarAsync();
            var totalAmount = Convert.ToDecimal(totalAmountObj);

            // 5. Create Payment record
            var createPaymentQuery = @"
                INSERT INTO Payment (OrderID, PaymentMethodID, Amount, PaymentStatus, PaymentDate)
                VALUES (@OrderID, @PaymentMethodID, @Amount, 'Completed', CURRENT_TIMESTAMP)";

            await using var paymentCmd = new MySqlCommand(createPaymentQuery, connection, transaction);
            paymentCmd.Parameters.AddWithValue("@OrderID", orderId);
            paymentCmd.Parameters.AddWithValue("@PaymentMethodID", paymentMethodId.HasValue ? (object)paymentMethodId.Value : DBNull.Value);
            paymentCmd.Parameters.AddWithValue("@Amount", totalAmount);
            await paymentCmd.ExecuteNonQueryAsync();

            // 6. Clear ShoppingCart
            var clearCartQuery = "DELETE FROM ShoppingCart WHERE UserID = @UserID";
            await using var clearCartCmd = new MySqlCommand(clearCartQuery, connection, transaction);
            clearCartCmd.Parameters.AddWithValue("@UserID", userId);
            await clearCartCmd.ExecuteNonQueryAsync();

            // Commit transaction
            await transaction.CommitAsync();

            // Get order details for response
            var orderDateQuery = "SELECT OrderDate FROM PurchaseOrder WHERE OrderID = @OrderID";
            var orderDateResult = await _databaseService.ExecuteQueryAsync(
                orderDateQuery,
                new Dictionary<string, object> { { "@OrderID", orderId } }
            );
            var orderDate = Convert.ToDateTime(orderDateResult.Rows[0]["OrderDate"]);

            // Get order items for response
            var itemsQuery = @"
                SELECT oli.BookID, oli.PriceAtSale, b.Title
                FROM OrderLineItem oli
                JOIN Book b ON oli.BookID = b.BookID
                WHERE oli.OrderID = @OrderID";

            var itemsResult = await _databaseService.ExecuteQueryAsync(
                itemsQuery,
                new Dictionary<string, object> { { "@OrderID", orderId } }
            );

            var items = new List<OrderItemSummary>();
            foreach (DataRow row in itemsResult.Rows)
            {
                items.Add(new OrderItemSummary
                {
                    BookId = Convert.ToInt32(row["BookID"]),
                    Title = row["Title"].ToString() ?? string.Empty,
                    PriceAtSale = Convert.ToDecimal(row["PriceAtSale"])
                });
            }

            return new OrderCreateResponse
            {
                OrderId = orderId,
                OrderDate = orderDate,
                Status = "New",
                TotalAmount = totalAmount,
                Items = items
            };
        }
        catch
        {
            // Rollback transaction on error
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<OrderListResponse>> GetUserOrdersAsync(int userId, string? status = null)
    {
        var query = @"
            SELECT 
                po.OrderID,
                po.OrderDate,
                po.Status,
                po.TotalAmount,
                COUNT(oli.LineItemID) as item_count
            FROM PurchaseOrder po
            LEFT JOIN OrderLineItem oli ON po.OrderID = oli.OrderID
            WHERE po.UserID = @UserID";

        var parameters = new Dictionary<string, object>
        {
            { "@UserID", userId }
        };

        if (!string.IsNullOrWhiteSpace(status))
        {
            query += " AND po.Status = @Status";
            parameters.Add("@Status", status);
        }

        query += @"
            GROUP BY po.OrderID, po.OrderDate, po.Status, po.TotalAmount
            ORDER BY po.OrderDate DESC";

        var dataTable = await _databaseService.ExecuteQueryAsync(query, parameters);

        var orders = new List<OrderListResponse>();

        foreach (DataRow row in dataTable.Rows)
        {
            orders.Add(new OrderListResponse
            {
                OrderId = Convert.ToInt32(row["OrderID"]),
                OrderDate = Convert.ToDateTime(row["OrderDate"]),
                Status = row["Status"].ToString() ?? string.Empty,
                TotalAmount = Convert.ToDecimal(row["TotalAmount"]),
                ItemCount = Convert.ToInt32(row["item_count"])
            });
        }

        return orders;
    }

    public async Task<OrderResponse?> GetOrderDetailsAsync(int orderId, int userId)
    {
        // First verify the order belongs to the user
        var verifyQuery = @"
            SELECT OrderID, UserID
            FROM PurchaseOrder
            WHERE OrderID = @OrderID";

        var verifyResult = await _databaseService.ExecuteQueryAsync(
            verifyQuery,
            new Dictionary<string, object> { { "@OrderID", orderId } }
        );

        if (verifyResult.Rows.Count == 0)
        {
            return null; // Order not found
        }

        var orderUserId = Convert.ToInt32(verifyResult.Rows[0]["UserID"]);
        if (orderUserId != userId)
        {
            throw new UnauthorizedAccessException("Order does not belong to you");
        }

        // Get order details
        var orderQuery = @"
            SELECT OrderID, OrderDate, Status, TotalAmount
            FROM PurchaseOrder
            WHERE OrderID = @OrderID";

        var orderResult = await _databaseService.ExecuteQueryAsync(
            orderQuery,
            new Dictionary<string, object> { { "@OrderID", orderId } }
        );

        if (orderResult.Rows.Count == 0)
        {
            return null;
        }

        var orderRow = orderResult.Rows[0];

        // Get order line items with book details
        var itemsQuery = @"
            SELECT 
                oli.LineItemID,
                oli.BookID,
                b.ISBN,
                b.Title,
                b.Author,
                b.Edition,
                oli.PriceAtSale
            FROM OrderLineItem oli
            JOIN Book b ON oli.BookID = b.BookID
            WHERE oli.OrderID = @OrderID";

        var itemsResult = await _databaseService.ExecuteQueryAsync(
            itemsQuery,
            new Dictionary<string, object> { { "@OrderID", orderId } }
        );

        var items = new List<OrderLineItemResponse>();
        foreach (DataRow row in itemsResult.Rows)
        {
            items.Add(new OrderLineItemResponse
            {
                LineItemId = Convert.ToInt32(row["LineItemID"]),
                BookId = Convert.ToInt32(row["BookID"]),
                ISBN = row["ISBN"].ToString() ?? string.Empty,
                Title = row["Title"].ToString() ?? string.Empty,
                Author = row["Author"].ToString() ?? string.Empty,
                Edition = row["Edition"].ToString() ?? string.Empty,
                PriceAtSale = Convert.ToDecimal(row["PriceAtSale"])
            });
        }

        // Get payment information
        var paymentQuery = @"
            SELECT 
                p.PaymentID,
                p.PaymentDate,
                p.Amount,
                p.PaymentStatus,
                pm.CardType,
                pm.LastFourDigits
            FROM Payment p
            LEFT JOIN PaymentMethod pm ON p.PaymentMethodID = pm.PaymentMethodID
            WHERE p.OrderID = @OrderID";

        var paymentResult = await _databaseService.ExecuteQueryAsync(
            paymentQuery,
            new Dictionary<string, object> { { "@OrderID", orderId } }
        );

        PaymentResponse? payment = null;
        if (paymentResult.Rows.Count > 0)
        {
            var paymentRow = paymentResult.Rows[0];
            var paymentMethod = paymentRow["CardType"] != DBNull.Value && paymentRow["LastFourDigits"] != DBNull.Value
                ? $"{paymentRow["CardType"]} ending in {paymentRow["LastFourDigits"]}"
                : null;

            payment = new PaymentResponse
            {
                PaymentId = Convert.ToInt32(paymentRow["PaymentID"]),
                PaymentDate = Convert.ToDateTime(paymentRow["PaymentDate"]),
                Amount = Convert.ToDecimal(paymentRow["Amount"]),
                PaymentStatus = paymentRow["PaymentStatus"].ToString() ?? string.Empty,
                PaymentMethod = paymentMethod
            };
        }

        return new OrderResponse
        {
            OrderId = Convert.ToInt32(orderRow["OrderID"]),
            OrderDate = Convert.ToDateTime(orderRow["OrderDate"]),
            Status = orderRow["Status"].ToString() ?? string.Empty,
            TotalAmount = Convert.ToDecimal(orderRow["TotalAmount"]),
            Items = items,
            Payment = payment
        };
    }
}

