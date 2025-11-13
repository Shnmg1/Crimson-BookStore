# CrimsonBookStore Database Design Documentation

## Overview
This document provides complete information about the database schema, business logic, data derivation methods, and implementation details for AI-assisted development. Use this document to understand how the database works, how to query it correctly, and how data flows through the system.

## Core Design Principles

### Book Inventory Model
- **Each BookID = One Physical Book**: Every record in the Book table represents a single, unique physical book. This allows tracking individual book conditions, prices, and status.
- **Stock Quantity is Calculated Dynamically**: Stock quantity is NOT stored as a field. Instead, it is calculated by counting available books with the same ISBN and Edition.
  - Query: `SELECT COUNT(*) FROM Book WHERE ISBN = ? AND Edition = ? AND Status = 'Available'`
  - Frontend must query this count before allowing books to be added to cart
  - Display as "X copies available" or "Out of stock"
- **No StockQuantity Field**: Removed from Book table to enforce normalization and prevent data inconsistency
- **Status Field**: Each book has Status = 'Available' or 'Sold'
  - When sold: Status changes to 'Sold'
  - When order cancelled: Status changes back to 'Available'

### Book Uniqueness
- **Multiple books can share ISBN+Edition**: The unique constraint on (ISBN, Edition) has been removed. This allows the bookstore to have multiple physical copies of the same book.
- **Each physical book gets its own BookID**: This allows tracking individual book conditions, acquisition costs, and selling prices, even for books with the same ISBN and Edition.

## Table Structures

### User Table
**Purpose**: Stores user account information for both customers and administrators.

**Fields**:
- `UserID` (PK): Unique identifier for each user
- `Username`: Unique username for login
- `Email`: Unique email address
- `Password`: Hashed password (should be hashed in application layer)
- `FirstName`: User's first name
- `LastName`: User's last name
- `Phone`: User's phone number (optional)
- `Address`: User's address (optional, TEXT field)
- `UserType`: Either 'Customer' or 'Admin'
- `CreatedDate`: Account creation timestamp

**Relationships**:
- One-to-Many with PurchaseOrder
- One-to-Many with SellSubmission
- One-to-Many with ShoppingCart
- One-to-Many with PaymentMethod

**Indexes**: Username, Email, UserType for efficient lookups

### Book Table
**Purpose**: Stores the main inventory of books available for sale.

**Fields**:
- `BookID` (PK): Unique identifier for each physical book
- `SubmissionID` (FK, nullable): Links to SellSubmission that created this book (if applicable)
- `ISBN`: International Standard Book Number
- `Title`: Book title
- `Author`: Book author
- `Edition`: Book edition
- `SellingPrice`: Price at which the book is sold to customers
- `AcquisitionCost`: Price the bookstore paid to acquire the book
- `BookCondition`: Physical condition ('New', 'Good', 'Fair')
- `CourseMajor`: Associated course or major (optional)
- `Status`: 'Available' or 'Sold'
- `CreatedDate`: When book was added to inventory

**Constraints**:
- `SellingPrice > 0`: Must have a positive selling price
- `AcquisitionCost >= 0`: Acquisition cost cannot be negative
- `SellingPrice > AcquisitionCost`: Business rule ensuring profit margin

**Relationships**:
- Many-to-One with SellSubmission (via SubmissionID)
- One-to-Many with OrderLineItem
- One-to-Many with ShoppingCart

**Indexes**: 
- ISBN, Status, CourseMajor for search/filtering
- Composite index on (ISBN, Edition, Status) for efficient stock counting queries

**Important Notes**:
- Multiple Book records can have the same ISBN+Edition (multiple physical copies)
- Stock count must be calculated, not read from a field
- When a book is sold, Status changes to 'Sold'
- When an order is cancelled, Status changes back to 'Available'

### SellSubmission Table
**Purpose**: Tracks customer submissions to sell books to the bookstore.

