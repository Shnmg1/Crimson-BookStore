-- ============================================================================
-- CrimsonBookStore Management Queries
-- MySQL 8.0+ Compatible
-- ============================================================================
-- This file contains comprehensive management-level SQL queries for bookstore
-- administration, covering inventory, sales, acquisitions, and user analytics.
-- ============================================================================

USE crimsonbookstore;

-- ============================================================================
-- INVENTORY AND STOCK MANAGEMENT QUERIES
-- ============================================================================

-- ----------------------------------------------------------------------------
-- Question: Which books have low stock (stock < 1) that have been sold 
--           in the past 6 months? This helps identify popular books that 
--           need restocking.
-- ----------------------------------------------------------------------------
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
ORDER BY OrdersInLast6Months DESC, RevenueFromSales DESC;


-- ----------------------------------------------------------------------------
-- Question: What is the total dollar value of current inventory (all 
--           available books)? This provides the inventory valuation for 
--           financial reporting.
-- ----------------------------------------------------------------------------
SELECT 
    COUNT(*) AS TotalBooksInStock,
    SUM(SellingPrice) AS TotalInventoryValue,
    SUM(AcquisitionCost) AS TotalAcquisitionCost,
    SUM(SellingPrice - AcquisitionCost) AS TotalPotentialProfit,
    AVG(SellingPrice) AS AverageSellingPrice,
    AVG(AcquisitionCost) AS AverageAcquisitionCost
FROM Book
WHERE Status = 'Available';


-- ----------------------------------------------------------------------------
-- Question: What is the breakdown of books by condition (New, Good, Fair) 
--           and their average prices? This helps understand inventory 
--           composition and pricing strategy.
-- ----------------------------------------------------------------------------
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
ORDER BY BookCount DESC;


-- ----------------------------------------------------------------------------
-- Question: Which ISBN+Edition combinations have the highest stock levels? 
--           This identifies overstocked items that may need promotion.
-- ----------------------------------------------------------------------------
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
LIMIT 20;


-- ============================================================================
-- SALES AND PURCHASE ORDER ANALYSIS QUERIES
-- ============================================================================

-- ----------------------------------------------------------------------------
-- Question: What is the monthly sales revenue for the last 6 months? 
--           This tracks sales trends over time.
-- ----------------------------------------------------------------------------
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
ORDER BY Month DESC;


-- ----------------------------------------------------------------------------
-- Question: What are the top-selling books by quantity sold in the last 
--           3 months? This identifies bestsellers for marketing purposes.
-- ----------------------------------------------------------------------------
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
LIMIT 20;


-- ----------------------------------------------------------------------------
-- Question: What is the average order value across all completed orders? 
--           This metric helps understand customer spending patterns.
-- ----------------------------------------------------------------------------
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
WHERE po.Status = 'Fulfilled';


-- ----------------------------------------------------------------------------
-- Question: Which customers have placed 3 or more orders? This identifies 
--           loyal/repeat customers for targeted marketing.
-- ----------------------------------------------------------------------------
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
FROM User u
INNER JOIN PurchaseOrder po ON u.UserID = po.UserID
WHERE po.Status = 'Fulfilled'
GROUP BY u.UserID, u.Username, u.Email, u.FirstName, u.LastName
HAVING OrderCount >= 3
ORDER BY OrderCount DESC, TotalSpent DESC;


-- ----------------------------------------------------------------------------
-- Question: What is the order status breakdown? This shows the distribution 
--           of orders across different statuses for operational visibility.
-- ----------------------------------------------------------------------------
SELECT 
    po.Status,
    COUNT(*) AS OrderCount,
    SUM(po.TotalAmount) AS TotalValue,
    AVG(po.TotalAmount) AS AverageOrderValue,
    ROUND(COUNT(*) * 100.0 / (SELECT COUNT(*) FROM PurchaseOrder), 2) AS PercentageOfAllOrders
FROM PurchaseOrder po
GROUP BY po.Status
ORDER BY OrderCount DESC;


-- ============================================================================
-- BOOK ACQUISITION (SELL SUBMISSION) ANALYSIS QUERIES
-- ============================================================================

