using Microsoft.AspNetCore.Mvc;
using CrimsonBookStore.Services;
using System.Data;

namespace CrimsonBookStore.Controllers;

[ApiController]
[Route("api/admin/queries")]
public class ManagementQueriesController : ControllerBase
{
    private readonly IDatabaseService _databaseService;
    private readonly IConfiguration _configuration;

    public ManagementQueriesController(IDatabaseService databaseService, IConfiguration configuration)
    {
        _databaseService = databaseService;
        _configuration = configuration;
    }

    [HttpGet("list")]
    public IActionResult GetQueryList()
    {
        // Return list of available queries with their descriptions
        var queries = new[]
        {
            new { id = "low_stock", name = "Low Stock Alert", category = "Inventory", description = "Books with low stock that have been sold in the past 6 months" },
            new { id = "inventory_valuation", name = "Inventory Valuation", category = "Inventory", description = "Total dollar value of current inventory" },
            new { id = "condition_breakdown", name = "Condition Breakdown", category = "Inventory", description = "Breakdown of books by condition (New, Good, Fair)" },
            new { id = "high_stock", name = "High Stock Levels", category = "Inventory", description = "ISBN+Edition combinations with highest stock levels" },
            new { id = "monthly_sales", name = "Monthly Sales Revenue", category = "Sales", description = "Monthly sales revenue for the last 6 months" },
            new { id = "top_selling", name = "Top Selling Books", category = "Sales", description = "Top-selling books by quantity sold in the last 3 months" },
            new { id = "avg_order_value", name = "Average Order Value", category = "Sales", description = "Average order value across all completed orders" },
            new { id = "loyal_customers", name = "Loyal Customers", category = "Sales", description = "Customers who have placed 3 or more orders" },
            new { id = "order_status", name = "Order Status Breakdown", category = "Sales", description = "Distribution of orders across different statuses" },
            new { id = "pending_submissions", name = "Pending Submissions", category = "Acquisition", description = "Count and average asking price of pending sell submissions" },
            new { id = "top_sellers", name = "Top Submitting Customers", category = "Acquisition", description = "Customers who have submitted the most books for sale" },
            new { id = "submission_status", name = "Submission Status Breakdown", category = "Acquisition", description = "Sell submission status breakdown and approval rate" },
            new { id = "admin_approvals", name = "Admin Approval Activity", category = "Acquisition", description = "Which admin has approved the most sell submissions" },
            new { id = "negotiation_rounds", name = "Average Negotiation Rounds", category = "Acquisition", description = "Average negotiation rounds per submission" },
            new { id = "new_users", name = "New User Registrations", category = "Users", description = "New users registered in the last 30 days" },
            new { id = "inactive_customers", name = "Inactive Customers", category = "Users", description = "Customers with no orders in the last 6 months" },
            new { id = "customer_lifetime_value", name = "Customer Lifetime Value", category = "Users", description = "Customer lifetime value analysis" },
            new { id = "registration_trend", name = "Registration Trend", category = "Users", description = "Daily registration trend for the last 30 days" },
            new { id = "slow_movers", name = "Slow Moving Books", category = "Inventory", description = "Books that have been in inventory the longest without selling" },
            new { id = "restock_verification", name = "Restock Verification", category = "Inventory", description = "Verify that books from cancelled orders have been restocked" }
        };

        return Ok(new { success = true, data = queries });
    }