**Fields**:
- `SubmissionID` (PK): Unique identifier for each submission
- `UserID` (FK): Customer who submitted the book
- `AdminUserID` (FK, nullable): Admin who reviewed/approved the submission
- `ISBN`: Book ISBN
- `Title`: Book title
- `Author`: Book author
- `Edition`: Book edition
- `PhysicalCondition`: Condition of the physical book ('New', 'Good', 'Fair')
- `CourseMajor`: Associated course or major (optional)
- `AskingPrice`: Price the customer wants to sell the book for
- `Status`: 'Pending Review', 'Approved', or 'Rejected'
- `SubmissionDate`: When the submission was created

**Relationships**:
- Many-to-One with User (customer)
- Many-to-One with User (admin reviewer)
- One-to-Many with PriceNegotiation
- One-to-Many with Book (via SubmissionID)

**Workflow**:
1. Customer creates submission with Status='Pending Review'
2. Admin reviews and negotiates price via PriceNegotiation table
3. When approved, Book record(s) are created with SubmissionID linking back
4. When rejected, no Book records are created

### PriceNegotiation Table
**Purpose**: Tracks multi-round price negotiations between customers and admins.

**Fields**:
- `NegotiationID` (PK): Unique identifier for each negotiation round
- `SubmissionID` (FK): The SellSubmission being negotiated
- `OfferedBy`: Who made this offer ('User' or 'Admin')
- `OfferedPrice`: The price being offered
- `OfferDate`: When the offer was made
- `OfferMessage`: Optional message (e.g., "Can you go lower?")
- `OfferStatus`: 'Pending', 'Accepted', or 'Rejected'
- `RoundNumber`: Sequential number for this negotiation round (1, 2, 3, etc.)

**Relationships**:
- Many-to-One with SellSubmission

**Workflow**:
1. Customer submits SellSubmission with AskingPrice
2. Admin creates PriceNegotiation entry (RoundNumber=1, OfferedBy='Admin')
3. Customer can:
   - Accept: Set OfferStatus='Accepted', update SellSubmission.Status='Approved', create Book record
   - Reject: Set OfferStatus='Rejected', update SellSubmission.Status='Rejected'
   - Counter: Create new PriceNegotiation entry (RoundNumber=2, OfferedBy='User')
4. Process continues until acceptance or final rejection
5. Final accepted price becomes AcquisitionCost in Book table

**Important Notes**:
- RoundNumber should increment sequentially per SubmissionID
- Only one offer should have OfferStatus='Accepted' per SubmissionID
- When accepted, the final price is used as AcquisitionCost when creating Book record

### PurchaseOrder Table
**Purpose**: Tracks customer orders to buy books from the bookstore.

**Fields**:
- `OrderID` (PK): Unique identifier for each order
- `UserID` (FK): Customer who placed the order
- `OrderDate`: When the order was placed
- `Status`: 'New', 'Processing', 'Fulfilled', or 'Cancelled'
- `TotalAmount`: Total cost of the order (sum of all line items)

**Status Flow**:
- New: Order just created
- Processing: Admin is processing the order
- Fulfilled: Order has been fulfilled (shipped/completed)
- Cancelled: Order was cancelled (books must be restocked)

**Relationships**:
- Many-to-One with User
- One-to-Many with OrderLineItem
- One-to-Many with Payment

**Important Notes**:
- Status transitions: New → Processing → Fulfilled (or Cancelled)
- When cancelled, all associated Book.Status must change back to 'Available'
- TotalAmount should be calculated from OrderLineItem prices

### OrderLineItem Table
**Purpose**: Links specific books to purchase orders.

**Fields**:
- `LineItemID` (PK): Unique identifier for each line item
- `OrderID` (FK): The purchase order this item belongs to
- `BookID` (FK): The specific book being purchased
- `PriceAtSale`: The price of the book at the time of sale (snapshot)

