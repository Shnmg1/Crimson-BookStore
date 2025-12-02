# CrimsonBookStore - Backend SQL Queries Documentation

This document provides a comprehensive overview of all SQL queries used in the backend services that support the frontend application. All queries use parameterized statements to prevent SQL injection attacks.

---

## Table of Contents

1. [BookService Queries](#bookservice-queries)
2. [CartService Queries](#cartservice-queries)
3. [OrderService Queries](#orderservice-queries)
4. [AuthService Queries](#authservice-queries)
5. [PaymentMethodService Queries](#paymentmethodservice-queries)
6. [SellSubmissionService Queries](#sellsubmissionservice-queries)

---

## BookService Queries

### 1. Get Available Books (with search/filtering)
**Purpose**: Retrieves available books for customer browsing with optional search, ISBN, and course/major filters. Groups books by ISBN/Edition and aggregates prices and conditions.

**Frontend Support**: Book browsing, search functionality

**Query**:
```sql
SELECT 
    ISBN,
    Edition,
    Title,
    Author,
    MIN(SellingPrice) as min_price,
    MAX(SellingPrice) as max_price,
    COUNT(*) as available_count,
    GROUP_CONCAT(DISTINCT BookCondition ORDER BY 
        CASE BookCondition 
            WHEN 'New' THEN 1 
            WHEN 'Good' THEN 2 
            WHEN 'Fair' THEN 3 
        END) as available_conditions,
    MIN(CourseMajor) as course_major
FROM Book
WHERE Status = 'Available'
    [AND (Title LIKE @Search OR Author LIKE @Search OR ISBN LIKE @Search OR CourseMajor LIKE @Search)]
    [AND ISBN = @ISBN]
    [AND CourseMajor = @CourseMajor]
GROUP BY ISBN, Edition, Title, Author
HAVING available_count > 0
ORDER BY Title, Edition
```

**Parameters**: `@Search` (optional), `@ISBN` (optional), `@CourseMajor` (optional)

---

### 2. Get Book by ID (Customer)
**Purpose**: Retrieves detailed information about a specific available book for customers.

**Frontend Support**: Book detail page

**Query**:
```sql
SELECT 
    b.BookID,
    b.ISBN,
    b.Title,
    b.Author,
    b.Edition,
    b.SellingPrice,
    b.BookCondition,
    b.CourseMajor,
    b.Status,
    (SELECT COUNT(*) 
     FROM Book b2 
     WHERE b2.ISBN = b.ISBN 
       AND b2.Edition = b.Edition 
       AND b2.Status = 'Available') as available_count
FROM Book b
WHERE b.BookID = @BookID AND b.Status = 'Available'
```

**Parameters**: `@BookID`

---

### 3. Get Book by ID (Admin)
**Purpose**: Retrieves book details regardless of status for admin management.

**Frontend Support**: Admin book management

**Query**:
```sql
SELECT 
    b.BookID,
    b.ISBN,
    b.Title,
    b.Author,
    b.Edition,
    b.SellingPrice,
    b.BookCondition,
    b.CourseMajor,
    b.Status,
    (SELECT COUNT(*) 
     FROM Book b2 
     WHERE b2.ISBN = b.ISBN 
       AND b2.Edition = b.Edition 
       AND b2.Status = 'Available') as available_count
FROM Book b
WHERE b.BookID = @BookID
```

**Parameters**: `@BookID`

---

### 4. Get Book Copies
**Purpose**: Retrieves all available copies of a specific ISBN/Edition combination, ordered by condition and price.

**Frontend Support**: Book detail page showing available copies

**Query**:
```sql
SELECT 
    BookID,
    SellingPrice,
    BookCondition
FROM Book
WHERE ISBN = @ISBN 
  AND Edition = @Edition 
  AND Status = 'Available'
ORDER BY 
    CASE BookCondition 
        WHEN 'New' THEN 1 
        WHEN 'Good' THEN 2 
        WHEN 'Fair' THEN 3 
    END,
    SellingPrice ASC
```

**Parameters**: `@ISBN`, `@Edition`

---

### 5. Create Book
**Purpose**: Inserts a new book into the inventory.

**Frontend Support**: Admin book creation, sell submission approval

**Query**:
```sql
INSERT INTO Book (
    ISBN, Title, Author, Edition, 
    SellingPrice, AcquisitionCost, BookCondition, 
    CourseMajor, Status, CreatedDate
)
VALUES (
    @ISBN, @Title, @Author, @Edition,
    @SellingPrice, @AcquisitionCost, @BookCondition,
    @CourseMajor, 'Available', NOW()
)
```

**Parameters**: `@ISBN`, `@Title`, `@Author`, `@Edition`, `@SellingPrice`, `@AcquisitionCost`, `@BookCondition`, `@CourseMajor` (nullable)

**Follow-up Query**:
```sql
SELECT LAST_INSERT_ID()
```

---

### 6. Get All Books for Admin
**Purpose**: Retrieves all books (including sold) for admin inventory management.

**Frontend Support**: Admin book inventory management

**Query**:
```sql
SELECT 
    b.BookID,
    b.ISBN,
    b.Title,
    b.Author,
    b.Edition,
    b.SellingPrice,
    b.AcquisitionCost,
    b.BookCondition,
    b.CourseMajor,
    b.Status,
    (SELECT COUNT(*) 
     FROM Book b2 
     WHERE b2.ISBN = b.ISBN 
       AND b2.Edition = b.Edition 
       AND b2.Status = 'Available') as available_count
FROM Book b
ORDER BY b.BookID DESC
```

**Parameters**: None

---

### 7. Check Book Exists (for Update)
**Purpose**: Validates book exists before updating.

**Frontend Support**: Admin book update

**Query**:
```sql
SELECT BookID, SellingPrice, AcquisitionCost FROM Book WHERE BookID = @BookID
```

**Parameters**: `@BookID`

---

### 8. Update Book
**Purpose**: Dynamically updates book fields based on provided values.

**Frontend Support**: Admin book editing

**Query**:
```sql
UPDATE Book 
SET [SellingPrice = @SellingPrice]
    [, AcquisitionCost = @AcquisitionCost]
    [, BookCondition = @BookCondition]
    [, CourseMajor = @CourseMajor]
    [, Status = @Status]
WHERE BookID = @BookID
```

**Parameters**: `@BookID`, plus any of: `@SellingPrice`, `@AcquisitionCost`, `@BookCondition`, `@CourseMajor`, `@Status`

**Validation Queries** (if needed):
```sql
SELECT AcquisitionCost FROM Book WHERE BookID = @BookID
SELECT SellingPrice FROM Book WHERE BookID = @BookID
```

---

### 9. Check Book in Active Order
**Purpose**: Validates book can be deleted (not in active orders).

**Frontend Support**: Admin book deletion

**Query**:
```sql
SELECT COUNT(*) as order_count
FROM OrderLineItem oli
JOIN PurchaseOrder po ON oli.OrderID = po.OrderID
WHERE oli.BookID = @BookID 
  AND po.Status != 'Cancelled'
```

**Parameters**: `@BookID`

---

### 10. Check Book Exists (for Delete)
**Purpose**: Validates book exists before deletion.

**Frontend Support**: Admin book deletion

**Query**:
```sql
SELECT BookID FROM Book WHERE BookID = @BookID
```

**Parameters**: `@BookID`

---

### 11. Delete Book
**Purpose**: Removes a book from inventory.

**Frontend Support**: Admin book deletion

**Query**:
```sql
DELETE FROM Book WHERE BookID = @BookID
```

**Parameters**: `@BookID`

---

## CartService Queries

### 1. Get Cart
**Purpose**: Retrieves all items in a user's shopping cart with book details.

**Frontend Support**: Shopping cart page

**Query**:
```sql
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
ORDER BY sc.AddedDate DESC
```

**Parameters**: `@UserID`

---

### 2. Validate Book for Cart
**Purpose**: Checks if book exists and is available before adding to cart.

**Frontend Support**: Add to cart functionality

**Query**:
```sql
SELECT BookID, Status
FROM Book
WHERE BookID = @BookID
```

**Parameters**: `@BookID`

---

### 3. Check Item Already in Cart
**Purpose**: Prevents duplicate items in cart.

**Frontend Support**: Add to cart functionality

**Query**:
```sql
SELECT CartItemID
FROM ShoppingCart
WHERE UserID = @UserID AND BookID = @BookID
```

**Parameters**: `@UserID`, `@BookID`

---

### 4. Add to Cart
**Purpose**: Inserts a book into the user's shopping cart.

**Frontend Support**: Add to cart functionality

**Query**:
```sql
INSERT INTO ShoppingCart (UserID, BookID, AddedDate)
VALUES (@UserID, @BookID, CURRENT_TIMESTAMP)
```

**Parameters**: `@UserID`, `@BookID`

**Follow-up Query**:
```sql
SELECT CartItemID
FROM ShoppingCart
WHERE UserID = @UserID AND BookID = @BookID
```

---

### 5. Remove from Cart
**Purpose**: Removes a specific item from the shopping cart.

**Frontend Support**: Remove item from cart

**Query**:
```sql
DELETE FROM ShoppingCart
WHERE CartItemID = @CartItemID AND UserID = @UserID
```

**Parameters**: `@CartItemID`, `@UserID`

---

### 6. Clear Cart
**Purpose**: Removes all items from a user's shopping cart.

**Frontend Support**: Clear cart functionality

**Query**:
```sql
DELETE FROM ShoppingCart
WHERE UserID = @UserID
```

**Parameters**: `@UserID`

---

## OrderService Queries

### 1. Validate Cart for Order Creation
**Purpose**: Checks cart contents and availability before creating order.

**Frontend Support**: Checkout process

**Query**:
```sql
SELECT sc.BookID, b.SellingPrice, b.Status, b.Title
FROM ShoppingCart sc
JOIN Book b ON sc.BookID = b.BookID
WHERE sc.UserID = @UserID
```

**Parameters**: `@UserID`

---

### 2. Create Purchase Order
**Purpose**: Creates a new purchase order record.

**Frontend Support**: Order creation

**Query**:
```sql
INSERT INTO PurchaseOrder (UserID, Status, TotalAmount, OrderDate)
VALUES (@UserID, 'New', 0, CURRENT_TIMESTAMP)
```

**Parameters**: `@UserID`

**Follow-up Query**:
```sql
SELECT LAST_INSERT_ID()
```

---

### 3. Create Order Line Items
**Purpose**: Creates line items for all books in the cart.

**Frontend Support**: Order creation

**Query**:
```sql
INSERT INTO OrderLineItem (OrderID, BookID, PriceAtSale)
SELECT @OrderID, sc.BookID, b.SellingPrice
FROM ShoppingCart sc
JOIN Book b ON sc.BookID = b.BookID
WHERE sc.UserID = @UserID AND b.Status = 'Available'
```

**Parameters**: `@OrderID`, `@UserID`

---

### 4. Update Book Status to Sold
**Purpose**: Marks books as sold when order is created.

**Frontend Support**: Order creation, inventory management

**Query**:
```sql
UPDATE Book
SET Status = 'Sold'
WHERE BookID IN (
    SELECT BookID FROM ShoppingCart WHERE UserID = @UserID
)
```

**Parameters**: `@UserID`

---

### 5. Calculate and Update Order Total
**Purpose**: Calculates total amount from line items and updates order.

**Frontend Support**: Order creation

**Query**:
```sql
UPDATE PurchaseOrder
SET TotalAmount = (
    SELECT COALESCE(SUM(PriceAtSale), 0)
    FROM OrderLineItem
    WHERE OrderID = @OrderID
)
WHERE OrderID = @OrderID
```

**Parameters**: `@OrderID`

---

### 6. Get Order Total
**Purpose**: Retrieves calculated total for order response.

**Frontend Support**: Order creation response

**Query**:
```sql
SELECT TotalAmount FROM PurchaseOrder WHERE OrderID = @OrderID
```

**Parameters**: `@OrderID`

---

### 7. Create Payment Record
**Purpose**: Creates payment record for the order.

**Frontend Support**: Order creation, payment tracking

**Query**:
```sql
INSERT INTO Payment (OrderID, PaymentMethodID, Amount, PaymentStatus, PaymentDate)
VALUES (@OrderID, @PaymentMethodID, @Amount, 'Completed', CURRENT_TIMESTAMP)
```

**Parameters**: `@OrderID`, `@PaymentMethodID` (nullable), `@Amount`

---

### 8. Clear Shopping Cart
**Purpose**: Removes all items from cart after successful order.

**Frontend Support**: Order creation

**Query**:
```sql
DELETE FROM ShoppingCart WHERE UserID = @UserID
```

**Parameters**: `@UserID`

---

### 9. Get Order Date
**Purpose**: Retrieves order date for response.

**Frontend Support**: Order creation response

**Query**:
```sql
SELECT OrderDate FROM PurchaseOrder WHERE OrderID = @OrderID
```

**Parameters**: `@OrderID`

---

### 10. Get Order Items for Response
**Purpose**: Retrieves order line items with book details for response.

**Frontend Support**: Order creation response

**Query**:
```sql
SELECT oli.BookID, oli.PriceAtSale, b.Title
FROM OrderLineItem oli
JOIN Book b ON oli.BookID = b.BookID
WHERE oli.OrderID = @OrderID
```

**Parameters**: `@OrderID`

---

### 11. Get User Orders
**Purpose**: Retrieves list of orders for a user with optional status filter.

**Frontend Support**: Order history page

**Query**:
```sql
SELECT 
    po.OrderID,
    po.OrderDate,
    po.Status,
    po.TotalAmount,
    COUNT(oli.LineItemID) as item_count
FROM PurchaseOrder po
LEFT JOIN OrderLineItem oli ON po.OrderID = oli.OrderID
WHERE po.UserID = @UserID
    [AND po.Status = @Status]
GROUP BY po.OrderID, po.OrderDate, po.Status, po.TotalAmount
ORDER BY po.OrderDate DESC
```

**Parameters**: `@UserID`, `@Status` (optional)

---

### 12. Verify Order Ownership
**Purpose**: Validates order belongs to user before retrieving details.

**Frontend Support**: Order details page

**Query**:
```sql
SELECT OrderID, UserID
FROM PurchaseOrder
WHERE OrderID = @OrderID
```

**Parameters**: `@OrderID`

---

### 13. Get Order Details
**Purpose**: Retrieves order header information.

**Frontend Support**: Order details page

**Query**:
```sql
SELECT OrderID, OrderDate, Status, TotalAmount
FROM PurchaseOrder
WHERE OrderID = @OrderID
```

**Parameters**: `@OrderID`

---

### 14. Get Order Line Items with Book Details
**Purpose**: Retrieves detailed line items for order display.

**Frontend Support**: Order details page

**Query**:
```sql
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
WHERE oli.OrderID = @OrderID
```

**Parameters**: `@OrderID`

---

### 15. Get Payment Information
**Purpose**: Retrieves payment details for order.

**Frontend Support**: Order details page

**Query**:
```sql
SELECT 
    p.PaymentID,
    p.PaymentDate,
    p.Amount,
    p.PaymentStatus,
    pm.CardType,
    pm.LastFourDigits
FROM Payment p
LEFT JOIN PaymentMethod pm ON p.PaymentMethodID = pm.PaymentMethodID
WHERE p.OrderID = @OrderID
```

**Parameters**: `@OrderID`

---

### 16. Get Admin Orders
**Purpose**: Retrieves all orders for admin management with optional status filter.

**Frontend Support**: Admin order management

**Query**:
```sql
SELECT 
    po.OrderID,
    po.UserID,
    u.Username,
    po.OrderDate,
    po.Status,
    po.TotalAmount,
    COUNT(oli.LineItemID) as item_count
FROM PurchaseOrder po
JOIN User u ON po.UserID = u.UserID
LEFT JOIN OrderLineItem oli ON po.OrderID = oli.OrderID
[WHERE po.Status = @Status]
GROUP BY po.OrderID, po.UserID, u.Username, po.OrderDate, po.Status, po.TotalAmount
ORDER BY po.OrderDate DESC
```

**Parameters**: `@Status` (optional)

---

### 17. Get Current Order Status
**Purpose**: Retrieves current status for status transition validation.

**Frontend Support**: Admin order status update

**Query**:
```sql
SELECT Status FROM PurchaseOrder WHERE OrderID = @OrderID
```

**Parameters**: `@OrderID`

---

### 18. Update Order Status (Simple)
**Purpose**: Updates order status for non-cancellation status changes.

**Frontend Support**: Admin order status update

**Query**:
```sql
UPDATE PurchaseOrder
SET Status = @Status
WHERE OrderID = @OrderID
```

**Parameters**: `@OrderID`, `@Status`

---

### 19. Update Order Status (Cancellation with Restock)
**Purpose**: Updates order status to Cancelled and restocks books in a transaction.

**Frontend Support**: Admin order cancellation

**Query**:
```sql
-- Update order status
UPDATE PurchaseOrder
SET Status = @Status
WHERE OrderID = @OrderID

-- Restock books
UPDATE Book b
INNER JOIN OrderLineItem oli ON b.BookID = oli.BookID
SET b.Status = 'Available'
WHERE oli.OrderID = @OrderID
```

**Parameters**: `@OrderID`, `@Status` (must be 'Cancelled')

**Note**: Executed within a transaction to ensure atomicity.

---

## AuthService Queries

### 1. Check Username Exists
**Purpose**: Validates username is unique during registration.

**Frontend Support**: User registration

**Query**:
```sql
SELECT COUNT(*) FROM `User` WHERE Username = @Username
```

**Parameters**: `@Username`

---

### 2. Check Email Exists
**Purpose**: Validates email is unique during registration.

**Frontend Support**: User registration

**Query**:
```sql
SELECT COUNT(*) FROM `User` WHERE Email = @Email
```

**Parameters**: `@Email`

---

### 3. Create User
**Purpose**: Inserts new user record.

**Frontend Support**: User registration

**Query**:
```sql
INSERT INTO `User` (Username, Email, Password, FirstName, LastName, Phone, Address, UserType, CreatedDate)
VALUES (@Username, @Email, @Password, @FirstName, @LastName, @Phone, @Address, @UserType, NOW())
```

**Parameters**: `@Username`, `@Email`, `@Password`, `@FirstName`, `@LastName`, `@Phone` (nullable), `@Address` (nullable), `@UserType`

---

### 4. Get Created User
**Purpose**: Retrieves user details after registration.

**Frontend Support**: User registration response

**Query**:
```sql
SELECT UserID, Username, Email, UserType FROM `User` WHERE Username = @Username
```

**Parameters**: `@Username`

---

### 5. Login - Get User by Username
**Purpose**: Retrieves user credentials for authentication.

**Frontend Support**: User login

**Query**:
```sql
SELECT UserID, Username, Email, Password, UserType 
FROM `User` 
WHERE Username = @Username
```

**Parameters**: `@Username`

---

### 6. Get Admin Users
**Purpose**: Retrieves all users for admin management with optional user type filter.

**Frontend Support**: Admin user management

**Query**:
```sql
SELECT 
    UserID,
    Username,
    Email,
    FirstName,
    LastName,
    UserType,
    CreatedDate
FROM `User`
[WHERE UserType = @UserType]
ORDER BY CreatedDate DESC
```

**Parameters**: `@UserType` (optional)

---

## PaymentMethodService Queries

### 1. Get Payment Methods
**Purpose**: Retrieves all payment methods for a user.

**Frontend Support**: Payment methods page

**Query**:
```sql
SELECT 
    PaymentMethodID,
    CardType,
    LastFourDigits,
    ExpirationDate,
    IsDefault,
    CreatedDate
FROM PaymentMethod
WHERE UserID = @UserID
ORDER BY IsDefault DESC, CreatedDate DESC
```

**Parameters**: `@UserID`

---

### 2. Unset Default Payment Methods
**Purpose**: Removes default flag from all payment methods when setting a new default.

**Frontend Support**: Create/set default payment method

**Query**:
```sql
UPDATE PaymentMethod
SET IsDefault = 0
WHERE UserID = @UserID AND IsDefault = 1
```

**Parameters**: `@UserID`

---

### 3. Create Payment Method
**Purpose**: Inserts new payment method.

**Frontend Support**: Add payment method

**Query**:
```sql
INSERT INTO PaymentMethod (UserID, CardType, LastFourDigits, ExpirationDate, IsDefault, CreatedDate)
VALUES (@UserID, @CardType, @LastFourDigits, @ExpirationDate, @IsDefault, NOW())
```

**Parameters**: `@UserID`, `@CardType`, `@LastFourDigits`, `@ExpirationDate`, `@IsDefault`

**Follow-up Query**:
```sql
SELECT PaymentMethodID
FROM PaymentMethod
WHERE UserID = @UserID AND CardType = @CardType AND LastFourDigits = @LastFourDigits
ORDER BY CreatedDate DESC
LIMIT 1
```

---

### 4. Verify Payment Method Ownership
**Purpose**: Validates payment method belongs to user before operations.

**Frontend Support**: Delete/set default payment method

**Query**:
```sql
SELECT PaymentMethodID
FROM PaymentMethod
WHERE PaymentMethodID = @PaymentMethodID AND UserID = @UserID
```

**Parameters**: `@PaymentMethodID`, `@UserID`

---

### 5. Check Payment Method Usage
**Purpose**: Validates payment method can be deleted (not used in orders).

**Frontend Support**: Delete payment method

**Query**:
```sql
SELECT COUNT(*) as UsageCount
FROM Payment
WHERE PaymentMethodID = @PaymentMethodID
```

**Parameters**: `@PaymentMethodID`

---

### 6. Delete Payment Method
**Purpose**: Removes payment method from user's account.

**Frontend Support**: Delete payment method

**Query**:
```sql
DELETE FROM PaymentMethod
WHERE PaymentMethodID = @PaymentMethodID AND UserID = @UserID
```

**Parameters**: `@PaymentMethodID`, `@UserID`

---

### 7. Set Default Payment Method
**Purpose**: Sets a payment method as default for user.

**Frontend Support**: Set default payment method

**Query**:
```sql
-- Unset all defaults
UPDATE PaymentMethod
SET IsDefault = 0
WHERE UserID = @UserID AND IsDefault = 1

-- Set new default
UPDATE PaymentMethod
SET IsDefault = 1
WHERE PaymentMethodID = @PaymentMethodID AND UserID = @UserID
```

**Parameters**: `@PaymentMethodID`, `@UserID`

---

## SellSubmissionService Queries

### 1. Create Sell Submission
**Purpose**: Inserts new sell submission from customer.

**Frontend Support**: Sell book submission

**Query**:
```sql
INSERT INTO SellSubmission (
    UserID, ISBN, Title, Author, Edition, 
    PhysicalCondition, CourseMajor, AskingPrice, 
    Status, SubmissionDate
)
VALUES (
    @UserID, @ISBN, @Title, @Author, @Edition,
    @PhysicalCondition, @CourseMajor, @AskingPrice,
    'Pending Review', CURRENT_TIMESTAMP
)
```

**Parameters**: `@UserID`, `@ISBN`, `@Title`, `@Author`, `@Edition`, `@PhysicalCondition`, `@CourseMajor` (nullable), `@AskingPrice`

**Follow-up Queries**:
```sql
SELECT LAST_INSERT_ID()
SELECT SubmissionDate FROM SellSubmission WHERE SubmissionID = @SubmissionID
```

---

### 2. Get User Submissions
**Purpose**: Retrieves all sell submissions for a user with optional status filter.

**Frontend Support**: User sell submissions page

**Query**:
```sql
SELECT 
    SubmissionID,
    ISBN,
    Title,
    Author,
    Edition,
    AskingPrice,
    Status,
    SubmissionDate
FROM SellSubmission
WHERE UserID = @UserID
    [AND Status = @Status]
ORDER BY SubmissionDate DESC
```

**Parameters**: `@UserID`, `@Status` (optional)

---

### 3. Verify Submission Ownership
**Purpose**: Validates submission belongs to user.

**Frontend Support**: Submission details, negotiation

**Query**:
```sql
SELECT SubmissionID, UserID
FROM SellSubmission
WHERE SubmissionID = @SubmissionID
```

**Parameters**: `@SubmissionID`

---

### 4. Get Submission Details
**Purpose**: Retrieves submission information.

**Frontend Support**: Submission details page

**Query**:
```sql
SELECT 
    SubmissionID,
    ISBN,
    Title,
    Author,
    Edition,
    AskingPrice,
    Status,
    SubmissionDate
FROM SellSubmission
WHERE SubmissionID = @SubmissionID
```

**Parameters**: `@SubmissionID`

---

### 5. Get Negotiation History
**Purpose**: Retrieves all price negotiations for a submission.

**Frontend Support**: Submission details, negotiation display

**Query**:
```sql
SELECT 
    NegotiationID,
    OfferedBy,
    OfferedPrice,
    OfferDate,
    OfferMessage,
    OfferStatus,
    RoundNumber
FROM PriceNegotiation
WHERE SubmissionID = @SubmissionID
ORDER BY RoundNumber ASC
```

**Parameters**: `@SubmissionID`

---

### 6. Verify Negotiation for Accept
**Purpose**: Validates negotiation exists and is in correct status for acceptance.

**Frontend Support**: Customer accept offer

**Query**:
```sql
SELECT NegotiationID, OfferStatus, OfferedPrice, OfferedBy
FROM PriceNegotiation
WHERE NegotiationID = @NegotiationID AND SubmissionID = @SubmissionID
```

**Parameters**: `@NegotiationID`, `@SubmissionID`

---

### 7. Get Latest Pending Admin Offer
**Purpose**: Ensures customer can only accept the latest pending admin offer.

**Frontend Support**: Customer accept offer validation

**Query**:
```sql
SELECT NegotiationID
FROM PriceNegotiation
WHERE SubmissionID = @SubmissionID
  AND OfferedBy = 'Admin'
  AND OfferStatus = 'Pending'
ORDER BY RoundNumber DESC
LIMIT 1
```

**Parameters**: `@SubmissionID`

---

### 8. Update Negotiation Status to Accepted
**Purpose**: Marks negotiation as accepted when customer accepts admin offer.

**Frontend Support**: Customer accept offer

**Query**:
```sql
UPDATE PriceNegotiation
SET OfferStatus = 'Accepted'
WHERE NegotiationID = @NegotiationID
```

**Parameters**: `@NegotiationID`

---

### 9. Update Submission Status to Approved
**Purpose**: Updates submission status when customer accepts offer.

**Frontend Support**: Customer accept offer

**Query**:
```sql
UPDATE SellSubmission
SET Status = 'Approved'
WHERE SubmissionID = @SubmissionID
```

**Parameters**: `@SubmissionID`

---

### 10. Verify Negotiation for Reject
**Purpose**: Validates negotiation exists for rejection.

**Frontend Support**: Customer reject offer

**Query**:
```sql
SELECT NegotiationID, OfferStatus, OfferedBy
FROM PriceNegotiation
WHERE NegotiationID = @NegotiationID AND SubmissionID = @SubmissionID
```

**Parameters**: `@NegotiationID`, `@SubmissionID`

---

### 11. Check Other Pending Offers
**Purpose**: Checks if other pending offers exist when rejecting.

**Frontend Support**: Customer reject offer

**Query**:
```sql
SELECT COUNT(*) as pending_count
FROM PriceNegotiation
WHERE SubmissionID = @SubmissionID
  AND OfferedBy = 'Admin'
  AND OfferStatus = 'Pending'
  AND NegotiationID != @NegotiationID
```

**Parameters**: `@SubmissionID`, `@NegotiationID`

---

### 12. Update Negotiation Status to Rejected
**Purpose**: Marks negotiation as rejected.

**Frontend Support**: Customer reject offer

**Query**:
```sql
UPDATE PriceNegotiation
SET OfferStatus = 'Rejected'
WHERE NegotiationID = @NegotiationID
```

**Parameters**: `@NegotiationID`

---

### 13. Update Submission Status to Rejected
**Purpose**: Rejects submission if no other pending offers exist.

**Frontend Support**: Customer reject offer

**Query**:
```sql
UPDATE SellSubmission
SET Status = 'Rejected'
WHERE SubmissionID = @SubmissionID
```

**Parameters**: `@SubmissionID`

---

### 14. Get Last Negotiation for Counter
**Purpose**: Validates customer can counter (last offer must be from admin and pending).

**Frontend Support**: Customer counter-offer

**Query**:
```sql
SELECT OfferedBy, OfferStatus
FROM PriceNegotiation
WHERE SubmissionID = @SubmissionID
ORDER BY RoundNumber DESC
LIMIT 1
```

**Parameters**: `@SubmissionID`

---

### 15. Get Next Round Number
**Purpose**: Calculates next round number for negotiation.

**Frontend Support**: Customer counter-offer, admin negotiate

**Query**:
```sql
SELECT COALESCE(MAX(RoundNumber), 0) + 1 as next_round
FROM PriceNegotiation
WHERE SubmissionID = @SubmissionID
```

**Parameters**: `@SubmissionID`

---

### 16. Create Customer Counter-Offer
**Purpose**: Inserts customer counter-offer negotiation.

**Frontend Support**: Customer counter-offer

**Query**:
```sql
INSERT INTO PriceNegotiation (
    SubmissionID, OfferedBy, OfferedPrice, 
    OfferDate, OfferMessage, OfferStatus, RoundNumber
)
VALUES (
    @SubmissionID, 'User', @OfferedPrice,
    CURRENT_TIMESTAMP, @OfferMessage, 'Pending', @RoundNumber
)
```

**Parameters**: `@SubmissionID`, `@OfferedPrice`, `@RoundNumber`, `@OfferMessage` (nullable)

**Follow-up Query**:
```sql
SELECT LAST_INSERT_ID()
```

---

### 17. Get Admin Submissions
**Purpose**: Retrieves all sell submissions for admin review with optional status filter.

**Frontend Support**: Admin sell submissions management

**Query**:
```sql
SELECT 
    ss.SubmissionID,
    ss.UserID,
    u.Username,
    ss.ISBN,
    ss.Title,
    ss.AskingPrice,
    ss.Status,
    ss.SubmissionDate
FROM SellSubmission ss
JOIN User u ON ss.UserID = u.UserID
[WHERE ss.Status = @Status]
ORDER BY ss.SubmissionDate DESC
```

**Parameters**: `@Status` (optional)

---

### 18. Get Admin Submission Details
**Purpose**: Retrieves detailed submission information for admin.

**Frontend Support**: Admin submission review

**Query**:
```sql
SELECT 
    ss.SubmissionID,
    ss.UserID,
    u.Username,
    ss.ISBN,
    ss.Title,
    ss.Author,
    ss.Edition,
    ss.AskingPrice,
    ss.PhysicalCondition,
    ss.CourseMajor,
    ss.Status,
    ss.SubmissionDate
FROM SellSubmission ss
JOIN User u ON ss.UserID = u.UserID
WHERE ss.SubmissionID = @SubmissionID
```

**Parameters**: `@SubmissionID`

---

### 19. Verify Submission for Admin Negotiate
**Purpose**: Validates submission exists and is in correct status.

**Frontend Support**: Admin negotiate offer

**Query**:
```sql
SELECT SubmissionID, Status
FROM SellSubmission
WHERE SubmissionID = @SubmissionID
```

**Parameters**: `@SubmissionID`

---

### 20. Get Last Negotiation for Admin
**Purpose**: Validates admin can make offer (checks last negotiation status).

**Frontend Support**: Admin negotiate offer

**Query**:
```sql
SELECT OfferedBy, OfferStatus
FROM PriceNegotiation
WHERE SubmissionID = @SubmissionID
ORDER BY RoundNumber DESC
LIMIT 1
```

**Parameters**: `@SubmissionID`

---

### 21. Reject Old Admin Offers
**Purpose**: Auto-rejects previous pending admin offers when new offer is made.

**Frontend Support**: Admin negotiate offer

**Query**:
```sql
UPDATE PriceNegotiation
SET OfferStatus = 'Rejected'
WHERE SubmissionID = @SubmissionID
  AND OfferedBy = 'Admin'
  AND OfferStatus = 'Pending'
```

**Parameters**: `@SubmissionID`

---

### 22. Create Admin Offer
**Purpose**: Inserts admin negotiation offer.

**Frontend Support**: Admin negotiate offer

**Query**:
```sql
INSERT INTO PriceNegotiation (
    SubmissionID, OfferedBy, OfferedPrice, 
    OfferDate, OfferMessage, OfferStatus, RoundNumber
)
VALUES (
    @SubmissionID, 'Admin', @OfferedPrice,
    CURRENT_TIMESTAMP, @OfferMessage, 'Pending', @RoundNumber
)
```

**Parameters**: `@SubmissionID`, `@OfferedPrice`, `@RoundNumber`, `@OfferMessage` (nullable)

**Follow-up Query**:
```sql
SELECT LAST_INSERT_ID()
```

---

### 23. Get Submission for Approval
**Purpose**: Retrieves submission details for approval process.

**Frontend Support**: Admin approve submission

**Query**:
```sql
SELECT 
    ss.SubmissionID,
    ss.ISBN,
    ss.Title,
    ss.Author,
    ss.Edition,
    ss.PhysicalCondition,
    ss.CourseMajor,
    ss.Status,
    ss.AskingPrice
FROM SellSubmission ss
WHERE ss.SubmissionID = @SubmissionID
```

**Parameters**: `@SubmissionID`

---

### 24. Check Book Already Created
**Purpose**: Prevents duplicate book creation from same submission.

**Frontend Support**: Admin approve submission

**Query**:
```sql
SELECT COUNT(*) as book_count
FROM Book
WHERE SubmissionID = @SubmissionID
```

**Parameters**: `@SubmissionID`

---

### 25. Get Accepted Negotiation
**Purpose**: Retrieves accepted admin offer to determine acquisition cost.

**Frontend Support**: Admin approve submission

**Query**:
```sql
SELECT OfferedPrice, NegotiationID, RoundNumber
FROM PriceNegotiation
WHERE SubmissionID = @SubmissionID
  AND OfferStatus = 'Accepted'
  AND OfferedBy = 'Admin'
ORDER BY RoundNumber DESC
LIMIT 1
```

**Parameters**: `@SubmissionID`

---

### 26. Get Pending Customer Counter-Offer
**Purpose**: Retrieves customer counter-offer to determine acquisition cost.

**Frontend Support**: Admin approve submission

**Query**:
```sql
SELECT OfferedPrice, NegotiationID, RoundNumber
FROM PriceNegotiation
WHERE SubmissionID = @SubmissionID
  AND OfferStatus = 'Pending'
  AND OfferedBy = 'User'
ORDER BY RoundNumber DESC
LIMIT 1
```

**Parameters**: `@SubmissionID`

---

### 27. Check Any Negotiations Exist
**Purpose**: Validates data consistency during approval.

**Frontend Support**: Admin approve submission

**Query**:
```sql
SELECT COUNT(*) as negotiation_count
FROM PriceNegotiation
WHERE SubmissionID = @SubmissionID
```

**Parameters**: `@SubmissionID`

---

### 28. Update Submission to Completed
**Purpose**: Marks submission as completed when book is created.

**Frontend Support**: Admin approve submission

**Query**:
```sql
UPDATE SellSubmission
SET Status = 'Completed', AdminUserID = @AdminUserID
WHERE SubmissionID = @SubmissionID
```

**Parameters**: `@SubmissionID`, `@AdminUserID`

---

### 29. Create Book from Submission
**Purpose**: Creates book record from approved submission.

**Frontend Support**: Admin approve submission

**Query**:
```sql
INSERT INTO Book (
    SubmissionID, ISBN, Title, Author, Edition,
    SellingPrice, AcquisitionCost, BookCondition,
    CourseMajor, Status, CreatedDate
)
VALUES (
    @SubmissionID, @ISBN, @Title, @Author, @Edition,
    @SellingPrice, @AcquisitionCost, @BookCondition,
    @CourseMajor, 'Available', CURRENT_TIMESTAMP
)
```

**Parameters**: `@SubmissionID`, `@ISBN`, `@Title`, `@Author`, `@Edition`, `@SellingPrice`, `@AcquisitionCost`, `@BookCondition`, `@CourseMajor` (nullable)

**Follow-up Query**:
```sql
SELECT LAST_INSERT_ID()
```

---

### 30. Verify Submission for Rejection
**Purpose**: Validates submission can be rejected.

**Frontend Support**: Admin reject submission

**Query**:
```sql
SELECT SubmissionID, Status
FROM SellSubmission
WHERE SubmissionID = @SubmissionID
```

**Parameters**: `@SubmissionID`

---

### 31. Reject Submission
**Purpose**: Updates submission status to rejected.

**Frontend Support**: Admin reject submission

**Query**:
```sql
UPDATE SellSubmission
SET Status = 'Rejected', AdminUserID = @AdminUserID
WHERE SubmissionID = @SubmissionID
```

**Parameters**: `@SubmissionID`, `@AdminUserID`

---

## Query Statistics Summary

- **Total Queries Documented**: 80+
- **Services**: 6
- **All Queries Use Parameterized Statements**: ✅ Yes
- **Transaction Support**: ✅ Yes (for critical operations)
- **SQL Injection Protection**: ✅ Yes (all queries use parameters)

---

## Key Features Demonstrated

1. **Parameterized Queries**: All queries use `@Parameter` syntax to prevent SQL injection
2. **Transaction Support**: Critical operations (order creation, cancellation, submission approval) use transactions
3. **Proper Joins**: Efficient JOIN operations for related data retrieval
4. **Aggregations**: GROUP BY, COUNT, SUM, MIN, MAX for data analysis
5. **Subqueries**: Used for calculated fields and validations
6. **Dynamic Queries**: Conditional WHERE clauses based on optional parameters
7. **Data Integrity**: Foreign key relationships and constraints enforced
8. **Error Prevention**: Validation queries before destructive operations

---

## Notes

- All queries are executed through the `DatabaseService` which handles connection management
- Queries marked with `[optional]` indicate conditional parts based on parameters
- Transaction queries ensure atomicity for multi-step operations
- All user input is sanitized through parameterized queries