-- ----------------------------------------------------------------------------
-- Question: What is the count and average asking price of pending sell 
--           submissions? This helps assess the review backlog and potential 
--           inventory acquisition costs.
-- ----------------------------------------------------------------------------
SELECT 
    COUNT(*) AS PendingSubmissionsCount,
    AVG(AskingPrice) AS AverageAskingPrice,
    MIN(AskingPrice) AS MinimumAskingPrice,
    MAX(AskingPrice) AS MaximumAskingPrice,
    SUM(AskingPrice) AS TotalPotentialCost,
    AVG(DATEDIFF(NOW(), SubmissionDate)) AS AverageDaysPending
FROM SellSubmission
WHERE Status = 'Pending Review';


-- ----------------------------------------------------------------------------
-- Question: Which customers have submitted the most books for sale? 
--           This identifies top suppliers for potential partnerships.
-- ----------------------------------------------------------------------------
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
FROM User u
INNER JOIN SellSubmission ss ON u.UserID = ss.UserID
GROUP BY u.UserID, u.Username, u.Email, u.FirstName, u.LastName
HAVING TotalSubmissions > 0
ORDER BY TotalSubmissions DESC, ApprovedSubmissions DESC
LIMIT 20;


-- ----------------------------------------------------------------------------
-- Question: What is the sell submission status breakdown and approval rate? 
--           This tracks the efficiency of the book acquisition process.
-- ----------------------------------------------------------------------------
SELECT 
    ss.Status,
    COUNT(*) AS SubmissionCount,
    AVG(ss.AskingPrice) AS AverageAskingPrice,
    SUM(ss.AskingPrice) AS TotalAskingPrice,
    AVG(DATEDIFF(
        COALESCE(
            (SELECT MAX(OfferDate) FROM PriceNegotiation WHERE SubmissionID = ss.SubmissionID),
            ss.SubmissionDate
        ),
        ss.SubmissionDate
    )) AS AverageProcessingDays,
    ROUND(COUNT(*) * 100.0 / (SELECT COUNT(*) FROM SellSubmission), 2) AS PercentageOfAllSubmissions
FROM SellSubmission ss
GROUP BY ss.Status
ORDER BY SubmissionCount DESC;


-- ----------------------------------------------------------------------------
-- Question: Which admin has approved the most sell submissions? 
--           This provides an audit trail of admin activity.
-- ----------------------------------------------------------------------------
SELECT 
    u.UserID AS AdminUserID,
    u.Username AS AdminUsername,
    u.Email AS AdminEmail,
    u.FirstName AS AdminFirstName,
    u.LastName AS AdminLastName,
    COUNT(ss.SubmissionID) AS TotalApprovals,
    AVG(ss.AskingPrice) AS AverageApprovedPrice,
    SUM(ss.AskingPrice) AS TotalApprovedValue,
    MIN(ss.SubmissionDate) AS FirstApprovalDate,
    MAX(ss.SubmissionDate) AS LastApprovalDate
FROM User u
INNER JOIN SellSubmission ss ON u.UserID = ss.AdminUserID
WHERE ss.Status = 'Approved'
GROUP BY u.UserID, u.Username, u.Email, u.FirstName, u.LastName
ORDER BY TotalApprovals DESC;


-- ----------------------------------------------------------------------------
-- Question: What is the average negotiation rounds per submission? 
--           This helps understand the negotiation process efficiency.
-- ----------------------------------------------------------------------------
SELECT 
    ss.Status,
    COUNT(DISTINCT ss.SubmissionID) AS SubmissionCount,
    COUNT(pn.NegotiationID) AS TotalNegotiationRounds,
    AVG(negotiation_rounds.RoundCount) AS AverageRoundsPerSubmission,
    MAX(negotiation_rounds.RoundCount) AS MaxRoundsPerSubmission
FROM SellSubmission ss
LEFT JOIN PriceNegotiation pn ON ss.SubmissionID = pn.SubmissionID
LEFT JOIN (
    SELECT SubmissionID, COUNT(*) AS RoundCount
    FROM PriceNegotiation
    GROUP BY SubmissionID
) negotiation_rounds ON ss.SubmissionID = negotiation_rounds.SubmissionID
GROUP BY ss.Status
ORDER BY AverageRoundsPerSubmission DESC;


-- ============================================================================
-- USER AND ADMINISTRATION AUDIT QUERIES
-- ============================================================================

