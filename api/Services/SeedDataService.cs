using System.Data;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;

namespace CrimsonBookStore.Services;

public class SeedDataService
{
    private readonly string _connectionString;

    public SeedDataService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    public async Task SeedDataAsync()
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        // Check if data already exists
        var checkQuery = "SELECT COUNT(*) FROM `User`";
        await using var checkCmd = new MySqlCommand(checkQuery, connection);
        var userCount = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

        if (userCount > 0)
        {
            Console.WriteLine("Database already contains data. Skipping seed data.");
            return;
        }

        Console.WriteLine("Seeding database with sample data...");

        // Seed Users
        await SeedUsersAsync(connection);
        
        // Seed Books
        await SeedBooksAsync(connection);
        
        // Seed Sell Submissions
        await SeedSellSubmissionsAsync(connection);
        
        // Seed Price Negotiations
        await SeedPriceNegotiationsAsync(connection);
        
        // Seed Orders
        await SeedOrdersAsync(connection);
        
        // Seed Shopping Cart Items
        await SeedShoppingCartAsync(connection);
        
        // Seed Payment Methods
        await SeedPaymentMethodsAsync(connection);

        Console.WriteLine("Database seeding completed successfully!");
    }

    private async Task SeedUsersAsync(MySqlConnection connection)
    {
        var users = new[]
        {
            // Required accounts
            ("admin", "admin@crimsonbookstore.com", "123", "Admin", "User", "555-0100", "123 Admin St", "Admin"),
            ("james", "james@example.com", "123", "James", "Smith", "555-0101", "456 Customer Ave", "Customer"),
            
            // Additional customers (need at least 10 buyers + 5 sellers = 15+ customers)
            ("sarah", "sarah@example.com", "password123", "Sarah", "Johnson", "555-0102", "789 Main St", "Customer"),
            ("mike", "mike@example.com", "password123", "Mike", "Williams", "555-0103", "321 Oak Blvd", "Customer"),
            ("emily", "emily@example.com", "password123", "Emily", "Brown", "555-0104", "654 Pine Rd", "Customer"),
            ("david", "david@example.com", "password123", "David", "Jones", "555-0105", "987 Elm St", "Customer"),
            ("lisa", "lisa@example.com", "password123", "Lisa", "Davis", "555-0106", "147 Maple Dr", "Customer"),
            ("chris", "chris@example.com", "password123", "Chris", "Miller", "555-0107", "258 Cedar Ln", "Customer"),
            ("alex", "alex@example.com", "password123", "Alex", "Wilson", "555-0108", "369 Birch St", "Customer"),
            ("jessica", "jessica@example.com", "password123", "Jessica", "Moore", "555-0109", "741 Spruce Ave", "Customer"),
            ("ryan", "ryan@example.com", "password123", "Ryan", "Taylor", "555-0110", "852 Willow Way", "Customer"),
            ("amanda", "amanda@example.com", "password123", "Amanda", "Anderson", "555-0111", "963 Poplar Rd", "Customer"),
            ("brian", "brian@example.com", "password123", "Brian", "Thomas", "555-0112", "159 Ash Dr", "Customer"),
            ("nicole", "nicole@example.com", "password123", "Nicole", "Jackson", "555-0113", "357 Hickory Ln", "Customer"),
            ("kevin", "kevin@example.com", "password123", "Kevin", "White", "555-0114", "468 Chestnut Blvd", "Customer"),
            ("rachel", "rachel@example.com", "password123", "Rachel", "Harris", "555-0115", "579 Walnut St", "Customer"),
            
            // Additional admin
            ("admin2", "admin2@crimsonbookstore.com", "admin123", "Admin", "Two", "555-0200", "999 Admin Way", "Admin")
        };

        foreach (var (username, email, password, firstName, lastName, phone, address, userType) in users)
        {
            var query = @"
                INSERT INTO `User` (Username, Email, Password, FirstName, LastName, Phone, Address, UserType, CreatedDate)
                VALUES (@Username, @Email, @Password, @FirstName, @LastName, @Phone, @Address, @UserType, DATE_SUB(NOW(), INTERVAL FLOOR(RAND() * 180) DAY))";

            await using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@Password", password);
            cmd.Parameters.AddWithValue("@FirstName", firstName);
            cmd.Parameters.AddWithValue("@LastName", lastName);
            cmd.Parameters.AddWithValue("@Phone", phone);
            cmd.Parameters.AddWithValue("@Address", address);
            cmd.Parameters.AddWithValue("@UserType", userType);
            await cmd.ExecuteNonQueryAsync();
        }

        Console.WriteLine($"Seeded {users.Length} users");
    }

    private async Task SeedBooksAsync(MySqlConnection connection)
    {
        var books = new[]
        {
            // Computer Science Books
            ("978-0134685991", "Effective Java", "Joshua Bloch", "3rd", 45.99m, 25.00m, "New", "CS 301"),
            ("978-0134685991", "Effective Java", "Joshua Bloch", "3rd", 35.99m, 20.00m, "Good", "CS 301"),
            ("978-0134685991", "Effective Java", "Joshua Bloch", "3rd", 28.99m, 15.00m, "Fair", "CS 301"),
            ("978-0134685991", "Effective Java", "Joshua Bloch", "2nd", 30.99m, 18.00m, "Good", "CS 301"),
            
            ("978-0596007126", "Head First Design Patterns", "Eric Freeman", "1st", 42.99m, 22.00m, "New", "CS 350"),
            ("978-0596007126", "Head First Design Patterns", "Eric Freeman", "1st", 32.99m, 18.00m, "Good", "CS 350"),
            
            ("978-0132350884", "Clean Code", "Robert C. Martin", "1st", 38.99m, 20.00m, "New", "CS 400"),
            ("978-0132350884", "Clean Code", "Robert C. Martin", "1st", 28.99m, 15.00m, "Good", "CS 400"),
            
            // Business Books
            ("978-0062315007", "The Lean Startup", "Eric Ries", "1st", 15.99m, 8.00m, "New", "BUS 200"),
            ("978-0062315007", "The Lean Startup", "Eric Ries", "1st", 12.99m, 6.00m, "Good", "BUS 200"),
            
            ("978-1591846354", "Zero to One", "Peter Thiel", "1st", 18.99m, 10.00m, "New", "BUS 300"),
            
            // MIS Books
            ("978-0133548015", "Management Information Systems", "Kenneth Laudon", "15th", 125.99m, 60.00m, "New", "MIS 330"),
            ("978-0133548015", "Management Information Systems", "Kenneth Laudon", "15th", 95.99m, 45.00m, "Good", "MIS 330"),
            ("978-0133548015", "Management Information Systems", "Kenneth Laudon", "15th", 75.99m, 35.00m, "Fair", "MIS 330"),
            ("978-0133548015", "Management Information Systems", "Kenneth Laudon", "14th", 85.99m, 40.00m, "Good", "MIS 330"),
            
            ("978-0133050691", "Database Systems", "Thomas Connolly", "10th", 110.99m, 55.00m, "New", "MIS 350"),
            ("978-0133050691", "Database Systems", "Thomas Connolly", "10th", 85.99m, 40.00m, "Good", "MIS 350"),
            
            // Math Books
            ("978-0134689489", "Calculus: Early Transcendentals", "James Stewart", "8th", 180.99m, 90.00m, "New", "MATH 125"),
            ("978-0134689489", "Calculus: Early Transcendentals", "James Stewart", "8th", 140.99m, 70.00m, "Good", "MATH 125"),
            
            ("978-0321973617", "Linear Algebra", "David Lay", "5th", 95.99m, 48.00m, "New", "MATH 227"),
            
            // General Reading
            ("978-1982137274", "The Seven Husbands of Evelyn Hugo", "Taylor Jenkins Reid", "1st", 12.99m, 6.00m, "New", null),
            ("978-1982137274", "The Seven Husbands of Evelyn Hugo", "Taylor Jenkins Reid", "1st", 9.99m, 4.50m, "Good", null),
            
            ("978-0593099322", "Project Hail Mary", "Andy Weir", "1st", 14.99m, 7.00m, "New", null),
            ("978-0593099322", "Project Hail Mary", "Andy Weir", "1st", 10.99m, 5.00m, "Good", null),
            
            // Additional books to exceed 30 total
            ("978-0134685991", "Effective Java", "Joshua Bloch", "3rd", 40.99m, 22.00m, "Good", "CS 301"),
            ("978-0596007126", "Head First Design Patterns", "Eric Freeman", "1st", 38.99m, 20.00m, "Good", "CS 350"),
            ("978-0132350884", "Clean Code", "Robert C. Martin", "1st", 35.99m, 18.00m, "Good", "CS 400"),
            ("978-0133548015", "Management Information Systems", "Kenneth Laudon", "15th", 110.99m, 55.00m, "New", "MIS 330"),
            ("978-0133050691", "Database Systems", "Thomas Connolly", "10th", 100.99m, 50.00m, "New", "MIS 350"),
            ("978-0134689489", "Calculus: Early Transcendentals", "James Stewart", "8th", 160.99m, 80.00m, "New", "MATH 125"),
            ("978-0321973617", "Linear Algebra", "David Lay", "5th", 90.99m, 45.00m, "New", "MATH 227"),
            ("978-1982137274", "The Seven Husbands of Evelyn Hugo", "Taylor Jenkins Reid", "1st", 11.99m, 5.50m, "Good", null),
            ("978-0593099322", "Project Hail Mary", "Andy Weir", "1st", 13.99m, 6.50m, "Good", null),
            
            // Sold books (for order history - need at least 10 buyers)
            ("978-0134685991", "Effective Java", "Joshua Bloch", "3rd", 45.99m, 25.00m, "New", "CS 301"),
            ("978-0596007126", "Head First Design Patterns", "Eric Freeman", "1st", 42.99m, 22.00m, "New", "CS 350"),
            ("978-0132350884", "Clean Code", "Robert C. Martin", "1st", 38.99m, 20.00m, "New", "CS 400"),
            ("978-0133548015", "Management Information Systems", "Kenneth Laudon", "15th", 125.99m, 60.00m, "New", "MIS 330"),
            ("978-0133050691", "Database Systems", "Thomas Connolly", "10th", 110.99m, 55.00m, "New", "MIS 350"),
            ("978-0134689489", "Calculus: Early Transcendentals", "James Stewart", "8th", 180.99m, 90.00m, "New", "MATH 125"),
            ("978-0321973617", "Linear Algebra", "David Lay", "5th", 95.99m, 48.00m, "New", "MATH 227"),
            ("978-0062315007", "The Lean Startup", "Eric Ries", "1st", 15.99m, 8.00m, "New", "BUS 200"),
            ("978-1591846354", "Zero to One", "Peter Thiel", "1st", 18.99m, 10.00m, "New", "BUS 300"),
            ("978-1982137274", "The Seven Husbands of Evelyn Hugo", "Taylor Jenkins Reid", "1st", 12.99m, 6.00m, "New", null),
            ("978-0593099322", "Project Hail Mary", "Andy Weir", "1st", 14.99m, 7.00m, "New", null)
        };

        foreach (var (isbn, title, author, edition, sellingPrice, acquisitionCost, condition, courseMajor) in books)
        {
            var query = @"
                INSERT INTO Book (ISBN, Title, Author, Edition, SellingPrice, AcquisitionCost, BookCondition, CourseMajor, Status, CreatedDate)
                VALUES (@ISBN, @Title, @Author, @Edition, @SellingPrice, @AcquisitionCost, @BookCondition, @CourseMajor, 
                        IF(RAND() < 0.15, 'Sold', 'Available'), 
                        DATE_SUB(NOW(), INTERVAL FLOOR(RAND() * 90) DAY))";

            await using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@ISBN", isbn);
            cmd.Parameters.AddWithValue("@Title", title);
            cmd.Parameters.AddWithValue("@Author", author);
            cmd.Parameters.AddWithValue("@Edition", edition);
            cmd.Parameters.AddWithValue("@SellingPrice", sellingPrice);
            cmd.Parameters.AddWithValue("@AcquisitionCost", acquisitionCost);
            cmd.Parameters.AddWithValue("@BookCondition", condition);
            cmd.Parameters.AddWithValue("@CourseMajor", courseMajor ?? (object)DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        Console.WriteLine($"Seeded {books.Length} books");
    }

    private async Task SeedSellSubmissionsAsync(MySqlConnection connection)
    {
        // Get user IDs
        var userIds = new List<int>();
        var userQuery = "SELECT UserID FROM `User` WHERE UserType = 'Customer' ORDER BY UserID";
        await using var userCmd = new MySqlCommand(userQuery, connection);
        await using var userReader = await userCmd.ExecuteReaderAsync();
        while (await userReader.ReadAsync())
        {
            userIds.Add(userReader.GetInt32("UserID"));
        }
        await userReader.CloseAsync();

        if (userIds.Count == 0) return;

        // Need at least 5 sellers (customers with sell submissions)
        // Distribute submissions across at least 5 different customers
        var submissions = new[]
        {
            // Seller 1 (james - user ID 2)
            ("978-0134685991", "Effective Java", "Joshua Bloch", "4th", "New", "CS 301", 30.00m, "Pending Review"),
            ("978-0596007126", "Head First Design Patterns", "Eric Freeman", "2nd", "Good", "CS 350", 25.00m, "Pending Review"),
            
            // Seller 2 (sarah - user ID 3)
            ("978-0132350884", "Clean Code", "Robert C. Martin", "2nd", "New", "CS 400", 22.00m, "Approved"),
            ("978-0062315007", "The Lean Startup", "Eric Ries", "1st", "Good", "BUS 200", 10.00m, "Pending Review"),
            
            // Seller 3 (mike - user ID 4)
            ("978-1591846354", "Zero to One", "Peter Thiel", "1st", "New", "BUS 300", 12.00m, "Rejected"),
            ("978-0133548015", "Management Information Systems", "Kenneth Laudon", "16th", "New", "MIS 330", 70.00m, "Pending Review"),
            
            // Seller 4 (emily - user ID 5)
            ("978-0133050691", "Database Systems", "Thomas Connolly", "11th", "Good", "MIS 350", 50.00m, "Approved"),
            ("978-0134689489", "Calculus: Early Transcendentals", "James Stewart", "9th", "New", "MATH 125", 100.00m, "Pending Review"),
            
            // Seller 5 (david - user ID 6)
            ("978-0321973617", "Linear Algebra", "David Lay", "6th", "Good", "MATH 227", 55.00m, "Completed"),
            ("978-1982137274", "The Seven Husbands of Evelyn Hugo", "Taylor Jenkins Reid", "1st", "New", null, 8.00m, "Pending Review"),
            
            // Additional submissions from other sellers to exceed requirement
            ("978-0593099322", "Project Hail Mary", "Andy Weir", "1st", "Good", null, 7.00m, "Pending Review"),
            ("978-0134685991", "Effective Java", "Joshua Bloch", "3rd", "Fair", "CS 301", 20.00m, "Pending Review"),
            ("978-0133548015", "Management Information Systems", "Kenneth Laudon", "15th", "Good", "MIS 330", 50.00m, "Approved")
        };

        // Distribute submissions across at least 5 different sellers
        // First 2 submissions to user 0, next 2 to user 1, etc. to ensure 5+ sellers
        var sellerIndex = 0;
        var statusIndex = 0;
        foreach (var (isbn, title, author, edition, condition, courseMajor, askingPrice, status) in submissions)
        {
            // Cycle through first 5 users to ensure at least 5 sellers
            var userId = userIds[sellerIndex % Math.Min(5, userIds.Count)];
            sellerIndex++;
            var adminUserId = 1; // First admin

            var query = @"
                INSERT INTO SellSubmission (UserID, ISBN, Title, Author, Edition, PhysicalCondition, CourseMajor, AskingPrice, Status, SubmissionDate, AdminUserID)
                VALUES (@UserID, @ISBN, @Title, @Author, @Edition, @PhysicalCondition, @CourseMajor, @AskingPrice, @Status, 
                        DATE_SUB(NOW(), INTERVAL FLOOR(RAND() * 30) DAY),
                        IF(@Status IN ('Rejected', 'Approved', 'Completed'), @AdminUserID, NULL))";

            await using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@UserID", userId);
            cmd.Parameters.AddWithValue("@ISBN", isbn);
            cmd.Parameters.AddWithValue("@Title", title);
            cmd.Parameters.AddWithValue("@Author", author);
            cmd.Parameters.AddWithValue("@Edition", edition);
            cmd.Parameters.AddWithValue("@PhysicalCondition", condition);
            cmd.Parameters.AddWithValue("@CourseMajor", courseMajor ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@AskingPrice", askingPrice);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@AdminUserID", adminUserId);
            await cmd.ExecuteNonQueryAsync();
            statusIndex++;
        }

        Console.WriteLine($"Seeded {submissions.Length} sell submissions");
    }

    private async Task SeedPriceNegotiationsAsync(MySqlConnection connection)
    {
        // Get submission IDs that are in Pending Review or Approved
        var submissionIds = new List<int>();
        var subQuery = "SELECT SubmissionID FROM SellSubmission WHERE Status IN ('Pending Review', 'Approved') ORDER BY SubmissionID";
        await using var subCmd = new MySqlCommand(subQuery, connection);
        await using var subReader = await subCmd.ExecuteReaderAsync();
        while (await subReader.ReadAsync())
        {
            submissionIds.Add(subReader.GetInt32("SubmissionID"));
        }
        await subReader.CloseAsync();

        if (submissionIds.Count == 0) return;

        var roundNumber = 1;
        foreach (var submissionId in submissionIds.Take(5)) // Add negotiations to first 5 submissions
        {
            // Admin makes first offer
            var adminOfferQuery = @"
                INSERT INTO PriceNegotiation (SubmissionID, OfferedBy, OfferedPrice, OfferDate, OfferMessage, OfferStatus, RoundNumber)
                VALUES (@SubmissionID, 'Admin', @OfferedPrice, DATE_SUB(NOW(), INTERVAL 5 DAY), @OfferMessage, 
                        IF(RAND() < 0.3, 'Accepted', IF(RAND() < 0.5, 'Rejected', 'Pending')), @RoundNumber)";

            await using var adminCmd = new MySqlCommand(adminOfferQuery, connection);
            adminCmd.Parameters.AddWithValue("@SubmissionID", submissionId);
            adminCmd.Parameters.AddWithValue("@OfferedPrice", 20.00m + (submissionId * 2));
            adminCmd.Parameters.AddWithValue("@OfferMessage", "We can offer this price based on current market value.");
            adminCmd.Parameters.AddWithValue("@RoundNumber", roundNumber);
            await adminCmd.ExecuteNonQueryAsync();

            // Customer counter-offer (if admin offer is pending)
            if (submissionId % 2 == 0)
            {
                roundNumber++;
                var customerOfferQuery = @"
                    INSERT INTO PriceNegotiation (SubmissionID, OfferedBy, OfferedPrice, OfferDate, OfferMessage, OfferStatus, RoundNumber)
                    VALUES (@SubmissionID, 'User', @OfferedPrice, DATE_SUB(NOW(), INTERVAL 3 DAY), @OfferMessage, 'Pending', @RoundNumber)";

                await using var customerCmd = new MySqlCommand(customerOfferQuery, connection);
                customerCmd.Parameters.AddWithValue("@SubmissionID", submissionId);
                customerCmd.Parameters.AddWithValue("@OfferedPrice", 25.00m + (submissionId * 2));
                customerCmd.Parameters.AddWithValue("@OfferMessage", "I think this is a fair price given the condition.");
                customerCmd.Parameters.AddWithValue("@RoundNumber", roundNumber);
                await customerCmd.ExecuteNonQueryAsync();
            }

            roundNumber++;
        }

        Console.WriteLine("Seeded price negotiations");
    }

    private async Task SeedOrdersAsync(MySqlConnection connection)
    {
        // Get user IDs and available book IDs
        var userIds = new List<int>();
        var userQuery = "SELECT UserID FROM `User` WHERE UserType = 'Customer' ORDER BY UserID";
        await using var userCmd = new MySqlCommand(userQuery, connection);
        await using var userReader = await userCmd.ExecuteReaderAsync();
        while (await userReader.ReadAsync())
        {
            userIds.Add(userReader.GetInt32("UserID"));
        }
        await userReader.CloseAsync();

        var bookIds = new List<int>();
        var bookQuery = "SELECT BookID FROM Book WHERE Status = 'Sold' ORDER BY BookID";
        await using var bookCmd = new MySqlCommand(bookQuery, connection);
        await using var bookReader = await bookCmd.ExecuteReaderAsync();
        while (await bookReader.ReadAsync())
        {
            bookIds.Add(bookReader.GetInt32("BookID"));
        }
        await bookReader.CloseAsync();

        if (userIds.Count == 0 || bookIds.Count < 3) return;

        var statuses = new[] { "New", "Processing", "Fulfilled", "Complete", "Cancelled" };
        var statusIndex = 0;

        // Create at least 10 orders (need 10+ buyers)
        var orderCount = Math.Min(12, Math.Min(userIds.Count, bookIds.Count / 2));
        for (int i = 0; i < orderCount && bookIds.Count >= 2; i++)
        {
            // Distribute orders across different users to ensure 10+ buyers
            var userId = userIds[i % userIds.Count];
            var status = statuses[statusIndex % statuses.Length];
            statusIndex++;

            // Create order
            var orderQuery = @"
                INSERT INTO PurchaseOrder (UserID, Status, TotalAmount, OrderDate)
                VALUES (@UserID, @Status, 0, DATE_SUB(NOW(), INTERVAL @DaysAgo DAY))";

            await using var orderCmd = new MySqlCommand(orderQuery, connection);
            orderCmd.Parameters.AddWithValue("@UserID", userId);
            orderCmd.Parameters.AddWithValue("@Status", status);
            orderCmd.Parameters.AddWithValue("@DaysAgo", 10 - (i * 2));
            await orderCmd.ExecuteNonQueryAsync();

            var orderId = (int)orderCmd.LastInsertedId;

            // Add line items
            var booksForOrder = bookIds.Skip(i * 2).Take(2).ToList();
            decimal totalAmount = 0;

            foreach (var bookId in booksForOrder)
            {
                var priceQuery = "SELECT SellingPrice FROM Book WHERE BookID = @BookID";
                await using var priceCmd = new MySqlCommand(priceQuery, connection);
                priceCmd.Parameters.AddWithValue("@BookID", bookId);
                var price = Convert.ToDecimal(await priceCmd.ExecuteScalarAsync());
                totalAmount += price;

                var lineItemQuery = @"
                    INSERT INTO OrderLineItem (OrderID, BookID, PriceAtSale)
                    VALUES (@OrderID, @BookID, @PriceAtSale)";

                await using var lineItemCmd = new MySqlCommand(lineItemQuery, connection);
                lineItemCmd.Parameters.AddWithValue("@OrderID", orderId);
                lineItemCmd.Parameters.AddWithValue("@BookID", bookId);
                lineItemCmd.Parameters.AddWithValue("@PriceAtSale", price);
                await lineItemCmd.ExecuteNonQueryAsync();
            }

            // Update total amount
            var updateTotalQuery = "UPDATE PurchaseOrder SET TotalAmount = @TotalAmount WHERE OrderID = @OrderID";
            await using var updateCmd = new MySqlCommand(updateTotalQuery, connection);
            updateCmd.Parameters.AddWithValue("@TotalAmount", totalAmount);
            updateCmd.Parameters.AddWithValue("@OrderID", orderId);
            await updateCmd.ExecuteNonQueryAsync();

            // Create payment record
            var paymentQuery = @"
                INSERT INTO Payment (OrderID, PaymentMethodID, Amount, PaymentStatus, PaymentDate)
                VALUES (@OrderID, NULL, @Amount, 'Completed', DATE_SUB(NOW(), INTERVAL @DaysAgo DAY))";

            await using var paymentCmd = new MySqlCommand(paymentQuery, connection);
            paymentCmd.Parameters.AddWithValue("@OrderID", orderId);
            paymentCmd.Parameters.AddWithValue("@Amount", totalAmount);
            paymentCmd.Parameters.AddWithValue("@DaysAgo", 10 - (i * 2));
            await paymentCmd.ExecuteNonQueryAsync();
        }

        Console.WriteLine("Seeded orders");
    }

    private async Task SeedShoppingCartAsync(MySqlConnection connection)
    {
        // Get user IDs and available book IDs
        var userIds = new List<int>();
        var userQuery = "SELECT UserID FROM `User` WHERE UserType = 'Customer' ORDER BY UserID LIMIT 3";
        await using var userCmd = new MySqlCommand(userQuery, connection);
        await using var userReader = await userCmd.ExecuteReaderAsync();
        while (await userReader.ReadAsync())
        {
            userIds.Add(userReader.GetInt32("UserID"));
        }
        await userReader.CloseAsync();

        var bookIds = new List<int>();
        var bookQuery = "SELECT BookID FROM Book WHERE Status = 'Available' ORDER BY BookID LIMIT 10";
        await using var bookCmd = new MySqlCommand(bookQuery, connection);
        await using var bookReader = await bookCmd.ExecuteReaderAsync();
        while (await bookReader.ReadAsync())
        {
            bookIds.Add(bookReader.GetInt32("BookID"));
        }
        await bookReader.CloseAsync();

        if (userIds.Count == 0 || bookIds.Count == 0) return;

        var cartIndex = 0;
        foreach (var userId in userIds)
        {
            var booksForUser = bookIds.Skip(cartIndex).Take(2).ToList();
            cartIndex += 2;

            foreach (var bookId in booksForUser)
            {
                var query = @"
                    INSERT INTO ShoppingCart (UserID, BookID, AddedDate)
                    VALUES (@UserID, @BookID, DATE_SUB(NOW(), INTERVAL @DaysAgo DAY))";

                await using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@BookID", bookId);
                cmd.Parameters.AddWithValue("@DaysAgo", cartIndex);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        Console.WriteLine("Seeded shopping cart items");
    }

    private async Task SeedPaymentMethodsAsync(MySqlConnection connection)
    {
        // Get customer user IDs
        var userIds = new List<int>();
        var userQuery = "SELECT UserID FROM `User` WHERE UserType = 'Customer' ORDER BY UserID LIMIT 5";
        await using var userCmd = new MySqlCommand(userQuery, connection);
        await using var userReader = await userCmd.ExecuteReaderAsync();
        while (await userReader.ReadAsync())
        {
            userIds.Add(userReader.GetInt32("UserID"));
        }
        await userReader.CloseAsync();

        if (userIds.Count == 0) return;

        var cardTypes = new[] { "Visa", "Mastercard", "American Express" };
        var cardIndex = 0;

        foreach (var userId in userIds)
        {
            var cardType = cardTypes[cardIndex % cardTypes.Length];
            var lastFour = ((1000 + (userId * 7)) % 10000).ToString().PadLeft(4, '0');
            var expirationMonth = (cardIndex % 12) + 1;
            var expirationYear = 2025 + (cardIndex / 12);
            var isDefault = cardIndex == 0 ? 1 : 0;

            var query = @"
                INSERT INTO PaymentMethod (UserID, CardType, LastFourDigits, ExpirationDate, IsDefault, CreatedDate)
                VALUES (@UserID, @CardType, @LastFourDigits, @ExpirationDate, @IsDefault, DATE_SUB(NOW(), INTERVAL @DaysAgo DAY))";

            await using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@UserID", userId);
            cmd.Parameters.AddWithValue("@CardType", cardType);
            cmd.Parameters.AddWithValue("@LastFourDigits", lastFour);
            cmd.Parameters.AddWithValue("@ExpirationDate", $"{expirationMonth:D2}/{expirationYear}");
            cmd.Parameters.AddWithValue("@IsDefault", isDefault);
            cmd.Parameters.AddWithValue("@DaysAgo", cardIndex * 10);
            await cmd.ExecuteNonQueryAsync();

            cardIndex++;
        }

        Console.WriteLine("Seeded payment methods");
    }
}

