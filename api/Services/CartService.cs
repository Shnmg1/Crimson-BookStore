using System.Data;
using CrimsonBookStore.Models;

namespace CrimsonBookStore.Services;

public class CartService : ICartService
{
    private readonly IDatabaseService _databaseService;

    public CartService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<CartResponse> GetCartAsync(int userId)
    {
        var query = @"
            SELECT 
                sc.CartItemID,
                b.BookID,
                b.ISBN,
                b.Title,
                b.Author,
                b.Edition,
                b.SellingPrice,
                b.BookCondition,
                b.CourseMajor,
                sc.AddedDate
            FROM ShoppingCart sc
            JOIN Book b ON sc.BookID = b.BookID
            WHERE sc.UserID = @UserID AND b.Status = 'Available'
            ORDER BY sc.AddedDate DESC";

        var parameters = new Dictionary<string, object>
        {
            { "@UserID", userId }
        };

        var dataTable = await _databaseService.ExecuteQueryAsync(query, parameters);

        var items = new List<CartItem>();
        decimal total = 0;

        foreach (DataRow row in dataTable.Rows)
        {
            var item = new CartItem
            {
                CartItemId = Convert.ToInt32(row["CartItemID"]),
                BookId = Convert.ToInt32(row["BookID"]),
                ISBN = row["ISBN"].ToString() ?? string.Empty,
                Title = row["Title"].ToString() ?? string.Empty,
                Author = row["Author"].ToString() ?? string.Empty,
                Edition = row["Edition"].ToString() ?? string.Empty,
                SellingPrice = Convert.ToDecimal(row["SellingPrice"]),
                BookCondition = row["BookCondition"].ToString() ?? string.Empty,
                CourseMajor = row["CourseMajor"] == DBNull.Value ? null : row["CourseMajor"].ToString(),
                AddedDate = Convert.ToDateTime(row["AddedDate"])
            };

            items.Add(item);
            total += item.SellingPrice;
        }

        return new CartResponse
        {
            Items = items,
            Total = total
        };
    }

    public async Task<int> AddToCartAsync(int userId, int bookId)
    {
        // First, validate that the book exists and is available
        var validateQuery = @"
            SELECT BookID, Status
            FROM Book
            WHERE BookID = @BookID";

        var validateParams = new Dictionary<string, object>
        {
            { "@BookID", bookId }
        };

        var validateResult = await _databaseService.ExecuteQueryAsync(validateQuery, validateParams);

        if (validateResult.Rows.Count == 0)
        {
            throw new KeyNotFoundException("Book not found");
        }

        var status = validateResult.Rows[0]["Status"].ToString();
        if (status != "Available")
        {
            throw new InvalidOperationException("Book not available");
        }

        // Check if item is already in cart
        var checkCartQuery = @"
            SELECT CartItemID
            FROM ShoppingCart
            WHERE UserID = @UserID AND BookID = @BookID";

        var checkParams = new Dictionary<string, object>
        {
            { "@UserID", userId },
            { "@BookID", bookId }
        };

        var existingItem = await _databaseService.ExecuteQueryAsync(checkCartQuery, checkParams);

        if (existingItem.Rows.Count > 0)
        {
            // Item already in cart
            throw new InvalidOperationException("Book already in cart");
        }

        // Insert new cart item
        var insertQuery = @"
            INSERT INTO ShoppingCart (UserID, BookID, AddedDate)
            VALUES (@UserID, @BookID, CURRENT_TIMESTAMP)";

        await _databaseService.ExecuteNonQueryAsync(insertQuery, checkParams);

        // Get the CartItemID that was just inserted
        var getCartItemIdQuery = @"
            SELECT CartItemID
            FROM ShoppingCart
            WHERE UserID = @UserID AND BookID = @BookID";

        var cartItemResult = await _databaseService.ExecuteQueryAsync(getCartItemIdQuery, checkParams);
        
        if (cartItemResult.Rows.Count > 0)
        {
            return Convert.ToInt32(cartItemResult.Rows[0]["CartItemID"]);
        }

        throw new InvalidOperationException("Failed to add book to cart");
    }

    public async Task<bool> RemoveFromCartAsync(int userId, int cartItemId)
    {
        var query = @"
            DELETE FROM ShoppingCart
            WHERE CartItemID = @CartItemID AND UserID = @UserID";

        var parameters = new Dictionary<string, object>
        {
            { "@CartItemID", cartItemId },
            { "@UserID", userId }
        };

        var rowsAffected = await _databaseService.ExecuteNonQueryAsync(query, parameters);
        return rowsAffected > 0;
    }

    public async Task<bool> ClearCartAsync(int userId)
    {
        var query = @"
            DELETE FROM ShoppingCart
            WHERE UserID = @UserID";

        var parameters = new Dictionary<string, object>
        {
            { "@UserID", userId }
        };

        await _databaseService.ExecuteNonQueryAsync(query, parameters);
        return true; // Always return true for clear operation
    }
}