-- ----------------------------------------------------------------------------
-- Question: How many new users registered in the last 30 days? 
--           This tracks user growth.
-- ----------------------------------------------------------------------------
SELECT 
    COUNT(*) AS NewUsersLast30Days,
    SUM(CASE WHEN UserType = 'Customer' THEN 1 ELSE 0 END) AS NewCustomers,
    SUM(CASE WHEN UserType = 'Admin' THEN 1 ELSE 0 END) AS NewAdmins,
    DATE_FORMAT(MIN(CreatedDate), '%Y-%m-%d') AS FirstRegistrationDate,
    DATE_FORMAT(MAX(CreatedDate), '%Y-%m-%d') AS LastRegistrationDate
FROM User
WHERE CreatedDate >= DATE_SUB(NOW(), INTERVAL 30 DAY);


-- ----------------------------------------------------------------------------
-- Question: Which customers have been inactive (no orders) in the last 
--           6 months? This identifies customers who may need re-engagement.
-- ----------------------------------------------------------------------------
SELECT 
    u.UserID,
    u.Username,
    u.Email,
    u.FirstName,
    u.LastName,
    u.CreatedDate AS RegistrationDate,
    COUNT(DISTINCT po.OrderID) AS TotalOrders,
    MAX(po.OrderDate) AS LastOrderDate,
    DATEDIFF(NOW(), MAX(COALESCE(po.OrderDate, u.CreatedDate))) AS DaysSinceLastActivity
FROM User u
LEFT JOIN PurchaseOrder po ON u.UserID = po.UserID AND po.Status = 'Fulfilled'
WHERE u.UserType = 'Customer'
GROUP BY u.UserID, u.Username, u.Email, u.FirstName, u.LastName, u.CreatedDate
HAVING LastOrderDate IS NULL 
    OR LastOrderDate < DATE_SUB(NOW(), INTERVAL 6 MONTH)
ORDER BY DaysSinceLastActivity DESC, TotalOrders DESC
LIMIT 50;


-- ----------------------------------------------------------------------------
-- Question: What is the customer lifetime value analysis? This shows 
--           total spending per customer for segmentation.
-- ----------------------------------------------------------------------------
SELECT 
    CASE 
        WHEN TotalSpent >= 500 THEN 'High Value ($500+)'
        WHEN TotalSpent >= 200 THEN 'Medium Value ($200-$499)'
        WHEN TotalSpent >= 50 THEN 'Low Value ($50-$199)'
        ELSE 'Minimal Value (<$50)'
    END AS CustomerSegment,
    COUNT(*) AS CustomerCount,
    AVG(TotalSpent) AS AverageSpent,
    SUM(TotalSpent) AS TotalSegmentRevenue,
    AVG(OrderCount) AS AverageOrdersPerCustomer
FROM (
    SELECT 
        u.UserID,
        COUNT(DISTINCT po.OrderID) AS OrderCount,
        COALESCE(SUM(po.TotalAmount), 0) AS TotalSpent
    FROM User u
    LEFT JOIN PurchaseOrder po ON u.UserID = po.UserID AND po.Status = 'Fulfilled'
    WHERE u.UserType = 'Customer'
    GROUP BY u.UserID
) customer_stats
GROUP BY CustomerSegment
ORDER BY AverageSpent DESC;


-- ----------------------------------------------------------------------------
-- Question: What is the daily registration trend for the last 30 days? 
--           This shows user acquisition patterns.
-- ----------------------------------------------------------------------------
SELECT 
    DATE(CreatedDate) AS RegistrationDate,
    COUNT(*) AS NewRegistrations,
    SUM(CASE WHEN UserType = 'Customer' THEN 1 ELSE 0 END) AS NewCustomers,
    SUM(CASE WHEN UserType = 'Admin' THEN 1 ELSE 0 END) AS NewAdmins
FROM User
WHERE CreatedDate >= DATE_SUB(NOW(), INTERVAL 30 DAY)
GROUP BY DATE(CreatedDate)
ORDER BY RegistrationDate DESC;


-- ----------------------------------------------------------------------------
-- Question: Which books have been in inventory the longest without selling? 
--           This identifies slow-moving inventory that may need price 
--           adjustments or promotions.
-- ----------------------------------------------------------------------------
SELECT 
    b.ISBN,
    b.Title,
    b.Author,
    b.Edition,
    b.BookCondition,
    b.SellingPrice,
    b.AcquisitionCost,
    b.CreatedDate AS DateAddedToInventory,
    DATEDIFF(NOW(), b.CreatedDate) AS DaysInInventory,
    b.CourseMajor