    [HttpPost("execute")]
    public async Task<IActionResult> ExecuteQuery([FromBody] ExecuteQueryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.QueryId))
        {
            return BadRequest(new { success = false, error = "Query ID is required" });
        }

        try
        {
            var query = GetQuerySql(request.QueryId);
            if (string.IsNullOrEmpty(query))
            {
                return NotFound(new { success = false, error = "Query not found" });
            }

            var result = await _databaseService.ExecuteQueryAsync(query, null);
            
            // Convert DataTable to list of dictionaries for JSON serialization
            var rows = new List<Dictionary<string, object>>();
            foreach (DataRow row in result.Rows)
            {
                var dict = new Dictionary<string, object>();
                foreach (DataColumn col in result.Columns)
                {
                    dict[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
                }
                rows.Add(dict);
            }

            return Ok(new { success = true, data = rows, columns = result.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray() });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    private string GetQuerySql(string queryId)
    {
        return queryId switch
        {
            "low_stock" => @"
                SELECT 
                    b.ISBN,
                    b.Title,
                    b.Author,
                    b.Edition,
                    COUNT(CASE WHEN b2.Status = 'Available' THEN 1 END) AS CurrentStock,
                    COUNT(DISTINCT oli.OrderID) AS OrdersInLast6Months,
                    SUM(oli.PriceAtSale) AS RevenueFromSales
                FROM Book b
                LEFT JOIN Book b2 ON b.ISBN = b2.ISBN AND b.Edition = b2.Edition
                LEFT JOIN OrderLineItem oli ON b.BookID = oli.BookID
                LEFT JOIN PurchaseOrder po ON oli.OrderID = po.OrderID
                    AND po.OrderDate >= DATE_SUB(NOW(), INTERVAL 6 MONTH)
                    AND po.Status = 'Fulfilled'
                WHERE b.Status = 'Available'
                GROUP BY b.ISBN, b.Title, b.Author, b.Edition
                HAVING CurrentStock < 1
                ORDER BY OrdersInLast6Months DESC, RevenueFromSales DESC",
            
            "inventory_valuation" => @"
                SELECT 
                    COUNT(*) AS TotalBooksInStock,
                    SUM(SellingPrice) AS TotalInventoryValue,
                    SUM(AcquisitionCost) AS TotalAcquisitionCost,
                    SUM(SellingPrice - AcquisitionCost) AS TotalPotentialProfit,
                    AVG(SellingPrice) AS AverageSellingPrice,
                    AVG(AcquisitionCost) AS AverageAcquisitionCost
                FROM Book
                WHERE Status = 'Available'",
            
            "condition_breakdown" => @"
                SELECT 
                    BookCondition,
                    COUNT(*) AS BookCount,
                    SUM(SellingPrice) AS TotalValue,
                    AVG(SellingPrice) AS AverageSellingPrice,
                    AVG(AcquisitionCost) AS AverageAcquisitionCost,
                    AVG(SellingPrice - AcquisitionCost) AS AverageProfitMargin,
                    ROUND(COUNT(*) * 100.0 / (SELECT COUNT(*) FROM Book WHERE Status = 'Available'), 2) AS PercentageOfInventory
                FROM Book
                WHERE Status = 'Available'
                GROUP BY BookCondition
                ORDER BY BookCount DESC",
            
            "high_stock" => @"
                SELECT 
                    ISBN,
                    Title,
                    Author,
                    Edition,
                    COUNT(*) AS StockQuantity,
                    AVG(SellingPrice) AS AveragePrice,
                    SUM(SellingPrice) AS TotalInventoryValue,
                    CourseMajor
                FROM Book
                WHERE Status = 'Available'
                GROUP BY ISBN, Title, Author, Edition, CourseMajor
                HAVING StockQuantity > 1
                ORDER BY StockQuantity DESC, TotalInventoryValue DESC
                LIMIT 20",
            
            "monthly_sales" => @"
                SELECT 
                    DATE_FORMAT(po.OrderDate, '%Y-%m') AS Month,
                    COUNT(DISTINCT po.OrderID) AS TotalOrders,
                    COUNT(oli.LineItemID) AS TotalBooksSold,
                    SUM(po.TotalAmount) AS TotalRevenue,
                    AVG(po.TotalAmount) AS AverageOrderValue,
                    SUM(oli.PriceAtSale - b.AcquisitionCost) AS TotalProfit
                FROM PurchaseOrder po
                INNER JOIN OrderLineItem oli ON po.OrderID = oli.OrderID
                INNER JOIN Book b ON oli.BookID = b.BookID
                WHERE po.Status = 'Fulfilled'
                    AND po.OrderDate >= DATE_SUB(NOW(), INTERVAL 6 MONTH)
                GROUP BY DATE_FORMAT(po.OrderDate, '%Y-%m')
                ORDER BY Month DESC",
            
            "top_selling" => @"
                SELECT 
                    b.ISBN,
                    b.Title,
                    b.Author,
                    b.Edition,
                    COUNT(oli.LineItemID) AS BooksSold,
                    SUM(oli.PriceAtSale) AS TotalRevenue,
                    AVG(oli.PriceAtSale) AS AverageSalePrice,
                    SUM(oli.PriceAtSale - b.AcquisitionCost) AS TotalProfit
                FROM Book b
                INNER JOIN OrderLineItem oli ON b.BookID = oli.BookID
                INNER JOIN PurchaseOrder po ON oli.OrderID = po.OrderID
                WHERE po.Status = 'Fulfilled'
                    AND po.OrderDate >= DATE_SUB(NOW(), INTERVAL 3 MONTH)
                GROUP BY b.ISBN, b.Title, b.Author, b.Edition
                ORDER BY BooksSold DESC, TotalRevenue DESC
                LIMIT 20",
            
            "avg_order_value" => @"
                SELECT 
                    COUNT(DISTINCT po.OrderID) AS TotalCompletedOrders,
                    SUM(po.TotalAmount) AS TotalRevenue,
                    AVG(po.TotalAmount) AS AverageOrderValue,
                    MIN(po.TotalAmount) AS MinimumOrderValue,
                    MAX(po.TotalAmount) AS MaximumOrderValue,
                    AVG(book_count.BookCount) AS AverageBooksPerOrder
                FROM PurchaseOrder po
                INNER JOIN (
                    SELECT OrderID, COUNT(*) AS BookCount
                    FROM OrderLineItem
                    GROUP BY OrderID
                ) book_count ON po.OrderID = book_count.OrderID
                WHERE po.Status = 'Fulfilled'",
            
            "loyal_customers" => @"
                SELECT 
                    u.UserID,
                    u.Username,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    COUNT(DISTINCT po.OrderID) AS OrderCount,
                    SUM(po.TotalAmount) AS TotalSpent,
                    AVG(po.TotalAmount) AS AverageOrderValue,
                    MAX(po.OrderDate) AS LastOrderDate,
                    MIN(po.OrderDate) AS FirstOrderDate
                FROM `User` u
                INNER JOIN PurchaseOrder po ON u.UserID = po.UserID
                WHERE po.Status = 'Fulfilled'
                GROUP BY u.UserID, u.Username, u.Email, u.FirstName, u.LastName
                HAVING OrderCount >= 3
                ORDER BY OrderCount DESC, TotalSpent DESC",
            
            "order_status" => @"
                SELECT 
                    po.Status,
                    COUNT(*) AS OrderCount,
                    SUM(po.TotalAmount) AS TotalValue,
                    AVG(po.TotalAmount) AS AverageOrderValue,
                    ROUND(COUNT(*) * 100.0 / (SELECT COUNT(*) FROM PurchaseOrder), 2) AS PercentageOfAllOrders
                FROM PurchaseOrder po
                GROUP BY po.Status
                ORDER BY OrderCount DESC",
            
            "pending_submissions" => @"
                SELECT 
                    COUNT(*) AS PendingSubmissionsCount,
                    AVG(AskingPrice) AS AverageAskingPrice,
                    MIN(AskingPrice) AS MinimumAskingPrice,
                    MAX(AskingPrice) AS MaximumAskingPrice,
                    SUM(AskingPrice) AS TotalPotentialCost,
                    AVG(DATEDIFF(NOW(), SubmissionDate)) AS AverageDaysPending
                FROM SellSubmission
                WHERE Status = 'Pending Review'",
            
            "top_sellers" => @"
                SELECT 
                    u.UserID,
                    u.Username,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    COUNT(ss.SubmissionID) AS TotalSubmissions,
                    SUM(CASE WHEN ss.Status = 'Approved' THEN 1 ELSE 0 END) AS ApprovedSubmissions,
                    SUM(CASE WHEN ss.Status = 'Rejected' THEN 1 ELSE 0 END) AS RejectedSubmissions,
                    SUM(CASE WHEN ss.Status = 'Pending Review' THEN 1 ELSE 0 END) AS PendingSubmissions,
                    AVG(ss.AskingPrice) AS AverageAskingPrice,
                    SUM(ss.AskingPrice) AS TotalAskingPrice
                FROM `User` u
                INNER JOIN SellSubmission ss ON u.UserID = ss.UserID
                GROUP BY u.UserID, u.Username, u.Email, u.FirstName, u.LastName
                HAVING TotalSubmissions > 0
                ORDER BY TotalSubmissions DESC, ApprovedSubmissions DESC
                LIMIT 20",
            
            "submission_status" => @"
                SELECT 
                    Status,
                    COUNT(*) AS SubmissionCount,
                    AVG(AskingPrice) AS AverageAskingPrice,
                    SUM(AskingPrice) AS TotalAskingPrice,
                    ROUND(COUNT(*) * 100.0 / (SELECT COUNT(*) FROM SellSubmission), 2) AS PercentageOfAllSubmissions
                FROM SellSubmission
                GROUP BY Status
                ORDER BY SubmissionCount DESC",
            
            "admin_approvals" => @"
                SELECT 
                    u.UserID,
                    u.Username,
                    u.Email,
                    COUNT(ss.SubmissionID) AS ApprovalsCount,
                    AVG(ss.AskingPrice) AS AverageAskingPrice,
                    SUM(ss.AskingPrice) AS TotalAcquisitionCost,
                    MIN(ss.SubmissionDate) AS FirstApprovalDate,
                    MAX(ss.SubmissionDate) AS LastApprovalDate
                FROM `User` u
                INNER JOIN SellSubmission ss ON u.UserID = ss.AdminUserID
                WHERE ss.Status IN ('Approved', 'Completed')
                GROUP BY u.UserID, u.Username, u.Email
                ORDER BY ApprovalsCount DESC",
            
            "negotiation_rounds" => @"
                SELECT 
                    ss.SubmissionID,
                    ss.Title,
                    ss.ISBN,
                    COUNT(pn.NegotiationID) AS NegotiationRounds,
                    AVG(pn.OfferedPrice) AS AverageOfferedPrice,
                    MAX(pn.RoundNumber) AS MaxRoundNumber
                FROM SellSubmission ss
                LEFT JOIN PriceNegotiation pn ON ss.SubmissionID = pn.SubmissionID
                GROUP BY ss.SubmissionID, ss.Title, ss.ISBN
                HAVING NegotiationRounds > 0
                ORDER BY NegotiationRounds DESC, MaxRoundNumber DESC
                LIMIT 20",
            
            "new_users" => @"
                SELECT 
                    COUNT(*) AS NewUsersCount,
                    COUNT(CASE WHEN UserType = 'Customer' THEN 1 END) AS NewCustomers,
                    COUNT(CASE WHEN UserType = 'Admin' THEN 1 END) AS NewAdmins
                FROM `User`
                WHERE CreatedDate >= DATE_SUB(NOW(), INTERVAL 30 DAY)",
            
            "inactive_customers" => @"
                SELECT 
                    u.UserID,
                    u.Username,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    MAX(po.OrderDate) AS LastOrderDate,
                    DATEDIFF(NOW(), MAX(po.OrderDate)) AS DaysSinceLastOrder
                FROM `User` u
                LEFT JOIN PurchaseOrder po ON u.UserID = po.UserID
                WHERE u.UserType = 'Customer'
                GROUP BY u.UserID, u.Username, u.Email, u.FirstName, u.LastName
                HAVING LastOrderDate IS NULL OR LastOrderDate < DATE_SUB(NOW(), INTERVAL 6 MONTH)
                ORDER BY DaysSinceLastOrder DESC
                LIMIT 50",
            
            "customer_lifetime_value" => @"
                SELECT 
                    u.UserID,
                    u.Username,
                    u.Email,
                    COUNT(DISTINCT po.OrderID) AS TotalOrders,
                    SUM(po.TotalAmount) AS TotalSpent,
                    AVG(po.TotalAmount) AS AverageOrderValue,
                    MIN(po.OrderDate) AS FirstOrderDate,
                    MAX(po.OrderDate) AS LastOrderDate,
                    DATEDIFF(MAX(po.OrderDate), MIN(po.OrderDate)) AS CustomerLifespanDays
                FROM `User` u
                LEFT JOIN PurchaseOrder po ON u.UserID = po.UserID AND po.Status = 'Fulfilled'
                WHERE u.UserType = 'Customer'
                GROUP BY u.UserID, u.Username, u.Email
                HAVING TotalOrders > 0
                ORDER BY TotalSpent DESC
                LIMIT 50",
            
            "registration_trend" => @"
                SELECT 
                    DATE(CreatedDate) AS RegistrationDate,
                    COUNT(*) AS NewUsers,
                    COUNT(CASE WHEN UserType = 'Customer' THEN 1 END) AS NewCustomers,
                    COUNT(CASE WHEN UserType = 'Admin' THEN 1 END) AS NewAdmins
                FROM `User`
                WHERE CreatedDate >= DATE_SUB(NOW(), INTERVAL 30 DAY)
                GROUP BY DATE(CreatedDate)
                ORDER BY RegistrationDate DESC",
            
            "slow_movers" => @"
                SELECT 
                    b.BookID,
                    b.ISBN,
                    b.Title,
                    b.Author,
                    b.Edition,
                    b.BookCondition,
                    b.SellingPrice,
                    b.CreatedDate,
                    DATEDIFF(NOW(), b.CreatedDate) AS DaysInInventory
                FROM Book b
                WHERE b.Status = 'Available'
                    AND b.BookID NOT IN (
                        SELECT DISTINCT BookID 
                        FROM OrderLineItem oli
                        INNER JOIN PurchaseOrder po ON oli.OrderID = po.OrderID
                        WHERE po.Status = 'Fulfilled'
                    )
                ORDER BY DaysInInventory DESC
                LIMIT 20",
            
            "restock_verification" => @"
                SELECT 
                    po.OrderID,
                    po.Status AS OrderStatus,
                    COUNT(oli.BookID) AS BooksInOrder,
                    COUNT(CASE WHEN b.Status = 'Available' THEN 1 END) AS BooksRestocked,
                    COUNT(CASE WHEN b.Status = 'Sold' THEN 1 END) AS BooksStillSold,
                    CASE 
                        WHEN COUNT(CASE WHEN b.Status = 'Available' THEN 1 END) = COUNT(oli.BookID) THEN 'All Restocked'
                        WHEN COUNT(CASE WHEN b.Status = 'Sold' THEN 1 END) = COUNT(oli.BookID) THEN 'Not Restocked'
                        ELSE 'Partially Restocked'
                    END AS RestockStatus
                FROM PurchaseOrder po
                INNER JOIN OrderLineItem oli ON po.OrderID = oli.OrderID
                INNER JOIN Book b ON oli.BookID = b.BookID
                WHERE po.Status = 'Cancelled'
                GROUP BY po.OrderID, po.Status
                ORDER BY po.OrderID DESC",
            
            _ => null
        };
    }
}

public class ExecuteQueryRequest
{
    public string QueryId { get; set; } = string.Empty;
}