**Important Notes**:
- **NO Quantity field**: Each line item represents ONE unique book
- If a customer wants multiple copies, create multiple OrderLineItem records
- PriceAtSale captures the price at time of sale (book's SellingPrice may change later)
- When order is created, Book.Status changes to 'Sold' for all books in order
- When order is cancelled, Book.Status changes back to 'Available' for all books

**Relationships**:
- Many-to-One with PurchaseOrder
- Many-to-One with Book

### ShoppingCart Table
**Purpose**: Persists user shopping carts in the database.

**Fields**:
- `CartItemID` (PK): Unique identifier for each cart item
- `UserID` (FK): User who owns this cart
- `BookID` (FK): Book in the cart
- `AddedDate`: When the book was added to cart

**Constraints**:
- UNIQUE constraint on (UserID, BookID): Prevents duplicate items in cart

**Relationships**:
- Many-to-One with User
- Many-to-One with Book

**Workflow**:
1. User adds book to cart: INSERT into ShoppingCart
2. User views cart: SELECT from ShoppingCart JOIN Book
3. User removes item: DELETE from ShoppingCart
4. User checks out: Create PurchaseOrder and OrderLineItems, then DELETE from ShoppingCart

**Important Notes**:
- Cart persists across browser sessions
- Must validate book is still Available before checkout
- Cart is cleared automatically on successful checkout

### PaymentMethod Table
**Purpose**: Stores saved payment methods per user (for demo purposes).

**Fields**:
- `PaymentMethodID` (PK): Unique identifier for each payment method
- `UserID` (FK): User who owns this payment method
- `CardType`: Type of card ('Visa', 'MasterCard', 'American Express', etc.)
- `LastFourDigits`: Last 4 digits of card number (for display)
- `ExpirationDate`: Card expiration (format: MM/YYYY)
- `IsDefault`: Whether this is the user's default payment method
- `CreatedDate`: When payment method was added

**Relationships**:
- Many-to-One with User
- One-to-Many with Payment

**Important Notes**:
- This is for demo purposes only - no real credit card data should be stored
- Only store mock/safe data (last 4 digits, card type)
- IsDefault allows users to have a preferred payment method

### Payment Table
**Purpose**: Records payment transactions for orders.

**Fields**:
- `PaymentID` (PK): Unique identifier for each payment
- `OrderID` (FK): Order being paid for
- `PaymentMethodID` (FK, nullable): Saved payment method used (if applicable)
- `PaymentDate`: When payment was processed
- `Amount`: Payment amount
- `PaymentStatus`: 'Pending', 'Completed', 'Failed', or 'Refunded'
- `TransactionID`: Optional transaction identifier

**Relationships**:
- Many-to-One with PurchaseOrder
- Many-to-One with PaymentMethod (optional)

**Important Notes**:
- PaymentMethodID is nullable (allows one-time payments without saving method)
- PaymentStatus='Completed' is default (simulated payment for demo)
- Amount should match PurchaseOrder.TotalAmount

## Business Logic & Workflows

### Inventory Management

#### Adding Books to Inventory
**When**: SellSubmission is approved after price negotiation

**Process**:
1. Final price is agreed upon in PriceNegotiation (OfferStatus='Accepted')
2. Update SellSubmission.Status = 'Approved'
3. Create Book record with:
   - SubmissionID = SellSubmission.SubmissionID
   - AcquisitionCost = accepted price from PriceNegotiation
   - SellingPrice = set by admin (must be > AcquisitionCost)
   - Status = 'Available'
   - All other fields from SellSubmission

**SQL Example**:
```sql
-- After negotiation is accepted
INSERT INTO Book (
    SubmissionID, ISBN, Title, Author, Edition, 
    SellingPrice, AcquisitionCost, BookCondition, 
    CourseMajor, Status
) VALUES (
    ?, ?, ?, ?, ?,
    ?, ?, ?, -- SellingPrice set by admin, AcquisitionCost from negotiation
    ?, ?, 'Available'
);
```

#### Selling Books (Order Processing)
**When**: Customer completes checkout

**Process**:
1. Create PurchaseOrder record (Status='New')
2. For each book in cart:
   - Create OrderLineItem with BookID and PriceAtSale
   - Update Book.Status = 'Sold' for that BookID
3. Calculate and update PurchaseOrder.TotalAmount
4. Create Payment record
5. Clear ShoppingCart for user

**SQL Example**:
```sql
-- Start transaction
BEGIN;

-- Create order
INSERT INTO PurchaseOrder (UserID, Status, TotalAmount) 
VALUES (?, 'New', 0);

SET @OrderID = LAST_INSERT_ID();

-- For each book in cart
INSERT INTO OrderLineItem (OrderID, BookID, PriceAtSale)
SELECT @OrderID, BookID, SellingPrice
FROM ShoppingCart sc
JOIN Book b ON sc.BookID = b.BookID
WHERE sc.UserID = ? AND b.Status = 'Available';

-- Update book statuses
UPDATE Book
SET Status = 'Sold'
WHERE BookID IN (
    SELECT BookID FROM ShoppingCart WHERE UserID = ?
);

-- Calculate total
UPDATE PurchaseOrder
SET TotalAmount = (
    SELECT SUM(PriceAtSale) 
    FROM OrderLineItem 
    WHERE OrderID = @OrderID
)
WHERE OrderID = @OrderID;

-- Create payment
INSERT INTO Payment (OrderID, PaymentMethodID, Amount, PaymentStatus)
VALUES (@OrderID, ?, (SELECT TotalAmount FROM PurchaseOrder WHERE OrderID = @OrderID), 'Completed');

-- Clear cart
DELETE FROM ShoppingCart WHERE UserID = ?;

COMMIT;
```

#### Cancelling Orders (Restocking)
**When**: Admin cancels a PurchaseOrder

**Process**:
1. Update PurchaseOrder.Status = 'Cancelled'
2. For all OrderLineItems in the order:
   - Update Book.Status = 'Available' for each BookID

**SQL Example**:
```sql
-- Update order status
UPDATE PurchaseOrder
SET Status = 'Cancelled'
WHERE OrderID = ?;

-- Restock books
UPDATE Book
SET Status = 'Available'
WHERE BookID IN (
    SELECT BookID FROM OrderLineItem WHERE OrderID = ?
);
```

### Price Negotiation Flow

**Complete Workflow**:

1. **Customer Submits Book**:
   ```sql
   INSERT INTO SellSubmission (UserID, ISBN, Title, Author, Edition, 
                               PhysicalCondition, CourseMajor, AskingPrice, Status)
   VALUES (?, ?, ?, ?, ?, ?, ?, ?, 'Pending Review');
   ```

2. **Admin Reviews and Makes Counter-Offer**:
   ```sql
   INSERT INTO PriceNegotiation (SubmissionID, OfferedBy, OfferedPrice, 
                                 OfferStatus, RoundNumber)
   VALUES (?, 'Admin', ?, 'Pending', 1);
   ```

3. **Customer Responds** (Accept, Reject, or Counter):
   
   **Accept**:
   ```sql
   UPDATE PriceNegotiation
   SET OfferStatus = 'Accepted'
   WHERE NegotiationID = ?;
   
   UPDATE SellSubmission
   SET Status = 'Approved', AdminUserID = ?
   WHERE SubmissionID = ?;
   
   -- Create Book record with AcquisitionCost = accepted price
   INSERT INTO Book (SubmissionID, ISBN, Title, Author, Edition, 
                     AcquisitionCost, SellingPrice, BookCondition, CourseMajor, Status)
   SELECT ss.SubmissionID, ss.ISBN, ss.Title, ss.Author, ss.Edition,
          pn.OfferedPrice, ?, ss.PhysicalCondition, ss.CourseMajor, 'Available'
   FROM SellSubmission ss
   JOIN PriceNegotiation pn ON ss.SubmissionID = pn.SubmissionID
   WHERE ss.SubmissionID = ? AND pn.OfferStatus = 'Accepted';
   ```
   
   **Reject**:
   ```sql
   UPDATE PriceNegotiation
   SET OfferStatus = 'Rejected'
   WHERE NegotiationID = ?;
   
   UPDATE SellSubmission
   SET Status = 'Rejected', AdminUserID = ?
   WHERE SubmissionID = ?;
   ```
   
   **Counter**:
   ```sql
   -- Get next round number
   SELECT COALESCE(MAX(RoundNumber), 0) + 1 
   FROM PriceNegotiation 
   WHERE SubmissionID = ?;
   
   INSERT INTO PriceNegotiation (SubmissionID, OfferedBy, OfferedPrice, 
                                 OfferStatus, RoundNumber)
   VALUES (?, 'User', ?, 'Pending', ?);
   ```

4. **Process Repeats** until acceptance or final rejection

### Shopping Cart Operations

#### Add to Cart
**Validation**: Check that book is available and stock count > 0

```sql
-- Check if book is available
SELECT COUNT(*) as available_count
FROM Book
WHERE ISBN = ? AND Edition = ? AND Status = 'Available';

-- If available_count > 0, add to cart
INSERT INTO ShoppingCart (UserID, BookID)
SELECT ?, BookID
FROM Book
WHERE ISBN = ? AND Edition = ? AND Status = 'Available'
LIMIT 1
ON DUPLICATE KEY UPDATE AddedDate = CURRENT_TIMESTAMP;
```

**Important**: The UNIQUE constraint on (UserID, BookID) prevents duplicates. If book already in cart, it updates the AddedDate.

#### View Cart
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
    b.CourseMajor
FROM ShoppingCart sc
JOIN Book b ON sc.BookID = b.BookID
WHERE sc.UserID = ? AND b.Status = 'Available'
ORDER BY sc.AddedDate DESC;
```

**Note**: Only shows books that are still available. If a book was sold between adding to cart and viewing, it won't appear.

#### Remove from Cart
```sql
DELETE FROM ShoppingCart
WHERE CartItemID = ? AND UserID = ?;
```

#### Checkout (See "Selling Books" section above)

### Order Processing Workflow

1. **Order Creation** (Status='New'):
   - Created during checkout process
   - TotalAmount calculated from line items

2. **Admin Processing** (Status='Processing'):
   ```sql
   UPDATE PurchaseOrder
   SET Status = 'Processing'
   WHERE OrderID = ?;
   ```

3. **Fulfillment** (Status='Fulfilled'):
   ```sql
   UPDATE PurchaseOrder
   SET Status = 'Fulfilled'
   WHERE OrderID = ?;
   ```

4. **Cancellation** (Status='Cancelled'):
   - See "Cancelling Orders" section above

## Data Derivation

### Stock Count Calculation

**How to Calculate Stock**:
Stock is calculated by counting available books with the same ISBN and Edition.

**Basic Query**:
```sql
SELECT COUNT(*) as stock_count
FROM Book
WHERE ISBN = ? AND Edition = ? AND Status = 'Available';
```

**For Display (All Books with Stock Counts)**:
```sql
SELECT 
    ISBN,
    Edition,
    Title,
    Author,
    MIN(SellingPrice) as min_price,
    MAX(SellingPrice) as max_price,
    COUNT(*) as available_count,
    MIN(BookCondition) as condition_range -- or use GROUP_CONCAT for all conditions
FROM Book
WHERE Status = 'Available'
GROUP BY ISBN, Edition, Title, Author
HAVING available_count > 0
ORDER BY Title, Edition;
```

**For Single Book Display**:
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
    (SELECT COUNT(*) 
     FROM Book b2 
     WHERE b2.ISBN = b.ISBN 
       AND b2.Edition = b.Edition 
       AND b2.Status = 'Available') as available_count
FROM Book b
WHERE b.BookID = ? AND b.Status = 'Available';
```

### Book Listing with Aggregate Information

**Display Books for Browse/Search**:
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
    MIN(CourseMajor) as course_major -- or GROUP_CONCAT if multiple
FROM Book
WHERE Status = 'Available'
GROUP BY ISBN, Edition, Title, Author
HAVING available_count > 0
ORDER BY Title, Edition;
```

**With Search Filters**:
```sql
SELECT 
    ISBN,
    Edition,
    Title,
    Author,
    MIN(SellingPrice) as min_price,
    MAX(SellingPrice) as max_price,
    COUNT(*) as available_count
FROM Book
WHERE Status = 'Available'
  AND (Title LIKE ? OR Author LIKE ? OR ISBN = ? OR CourseMajor = ?)
GROUP BY ISBN, Edition, Title, Author
HAVING available_count > 0
ORDER BY Title, Edition;
```

### Cart Validation

**Before Adding to Cart**:
```sql
-- Check if any copies are available
SELECT COUNT(*) as available_count
FROM Book
WHERE ISBN = ? AND Edition = ? AND Status = 'Available';

-- If available_count > 0, proceed with add to cart
```

**Before Checkout**:
```sql
-- Verify all cart items are still available
SELECT COUNT(*) as unavailable_count
FROM ShoppingCart sc
JOIN Book b ON sc.BookID = b.BookID
WHERE sc.UserID = ? AND b.Status != 'Available';

-- If unavailable_count > 0, inform user and remove unavailable items
DELETE FROM ShoppingCart
WHERE UserID = ? AND BookID IN (
    SELECT BookID FROM Book WHERE Status != 'Available'
);
```

### Order History

**Get User's Order History**:
```sql
SELECT 
    po.OrderID,
    po.OrderDate,
    po.Status,
    po.TotalAmount,
    COUNT(oli.LineItemID) as item_count
FROM PurchaseOrder po
LEFT JOIN OrderLineItem oli ON po.OrderID = oli.OrderID
WHERE po.UserID = ?
GROUP BY po.OrderID, po.OrderDate, po.Status, po.TotalAmount
ORDER BY po.OrderDate DESC;
```

**Get Order Details**:
```sql
SELECT 
    po.OrderID,
    po.OrderDate,
    po.Status,
    po.TotalAmount,
    b.ISBN,
    b.Title,
    b.Author,
    b.Edition,
    oli.PriceAtSale
FROM PurchaseOrder po
JOIN OrderLineItem oli ON po.OrderID = oli.OrderID
JOIN Book b ON oli.BookID = b.BookID
WHERE po.OrderID = ?
ORDER BY b.Title;
```

## Frontend Integration Notes

### Stock Display

**Display Format**:
- If stock_count > 0: "X copies available"
- If stock_count = 0: "Out of stock" (disable Add to Cart button)

**Implementation**:
1. Query stock count for each book (ISBN+Edition combination)
2. Display count next to book information
3. Disable "Add to Cart" if count = 0
4. Update display when books are added/removed from cart

### Cart Management

**Load Cart**:
- Query ShoppingCart JOIN Book WHERE UserID = ? AND Book.Status = 'Available'
- Display each item with book details and price
- Calculate total: SUM(SellingPrice) from cart items

**Add to Cart**:
1. Validate book is available (stock count > 0)
2. Check if already in cart (UNIQUE constraint will prevent duplicates)
3. INSERT into ShoppingCart
4. Refresh cart display

**Remove from Cart**:
- DELETE from ShoppingCart WHERE CartItemID = ?
- Refresh cart display

**Update Quantities**:
- Not applicable - each book is unique
- If user wants multiple copies, they add the same ISBN+Edition multiple times (different BookIDs)

### Search & Browse

**Browse All Available Books**:
- Query books grouped by ISBN+Edition with stock counts
- Filter: WHERE Status = 'Available'
- Group by: ISBN, Edition
- Display aggregate information (min/max price, available count)

**Search Functionality**:
- Search by: Title, Author, ISBN, CourseMajor
- Use LIKE for partial matches on Title/Author
- Use exact match for ISBN
- Filter results to only show available books

**Detailed Book View**:
- Show individual book details
- Display stock count for that ISBN+Edition
- Show all available copies (if user wants to see individual books)
- Allow adding specific book (BookID) to cart

### Required Validations

**Before Database Operations**:

1. **Add to Cart**:
   - Verify user is logged in
   - Verify book exists and Status = 'Available'
   - Verify stock count > 0
   - Check if already in cart (optional - UNIQUE constraint handles this)

2. **Checkout**:
   - Verify cart is not empty
   - Verify all cart items are still available
   - Verify user has selected payment method
   - Calculate total amount

3. **Sell Submission**:
   - Verify user is logged in and is Customer
   - Verify all required fields are provided
   - Verify AskingPrice > 0

4. **Price Negotiation**:
   - Verify user is Admin (for admin offers)
   - Verify user is submission owner (for customer responses)
   - Verify submission Status = 'Pending Review'
   - Verify RoundNumber is sequential

5. **Order Status Updates**:
   - Verify user is Admin
   - Verify order exists
   - Verify status transition is valid (New → Processing → Fulfilled, or → Cancelled)

## Query Examples

### Stock Counting Queries

**Get Stock Count for Specific Book**:
```sql
SELECT COUNT(*) as stock_count
FROM Book
WHERE ISBN = '978-0123456789' 
  AND Edition = '5th' 
  AND Status = 'Available';
```

**Get Stock Counts for All Books**:
```sql
SELECT 
    ISBN,
    Edition,
    Title,
    COUNT(*) as stock_count
FROM Book
WHERE Status = 'Available'
GROUP BY ISBN, Edition, Title
ORDER BY Title, Edition;
```

**Low Stock Alert** (less than 3 copies):
```sql
SELECT 
    ISBN,
    Edition,
    Title,
    COUNT(*) as stock_count
FROM Book
WHERE Status = 'Available'
GROUP BY ISBN, Edition, Title
HAVING stock_count < 3
ORDER BY stock_count ASC;
```

### Book Listing Queries

**List All Available Books with Details**:
```sql
SELECT 
    ISBN,
    Edition,
    Title,
    Author,
    MIN(SellingPrice) as min_price,
    MAX(SellingPrice) as max_price,
    COUNT(*) as available_count,
    GROUP_CONCAT(DISTINCT BookCondition) as conditions
FROM Book
WHERE Status = 'Available'
GROUP BY ISBN, Edition, Title, Author
ORDER BY Title, Edition;
```

**Search Books**:
```sql
SELECT 
    ISBN,
    Edition,
    Title,
    Author,
    MIN(SellingPrice) as min_price,
    COUNT(*) as available_count
FROM Book
WHERE Status = 'Available'
  AND (Title LIKE '%database%' 
       OR Author LIKE '%database%' 
       OR ISBN = '978-0123456789'
       OR CourseMajor = 'MIS 301')
GROUP BY ISBN, Edition, Title, Author
ORDER BY Title;
```

### Cart Queries

**Get User's Shopping Cart**:
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
    b.CourseMajor
FROM ShoppingCart sc
JOIN Book b ON sc.BookID = b.BookID
WHERE sc.UserID = 1 AND b.Status = 'Available'
ORDER BY sc.AddedDate DESC;
```

**Get Cart Total**:
```sql
SELECT SUM(b.SellingPrice) as cart_total
FROM ShoppingCart sc
JOIN Book b ON sc.BookID = b.BookID
WHERE sc.UserID = 1 AND b.Status = 'Available';
```

**Check if Book is in Cart**:
```sql
SELECT COUNT(*) as in_cart
FROM ShoppingCart
WHERE UserID = 1 AND BookID = 123;
```

### Order History Queries

**User's Order History**:
```sql
SELECT 
    po.OrderID,
    po.OrderDate,
    po.Status,
    po.TotalAmount,
    COUNT(oli.LineItemID) as item_count
FROM PurchaseOrder po
LEFT JOIN OrderLineItem oli ON po.OrderID = oli.OrderID
WHERE po.UserID = 1
GROUP BY po.OrderID, po.OrderDate, po.Status, po.TotalAmount
ORDER BY po.OrderDate DESC;
```

**Order Details**:
```sql
SELECT 
    po.OrderID,
    po.OrderDate,
    po.Status,
    po.TotalAmount,
    b.ISBN,
    b.Title,
    b.Author,
    b.Edition,
    oli.PriceAtSale
FROM PurchaseOrder po
JOIN OrderLineItem oli ON po.OrderID = oli.OrderID
JOIN Book b ON oli.BookID = b.BookID
WHERE po.OrderID = 100
ORDER BY b.Title;
```

### Price Negotiation Queries

**Get Negotiation History for Submission**:
```sql
SELECT 
    pn.NegotiationID,
    pn.OfferedBy,
    pn.OfferedPrice,
    pn.OfferDate,
    pn.OfferMessage,
    pn.OfferStatus,
    pn.RoundNumber
FROM PriceNegotiation pn
WHERE pn.SubmissionID = 50
ORDER BY pn.RoundNumber ASC;
```

**Get Pending Negotiations**:
```sql
SELECT 
    ss.SubmissionID,
    ss.Title,
    ss.AskingPrice,
    ss.Status,
    pn.OfferedPrice as last_offer,
    pn.OfferedBy as last_offer_by,
    pn.RoundNumber
FROM SellSubmission ss
LEFT JOIN PriceNegotiation pn ON ss.SubmissionID = pn.SubmissionID
WHERE ss.Status = 'Pending Review'
  AND (pn.OfferStatus = 'Pending' OR pn.OfferStatus IS NULL)
ORDER BY ss.SubmissionDate ASC;
```

## Constraints & Validation Rules

### Database-Level Constraints

1. **Book Table**:
   - `SellingPrice > 0`: Must have positive selling price
   - `AcquisitionCost >= 0`: Acquisition cost cannot be negative
   - `SellingPrice > AcquisitionCost`: Business rule ensuring profit margin

2. **SellSubmission Table**:
   - `AskingPrice > 0`: Asking price must be positive

3. **PriceNegotiation Table**:
   - `OfferedPrice > 0`: Offered price must be positive

4. **OrderLineItem Table**:
   - `PriceAtSale > 0`: Sale price must be positive

5. **Payment Table**:
   - `Amount > 0`: Payment amount must be positive

6. **PurchaseOrder Table**:
   - `TotalAmount >= 0`: Total cannot be negative

7. **ShoppingCart Table**:
   - UNIQUE constraint on (UserID, BookID): Prevents duplicate items

### Application-Level Validations

1. **Stock Validation**:
   - Before adding to cart: Verify stock count > 0
   - Before checkout: Verify all cart items are still available

2. **Price Negotiation**:
   - RoundNumber must increment sequentially per SubmissionID
   - Only one offer can have OfferStatus='Accepted' per SubmissionID
   - Customer can only respond to Admin offers, and vice versa

3. **Order Status Transitions**:
   - Valid transitions: New → Processing → Fulfilled
   - Valid cancellation: New/Processing → Cancelled
   - Invalid: Cannot go backwards (Fulfilled → Processing)

4. **User Permissions**:
   - Only Admins can: Approve/Reject submissions, update order status, manage inventory
   - Only Customers can: Create sell submissions, place orders, manage their own cart

5. **Data Integrity**:
   - When Book is created from SellSubmission, all required fields must be populated
   - When Order is created, TotalAmount must match sum of OrderLineItem prices
   - When Order is cancelled, all associated Books must be restocked

## Important Implementation Notes

### Transaction Management

**Critical Operations Requiring Transactions**:

1. **Checkout Process**:
   - Must be atomic: Create order, update book statuses, create payment, clear cart
   - If any step fails, rollback all changes

2. **Order Cancellation**:
   - Update order status and restock all books atomically

3. **Price Negotiation Acceptance**:
   - Update negotiation status, update submission status, create book record atomically

### Performance Considerations

1. **Stock Counting**:
   - Use the composite index on (ISBN, Edition, Status) for efficient counting
   - Consider caching stock counts for frequently viewed books

2. **Cart Queries**:
   - Index on (UserID, BookID) ensures fast cart lookups
   - Join with Book table uses BookID index

3. **Search Queries**:
   - Indexes on ISBN, Title (consider full-text), Author, CourseMajor improve search performance

### Error Handling

1. **Stock Depletion**:
   - Between cart addition and checkout, book may be sold
   - Always verify availability before checkout
   - Inform user if items become unavailable

2. **Duplicate Cart Items**:
   - UNIQUE constraint prevents duplicates
   - Handle constraint violation gracefully (inform user item already in cart)

3. **Foreign Key Violations**:
   - Cannot delete User with active orders (RESTRICT)
   - Cannot delete Book that's in an order (RESTRICT)
   - Cart items cascade delete when user/book is deleted

## Summary

This database design supports a complete bookstore management system with:
- Dynamic stock counting (no stored quantity field)
- Multi-round price negotiations
- Persistent shopping carts
- Saved payment methods
- Complete order tracking
- Individual book tracking (each BookID = one physical book)

Key principles to remember:
- Stock is calculated, not stored
- Each book is unique (BookID)
- Cart persists in database
- Orders track individual books (no quantity field)
- Negotiations support unlimited rounds
- All critical operations should use transactions

Use this document as a reference when implementing frontend features, writing queries, or making database changes.