FROM Book b
WHERE b.Status = 'Available'
    AND b.BookID NOT IN (
        SELECT DISTINCT BookID 
        FROM OrderLineItem
    )
ORDER BY DaysInInventory DESC
LIMIT 30;


-- ============================================================================
-- ORDER CANCELLATION AND RESTOCKING VERIFICATION QUERIES
-- ============================================================================

-- ----------------------------------------------------------------------------
-- Question: Verify that books from cancelled orders have been restocked
--           (Status = 'Available'). This query shows all cancelled orders
--           and the current status of books that were in those orders.
-- ----------------------------------------------------------------------------
SELECT 
    po.OrderID,
    po.OrderDate,
    po.Status AS OrderStatus,
    po.TotalAmount,
    b.BookID,
    b.ISBN,
    b.Title,
    b.Author,
    b.Edition,
    b.Status AS BookStatus,
    CASE 
        WHEN b.Status = 'Available' THEN '✓ Restocked'
        WHEN b.Status = 'Sold' THEN '✗ NOT Restocked (ERROR!)'
        ELSE '? Unknown Status'
    END AS RestockStatus,
    oli.PriceAtSale
FROM PurchaseOrder po
INNER JOIN OrderLineItem oli ON po.OrderID = oli.OrderID
INNER JOIN Book b ON oli.BookID = b.BookID
WHERE po.Status = 'Cancelled'
ORDER BY po.OrderID DESC, b.BookID;

-- ----------------------------------------------------------------------------
-- Question: Summary of restocking verification - count how many books
--           from cancelled orders are properly restocked vs. not restocked.
--           This gives a quick health check.
-- ----------------------------------------------------------------------------
SELECT 
    COUNT(DISTINCT po.OrderID) AS TotalCancelledOrders,
    COUNT(DISTINCT b.BookID) AS TotalBooksInCancelledOrders,
    COUNT(CASE WHEN b.Status = 'Available' THEN 1 END) AS BooksRestocked,
    COUNT(CASE WHEN b.Status = 'Sold' THEN 1 END) AS BooksNOTRestocked,
    COUNT(CASE WHEN b.Status NOT IN ('Available', 'Sold') THEN 1 END) AS BooksWithOtherStatus,
    CASE 
        WHEN COUNT(CASE WHEN b.Status = 'Sold' THEN 1 END) = 0 
        THEN '✓ All books properly restocked'
        ELSE '✗ WARNING: Some books not restocked!'
    END AS RestockHealth
FROM PurchaseOrder po
INNER JOIN OrderLineItem oli ON po.OrderID = oli.OrderID
INNER JOIN Book b ON oli.BookID = b.BookID
WHERE po.Status = 'Cancelled';

-- ----------------------------------------------------------------------------
-- Question: Check a specific cancelled order to verify restocking.
--           Replace @OrderID with the actual order ID you want to check.
-- ----------------------------------------------------------------------------
-- Example: Check order #2
SELECT 
    po.OrderID,
    po.OrderDate,
    po.Status AS OrderStatus,
    b.BookID,
    b.ISBN,
    b.Title,
    b.Author,
    b.Edition,
    b.Status AS BookStatus,
    CASE 
        WHEN b.Status = 'Available' THEN '✓ Restocked'
        ELSE '✗ NOT Restocked'
    END AS RestockStatus
FROM PurchaseOrder po
INNER JOIN OrderLineItem oli ON po.OrderID = oli.OrderID
INNER JOIN Book b ON oli.BookID = b.BookID
WHERE po.OrderID = 2  -- Replace with the order ID you want to check
ORDER BY b.BookID;

-- ----------------------------------------------------------------------------
-- Question: Find any books that are still marked as 'Sold' but belong
--           to cancelled orders. These should have been restocked.
--           This identifies any restocking failures.
-- ----------------------------------------------------------------------------
SELECT 
    po.OrderID,
    po.OrderDate,
    po.Status AS OrderStatus,
    b.BookID,
    b.ISBN,
    b.Title,
    b.Author,
    b.Edition,
    b.Status AS BookStatus,
    'ERROR: Book should be Available but is still Sold!' AS Issue
FROM PurchaseOrder po
INNER JOIN OrderLineItem oli ON po.OrderID = oli.OrderID
INNER JOIN Book b ON oli.BookID = b.BookID
WHERE po.Status = 'Cancelled'
    AND b.Status = 'Sold'
ORDER BY po.OrderID, b.BookID;
