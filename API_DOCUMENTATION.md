# API Documentation

## Base URL
```
http://localhost:5000/api
```
or
```
https://localhost:5001/api
```

## Authentication

User authentication is handled via token-based authentication using in-memory session storage.

**How it works:**
1. User registers or logs in via `/api/auth/register` or `/api/auth/login`
2. API returns a session token (GUID) in the response
3. Client stores the token in localStorage
4. Client includes the token in the `Authorization` header for subsequent requests
5. Server validates the token and retrieves user information from session storage
6. Sessions expire after 24 hours

**Headers** (when authenticated):
```
Authorization: Bearer {token}
```

**Getting Current User in Controllers:**
Controllers can use the `AuthHelper.GetCurrentUser()` method to retrieve the authenticated user:
```csharp
var currentUser = AuthHelper.GetCurrentUser(Request, _sessionService);
if (currentUser == null)
{
    return Unauthorized(new { success = false, error = "Not authenticated", statusCode = 401 });
}
```

**Session Management:**
- Sessions are stored in-memory (server restart clears all sessions)
- Each session token is a GUID
- Sessions automatically expire after 24 hours
- Expired sessions are cleaned up automatically

---

## Response Format

All API responses follow this structure:

**Success Response:**
```json
{
  "success": true,
  "data": { ... },
  "message": "Optional success message"
}
```

**Error Response:**
```json
{
  "success": false,
  "error": "Error message",
  "statusCode": 400
}
```

---

## Endpoints

### Authentication

#### POST `/api/auth/register`
Register a new user account.

**Request Body:**
```json
{
  "username": "johndoe",
  "email": "john@example.com",
  "password": "securepassword",
  "firstName": "John",
  "lastName": "Doe",
  "phone": "205-555-1234",
  "address": "123 Main St, Tuscaloosa, AL",
  "userType": "Customer"
}
```

**Field Requirements:**
- `username` (required): Unique username, must not be empty
- `email` (required): Unique email address, must not be empty
- `password` (required): Password (stored as plain text for demo purposes)
- `firstName` (required): User's first name
- `lastName` (required): User's last name
- `phone` (optional): Phone number
- `address` (optional): User's address
- `userType` (optional): Either "Customer" or "Admin", defaults to "Customer"

**Response:**
```json
{
  "success": true,
  "data": {
    "userId": 1,
    "username": "johndoe",
    "email": "john@example.com",
    "userType": "Customer"
  }
}
```

**Status Codes:**
- `201 Created` - User registered successfully
- `400 Bad Request` - Validation error:
  - Missing required fields
  - Username already exists
  - Email already exists
  - Invalid UserType (must be "Customer" or "Admin")
- `500 Internal Server Error` - Server error

---

#### POST `/api/auth/login`
Authenticate user and create session token.

**Request Body:**
```json
{
  "username": "johndoe",
  "password": "securepassword"
}
```

**Field Requirements:**
- `username` (required): User's username
- `password` (required): User's password (plain text comparison for demo purposes)

**Response:**
```json
{
  "success": true,
  "data": {
    "userId": 1,
    "username": "johndoe",
    "userType": "Customer",
    "token": "550e8400-e29b-41d4-a716-446655440000"
  }
}
```

**Status Codes:**
- `200 OK` - Login successful, token returned
- `400 Bad Request` - Missing username or password
- `401 Unauthorized` - Invalid credentials (username not found or password incorrect)
- `500 Internal Server Error` - Server error

**Note:** The token is a GUID that should be stored by the client and included in the `Authorization: Bearer {token}` header for subsequent requests.

---

#### POST `/api/auth/logout`
Log out current user and invalidate session token.

**Headers:**
```
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "message": "Logged out successfully"
}
```

**Status Codes:**
- `200 OK` - Logout successful
- `500 Internal Server Error` - Server error

**Note:** Client should remove the token from localStorage after logout.

---

### Books

#### GET `/api/books`
Get list of all available books with stock counts. Books are grouped by ISBN and Edition.

**Query Parameters:**
- `search` (optional) - Search by title, author, ISBN, or course/major (partial match)
- `isbn` (optional) - Filter by exact ISBN
- `courseMajor` (optional) - Filter by exact course/major
- `page` (optional) - Page number (default: 1)
- `pageSize` (optional) - Items per page (default: 20)

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "isbn": "978-0123456789",
      "edition": "5th",
      "title": "Database Systems",
      "author": "John Smith",
      "minPrice": 45.99,
      "maxPrice": 55.99,
      "availableCount": 3,
      "availableBookIds": [1, 2, 3],
      "availableConditions": ["New", "Good"],
      "courseMajor": "MIS 301"
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 30,
    "totalPages": 2
  }
}
```

**Important Notes:**
- Books are **grouped by ISBN + Edition** (multiple physical copies shown as one entry)
- `availableCount` is calculated dynamically: `COUNT(*) WHERE ISBN=? AND Edition=? AND Status='Available'`
- `availableBookIds` is an array of BookID values for all available copies of this ISBN+Edition group
- `minPrice` and `maxPrice` show the price range for all available copies
- `availableConditions` lists all available conditions (New, Good, Fair)
- Only books with `Status = 'Available'` are returned
- Pagination is performed in-memory after grouping (simple implementation for school project)

**Status Codes:**
- `200 OK` - Success
- `500 Internal Server Error` - Server error

---

#### GET `/api/books/{bookId}`
Get detailed information about a specific book by its BookID.

**Response:**
```json
{
  "success": true,
  "data": {
    "bookId": 1,
    "isbn": "978-0123456789",
    "title": "Database Systems",
    "author": "John Smith",
    "edition": "5th",
    "sellingPrice": 49.99,
    "bookCondition": "Good",
    "courseMajor": "MIS 301",
    "status": "Available",
    "availableCount": 3
  }
}
```

**Important Notes:**
- Returns a single book by its unique `BookID`
- `availableCount` is calculated dynamically for all books with the same ISBN and Edition
- Only returns books with `Status = 'Available'`
- If book is not available or not found, returns 404

**Status Codes:**
- `200 OK` - Success
- `404 Not Found` - Book not found or not available
- `500 Internal Server Error` - Server error

---

#### GET `/api/books/search`
Search books by title, author, ISBN, or course/major.

**Query Parameters:**
- `q` (required) - Search term

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "isbn": "978-0123456789",
      "edition": "5th",
      "title": "Database Systems",
      "author": "John Smith",
      "minPrice": 45.99,
      "maxPrice": 55.99,
      "availableCount": 3,
      "availableConditions": ["New", "Good"],
      "courseMajor": "MIS 301"
    }
  ]
}
```

**Status Codes:**
- `200 OK` - Success
- `400 Bad Request` - Search term is required
- `500 Internal Server Error` - Server error

---

#### GET `/api/books/stock/{isbn}/{edition}`
Get stock count for a specific ISBN and edition.

**Response:**
```json
{
  "success": true,
  "data": {
    "isbn": "978-0123456789",
    "edition": "5th",
    "stockCount": 3
  }
}
```

**Status Codes:**
- `200 OK` - Success (returns stockCount: 0 if not found)
- `500 Internal Server Error` - Server error

**Note:** Stock count is calculated dynamically by counting available books with matching ISBN and Edition.

---

#### GET `/api/books/copies/{isbn}/{edition}`
Get individual book copies for a specific ISBN and edition. Returns all available copies with their BookID, price, and condition.

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "bookId": 1,
      "price": 45.99,
      "condition": "New"
    },
    {
      "bookId": 2,
      "price": 49.99,
      "condition": "Good"
    },
    {
      "bookId": 3,
      "price": 55.99,
      "condition": "New"
    }
  ]
}
```

**Field Descriptions:**
- `bookId`: Unique BookID for this specific physical copy
- `price`: Selling price for this copy
- `condition`: Book condition (New, Good, Fair)

**Status Codes:**
- `200 OK` - Success (returns empty array if no copies available)
- `500 Internal Server Error` - Server error

**Note:** Copies are ordered by condition (New, Good, Fair) and then by price (ascending). This endpoint is useful for allowing users to select a specific copy when multiple copies are available.

---

#### POST `/api/books` (Admin Only)
Add a new book to inventory.

**Request Body:**
```json
{
  "isbn": "978-0123456789",
  "title": "Database Systems",
  "author": "John Smith",
  "edition": "5th",
  "sellingPrice": 49.99,
  "acquisitionCost": 25.00,
  "bookCondition": "Good",
  "courseMajor": "MIS 301"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "bookId": 1
  }
}
```

**Status Codes:**
- `201 Created` - Book created
- `400 Bad Request` - Validation error (e.g., SellingPrice <= AcquisitionCost)
- `403 Forbidden` - Admin access required
- `500 Internal Server Error` - Server error

---

#### PUT `/api/books/{bookId}` (Admin Only)
Update an existing book.

**Request Body:**
```json
{
  "sellingPrice": 54.99,
  "bookCondition": "New",
  "status": "Available"
}
```

**Status Codes:**
- `200 OK` - Book updated
- `400 Bad Request` - Validation error
- `403 Forbidden` - Admin access required
- `404 Not Found` - Book not found
- `500 Internal Server Error` - Server error

---

#### DELETE `/api/books/{bookId}` (Admin Only)
Delete a book from inventory.

**Status Codes:**
- `200 OK` - Book deleted
- `403 Forbidden` - Admin access required
- `404 Not Found` - Book not found
- `409 Conflict` - Book is in an active order (cannot delete)
- `500 Internal Server Error` - Server error

---

### Shopping Cart

#### GET `/api/cart`
Get current user's shopping cart.

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "cartItemId": 1,
      "bookId": 5,
      "isbn": "978-0123456789",
      "title": "Database Systems",
      "author": "John Smith",
      "edition": "5th",
      "sellingPrice": 49.99,
      "bookCondition": "Good",
      "courseMajor": "MIS 301",
      "addedDate": "2024-01-15T10:30:00Z"
    }
  ],
  "total": 49.99
}
```

**Status Codes:**
- `200 OK` - Success
- `401 Unauthorized` - Not logged in
- `500 Internal Server Error` - Server error

---

#### POST `/api/cart`
Add a book to shopping cart.

**Request Body:**
```json
{
  "bookId": 5
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "cartItemId": 1,
    "message": "Book added to cart"
  }
}
```

**Status Codes:**
- `201 Created` - Book added
- `400 Bad Request` - Book not available or already in cart
- `401 Unauthorized` - Not logged in
- `404 Not Found` - Book not found
- `500 Internal Server Error` - Server error

---

#### DELETE `/api/cart/{cartItemId}`
Remove an item from shopping cart.

**Status Codes:**
- `200 OK` - Item removed
- `401 Unauthorized` - Not logged in
- `403 Forbidden` - Not your cart item
- `404 Not Found` - Cart item not found
- `500 Internal Server Error` - Server error

---

#### DELETE `/api/cart`
Clear entire shopping cart.

**Status Codes:**
- `200 OK` - Cart cleared
- `401 Unauthorized` - Not logged in
- `500 Internal Server Error` - Server error

---

### Orders

#### POST `/api/orders`
Create a new order from shopping cart (checkout).

**Request Body:**
```json
{
  "paymentMethodId": 1
}
```
or for one-time payment:
```json
{
  "paymentMethodId": null
}
```

**Field Requirements:**
- `paymentMethodId` (optional): ID of a saved payment method, or `null` for one-time payment
  - If provided, must be a valid payment method ID that belongs to the current user
  - If `null`, payment is processed as a one-time payment without saving the method
  - See `/api/payment-methods` endpoints to manage saved payment methods

**Response:**
```json
{
  "success": true,
  "data": {
    "orderId": 100,
    "orderDate": "2024-01-15T10:30:00Z",
    "status": "New",
    "totalAmount": 99.98,
    "items": [
      {
        "bookId": 5,
        "title": "Database Systems",
        "priceAtSale": 49.99
      }
    ]
  }
}
```

**Status Codes:**
- `201 Created` - Order created successfully
- `400 Bad Request` - Cart empty or items no longer available
- `401 Unauthorized` - Not logged in
- `404 Not Found` - Payment method ID not found (if provided)
- `500 Internal Server Error` - Server error

**Notes:**
- The shopping cart is automatically cleared after successful order creation
- Payment is automatically processed and recorded in the Payment table
- If a `paymentMethodId` is provided, it must belong to the authenticated user

---

#### GET `/api/orders`
Get current user's order history.

**Query Parameters:**
- `status` (optional) - Filter by status (New, Processing, Fulfilled, Cancelled)
- `page` (optional) - Page number
- `pageSize` (optional) - Items per page

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "orderId": 100,
      "orderDate": "2024-01-15T10:30:00Z",
      "status": "Fulfilled",
      "totalAmount": 99.98,
      "itemCount": 2
    }
  ]
}
```

**Status Codes:**
- `200 OK` - Success
- `401 Unauthorized` - Not logged in
- `500 Internal Server Error` - Server error

---

#### GET `/api/orders/{orderId}`
Get detailed information about a specific order.

**Response:**
```json
{
  "success": true,
  "data": {
    "orderId": 100,
    "orderDate": "2024-01-15T10:30:00Z",
    "status": "Fulfilled",
    "totalAmount": 99.98,
    "items": [
      {
        "lineItemId": 1,
        "bookId": 5,
        "isbn": "978-0123456789",
        "title": "Database Systems",
        "author": "John Smith",
        "edition": "5th",
        "priceAtSale": 49.99
      }
    ],
    "payment": {
      "paymentId": 50,
      "paymentDate": "2024-01-15T10:30:00Z",
      "amount": 99.98,
      "paymentStatus": "Completed",
      "paymentMethod": "Visa ending in 1234"
    }
  }
}
```

**Status Codes:**
- `200 OK` - Success
- `401 Unauthorized` - Not logged in
- `403 Forbidden` - Not your order
- `404 Not Found` - Order not found
- `500 Internal Server Error` - Server error

---

#### PUT `/api/orders/{orderId}/status` (Admin Only)
Update order status.

**Request Body:**
```json
{
  "status": "Processing"
}
```

**Valid Status Transitions:**
- `New` → `Processing` → `Fulfilled`
- `New` / `Processing` → `Cancelled`

**Response:**
```json
{
  "success": true,
  "data": {
    "orderId": 100,
    "status": "Processing"
  }
}
```

**Status Codes:**
- `200 OK` - Status updated
- `400 Bad Request` - Invalid status transition
- `403 Forbidden` - Admin access required
- `404 Not Found` - Order not found
- `500 Internal Server Error` - Server error

---

### Sell Submissions

#### POST `/api/sell-submissions`
Submit a book for sale to the bookstore.

**Request Body:**
```json
{
  "isbn": "978-0123456789",
  "title": "Database Systems",
  "author": "John Smith",
  "edition": "5th",
  "physicalCondition": "Good",
  "courseMajor": "MIS 301",
  "askingPrice": 30.00
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "submissionId": 50,
    "status": "Pending Review",
    "submissionDate": "2024-01-15T10:30:00Z"
  }
}
```

**Status Codes:**
- `201 Created` - Submission created
- `400 Bad Request` - Validation error
- `401 Unauthorized` - Not logged in
- `500 Internal Server Error` - Server error

---

#### GET `/api/sell-submissions`
Get current user's sell submissions.

**Query Parameters:**
- `status` (optional) - Filter by status (Pending Review, Approved, Rejected, Completed)

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "submissionId": 50,
      "isbn": "978-0123456789",
      "title": "Database Systems",
      "author": "John Smith",
      "edition": "5th",
      "askingPrice": 30.00,
      "status": "Pending Review",
      "submissionDate": "2024-01-15T10:30:00Z"
    }
  ]
}
```

**Status Codes:**
- `200 OK` - Success
- `401 Unauthorized` - Not logged in
- `500 Internal Server Error` - Server error

**Note:** Status values include:
- `Pending Review`: Initial submission, awaiting admin review
- `Approved`: Customer has accepted an admin offer, awaiting admin final approval
- `Rejected`: Submission has been rejected
- `Completed`: Submission has been approved and book has been created in inventory

---

#### GET `/api/sell-submissions/{submissionId}`
Get detailed information about a sell submission, including negotiation history.

**Response:**
```json
{
  "success": true,
  "data": {
    "submissionId": 50,
    "isbn": "978-0123456789",
    "title": "Database Systems",
    "author": "John Smith",
    "edition": "5th",
    "askingPrice": 30.00,
    "status": "Pending Review",
    "submissionDate": "2024-01-15T10:30:00Z",
    "negotiations": [
      {
        "negotiationId": 1,
        "offeredBy": "Admin",
        "offeredPrice": 25.00,
        "offerDate": "2024-01-16T09:00:00Z",
        "offerMessage": "We can offer $25 based on condition",
        "offerStatus": "Pending",
        "roundNumber": 1
      }
    ]
  }
}
```

**Status Codes:**
- `200 OK` - Success
- `401 Unauthorized` - Not logged in
- `403 Forbidden` - Not your submission
- `404 Not Found` - Submission not found
- `500 Internal Server Error` - Server error

---

#### POST `/api/sell-submissions/{submissionId}/negotiate`
Respond to a price negotiation (accept, reject, or counter).

**Request Body:**
```json
{
  "action": "accept",
  "negotiationId": 1
}
```

or counter-offer:
```json
{
  "action": "counter",
  "offeredPrice": 27.50,
  "offerMessage": "Can we meet at $27.50?"
}
```

or reject:
```json
{
  "action": "reject",
  "negotiationId": 1
}
```

**Field Requirements:**
- `action` (required): One of "accept", "reject", or "counter"
- `negotiationId` (required for accept/reject): ID of the negotiation to accept or reject
- `offeredPrice` (required for counter): Customer's counter-offer price
- `offerMessage` (optional for counter): Optional message with counter-offer

**Response:**
```json
{
  "success": true,
  "data": {
    "negotiationId": 1,
    "offerStatus": "Accepted",
    "message": "Price accepted. Book will be added to inventory."
  }
}
```

**Business Rules:**
- **Accept**: Customer can only accept the **latest pending admin offer**. Older offers are automatically rejected when a new admin offer is made.
- **Reject**: Customer can only reject admin offers. If this is the only pending offer, the submission status becomes "Rejected". If other pending offers exist, only this offer is rejected.
- **Counter**: Customer can only counter admin offers. Creates a new negotiation round with status "Pending".

**Status Codes:**
- `200 OK` - Action processed
- `400 Bad Request` - Invalid action, price, or negotiation (e.g., trying to accept an old offer)
- `401 Unauthorized` - Not logged in
- `403 Forbidden` - Not your submission
- `404 Not Found` - Submission or negotiation not found
- `500 Internal Server Error` - Server error

---

### Admin - Sell Submissions

#### GET `/api/admin/sell-submissions`
Get all sell submissions (Admin Only).

**Query Parameters:**
- `status` (optional) - Filter by status (Pending Review, Approved, Rejected, Completed)
- `page` (optional) - Page number
- `pageSize` (optional) - Items per page

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "submissionId": 50,
      "userId": 5,
      "username": "johndoe",
      "isbn": "978-0123456789",
      "title": "Database Systems",
      "author": "John Smith",
      "edition": "5th",
      "askingPrice": 30.00,
      "physicalCondition": "Good",
      "courseMajor": "MIS 301",
      "status": "Pending Review",
      "submissionDate": "2024-01-15T10:30:00Z",
      "negotiations": [
        {
          "negotiationId": 1,
          "offeredBy": "Admin",
          "offeredPrice": 25.00,
          "offerDate": "2024-01-16T09:00:00Z",
          "offerMessage": "We can offer $25 based on condition",
          "offerStatus": "Pending",
          "roundNumber": 1
        }
      ]
    }
  ]
}
```

**Status Codes:**
- `200 OK` - Success
- `403 Forbidden` - Admin access required
- `500 Internal Server Error` - Server error

---

#### GET `/api/admin/sell-submissions/{submissionId}`
Get detailed information about a specific sell submission, including full negotiation history (Admin Only).

**Response:**
```json
{
  "success": true,
  "data": {
    "submissionId": 50,
    "userId": 5,
    "username": "johndoe",
    "isbn": "978-0123456789",
    "title": "Database Systems",
    "author": "John Smith",
    "edition": "5th",
    "askingPrice": 30.00,
    "physicalCondition": "Good",
    "courseMajor": "MIS 301",
    "status": "Pending Review",
    "submissionDate": "2024-01-15T10:30:00Z",
    "negotiations": [
      {
        "negotiationId": 1,
        "offeredBy": "Admin",
        "offeredPrice": 25.00,
        "offerDate": "2024-01-16T09:00:00Z",
        "offerMessage": "We can offer $25 based on condition",
        "offerStatus": "Pending",
        "roundNumber": 1
      },
      {
        "negotiationId": 2,
        "offeredBy": "User",
        "offeredPrice": 27.50,
        "offerDate": "2024-01-16T10:00:00Z",
        "offerMessage": "Can we meet at $27.50?",
        "offerStatus": "Pending",
        "roundNumber": 2
      }
    ]
  }
}
```

**Status Codes:**
- `200 OK` - Success
- `403 Forbidden` - Admin access required
- `404 Not Found` - Submission not found
- `500 Internal Server Error` - Server error

---

#### POST `/api/admin/sell-submissions/{submissionId}/negotiate`
Admin makes a counter-offer in price negotiation.

**Request Body:**
```json
{
  "offeredPrice": 25.00,
  "offerMessage": "We can offer $25 based on the book's condition."
}
```

**Field Requirements:**
- `offeredPrice` (required): Admin's offer price (must be > 0)
- `offerMessage` (optional): Optional message with the offer

**Response:**
```json
{
  "success": true,
  "data": {
    "negotiationId": 1,
    "roundNumber": 1,
    "offeredPrice": 25.00,
    "offerStatus": "Pending"
  }
}
```

**Business Rules:**
- Admin can only negotiate when submission status is "Pending Review" and there isn't a pending admin offer, OR when the last pending offer is from a user
- When admin makes a new offer, **all previous pending admin offers are automatically rejected** (superseded by the new offer)
- Each negotiation round increments the round number

**Status Codes:**
- `201 Created` - Counter-offer created
- `400 Bad Request` - Validation error (e.g., invalid price, submission not in correct status)
- `403 Forbidden` - Admin access required
- `404 Not Found` - Submission not found
- `500 Internal Server Error` - Server error

---

#### PUT `/api/admin/sell-submissions/{submissionId}/approve`
Approve a sell submission and create book in inventory.

**Request Body:**
```json
{
  "sellingPrice": 49.99
}
```

**Field Requirements:**
- `sellingPrice` (required): The price at which the book will be sold (must be > 0)

**Business Rules:**
- **Initial Approval** (no negotiations): Submission must be in "Pending Review" status. `AcquisitionCost` = `AskingPrice`. `SellingPrice` must be > `AskingPrice`.
- **Negotiated Approval** (after customer accepts): Submission must be in "Approved" status (customer has accepted an offer). `AcquisitionCost` = accepted negotiation `OfferedPrice`. `SellingPrice` must be > accepted `OfferedPrice`.
- **Re-approval Prevention**: If a book already exists for this submission (SubmissionID in Book table), approval is rejected with error "This submission has already been approved and a book has been created. Cannot approve again."
- After successful approval, submission status is set to **"Completed"** (not "Approved") to indicate the book has been created.

**Response:**
```json
{
  "success": true,
  "data": {
    "submissionId": 50,
    "bookId": 100,
    "status": "Completed"
  }
}
```

**Status Codes:**
- `200 OK` - Submission approved, book created
- `400 Bad Request` - Validation error:
  - `SellingPrice` <= `AcquisitionCost`
  - Submission not in correct status for approval type
- `403 Forbidden` - Admin access required
- `404 Not Found` - Submission not found
- `409 Conflict` - Book already exists for this submission (cannot approve again)
- `500 Internal Server Error` - Server error

**Note:** The book is created in a single transaction with the submission status update. If book creation fails, the entire operation is rolled back.

---

#### PUT `/api/admin/sell-submissions/{submissionId}/reject`
Reject a sell submission.

**Request Body:**
```json
{
  "reason": "Book condition does not meet our standards"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "submissionId": 50,
    "status": "Rejected"
  }
}
```

**Status Codes:**
- `200 OK` - Submission rejected
- `403 Forbidden` - Admin access required
- `404 Not Found` - Submission not found
- `500 Internal Server Error` - Server error

---

### Admin - Orders

#### GET `/api/admin/orders`
Get all orders (Admin Only).

**Query Parameters:**
- `status` (optional) - Filter by status
- `page` (optional) - Page number
- `pageSize` (optional) - Items per page

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "orderId": 100,
      "userId": 5,
      "username": "johndoe",
      "orderDate": "2024-01-15T10:30:00Z",
      "status": "New",
      "totalAmount": 99.98,
      "itemCount": 2
    }
  ]
}
```

**Status Codes:**
- `200 OK` - Success
- `403 Forbidden` - Admin access required
- `500 Internal Server Error` - Server error

---

### Payment Methods

#### GET `/api/payment-methods`
Get current user's saved payment methods.

**Headers:**
```
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "paymentMethodId": 1,
      "cardType": "Visa",
      "lastFourDigits": "1234",
      "expirationDate": "12/2025",
      "isDefault": true,
      "createdDate": "2024-01-15T10:30:00Z",
      "displayName": "Visa ending in 1234"
    }
  ]
}
```

**Field Descriptions:**
- `paymentMethodId`: Unique identifier for the payment method
- `cardType`: Type of card (Visa, MasterCard, American Express, Discover, etc.)
- `lastFourDigits`: Last 4 digits of the card number (for display only)
- `expirationDate`: Card expiration date in MM/YYYY format
- `isDefault`: Whether this is the user's default payment method
- `createdDate`: When the payment method was added
- `displayName`: Formatted display string (e.g., "Visa ending in 1234")

**Status Codes:**
- `200 OK` - Success
- `401 Unauthorized` - Not logged in
- `500 Internal Server Error` - Server error

**Notes:**
- Payment methods are ordered by default status first (defaults first), then by creation date (newest first)
- Only the authenticated user's payment methods are returned

---

#### POST `/api/payment-methods`
Add a new payment method.

**Headers:**
```
Authorization: Bearer {token}
```

**Request Body:**
```json
{
  "cardType": "Visa",
  "lastFourDigits": "1234",
  "expirationDate": "12/2025",
  "isDefault": false
}
```

**Field Requirements:**
- `cardType` (required): Card type (Visa, MasterCard, American Express, Discover, etc.)
- `lastFourDigits` (required): Exactly 4 digits (numbers only)
- `expirationDate` (required): Format MM/YYYY (e.g., "12/2025")
- `isDefault` (optional): Boolean, defaults to false. If true, automatically unsets all other defaults for the user

**Response:**
```json
{
  "success": true,
  "data": {
    "paymentMethodId": 1
  }
}
```

**Status Codes:**
- `201 Created` - Payment method added successfully
- `400 Bad Request` - Validation error:
  - Missing required fields
  - Invalid expiration date format
  - Last four digits not exactly 4 digits
  - Invalid card type
- `401 Unauthorized` - Not logged in
- `500 Internal Server Error` - Server error

**Business Rules:**
- If `isDefault` is set to `true`, all other payment methods for the user are automatically set to `false`
- Only one payment method can be default per user
- Expiration date must be in MM/YYYY format (month 01-12, year 2000-2099)
- Last four digits must be exactly 4 numeric digits

---

#### PUT `/api/payment-methods/{paymentMethodId}/default`
Set a payment method as the default.

**Headers:**
```
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "message": "Default payment method updated successfully"
}
```

**Status Codes:**
- `200 OK` - Default payment method updated
- `401 Unauthorized` - Not logged in
- `404 Not Found` - Payment method not found or does not belong to user
- `500 Internal Server Error` - Server error

**Business Rules:**
- Setting a payment method as default automatically unsets all other defaults for that user
- Only the owner of the payment method can set it as default

---

#### DELETE `/api/payment-methods/{paymentMethodId}`
Delete a payment method.

**Headers:**
```
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "message": "Payment method deleted successfully"
}
```

**Status Codes:**
- `200 OK` - Payment method deleted successfully
- `400 Bad Request` - Payment method has been used in orders (cannot delete)
- `401 Unauthorized` - Not logged in
- `404 Not Found` - Payment method not found or does not belong to user
- `500 Internal Server Error` - Server error

**Business Rules:**
- Cannot delete a payment method that has been used in any Payment records
- Only the owner of the payment method can delete it
- If the deleted payment method was the default, no new default is automatically set

---

### Users (Admin Only)

#### GET `/api/admin/users`
Get list of all users.

**Query Parameters:**
- `userType` (optional) - Filter by Customer or Admin
- `page` (optional) - Page number
- `pageSize` (optional) - Items per page

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "userId": 1,
      "username": "johndoe",
      "email": "john@example.com",
      "firstName": "John",
      "lastName": "Doe",
      "userType": "Customer",
      "createdDate": "2024-01-01T00:00:00Z"
    }
  ]
}
```

**Status Codes:**
- `200 OK` - Success
- `403 Forbidden` - Admin access required
- `500 Internal Server Error` - Server error

---

## Error Handling

All endpoints return appropriate HTTP status codes:

- `200 OK` - Successful GET, PUT, DELETE
- `201 Created` - Successful POST (resource created)
- `400 Bad Request` - Validation error, invalid input
- `401 Unauthorized` - Not authenticated
- `403 Forbidden` - Authenticated but not authorized (e.g., admin-only endpoint)
- `404 Not Found` - Resource not found
- `409 Conflict` - Resource conflict (e.g., duplicate entry)
- `500 Internal Server Error` - Server error

## Notes for Implementation

### Authentication
- Token-based authentication using in-memory session storage
- Sessions expire after 24 hours
- Tokens are GUIDs generated on login
- Use `AuthHelper.GetCurrentUser(Request, sessionService)` in controllers to get authenticated user
- Return `401 Unauthorized` if `GetCurrentUser()` returns null
- Passwords are stored as plain text (for demo/school project purposes only)
- Session storage is in-memory (sessions cleared on server restart)

### Transaction Management
Critical operations should use database transactions:
- Checkout (create order, update books, create payment, clear cart)
- Order cancellation (update order, restock books)
- Approve sell submission (update submission status to "Completed", create book)
- Price negotiation (accept/reject updates multiple tables atomically)

### Validation
- Stock availability must be checked before adding to cart
- Stock must be verified again before checkout
- `SellingPrice > AcquisitionCost` must be enforced
- Order status transitions must be validated
- Sell submission approval: Check if book already exists (prevent re-approval)
- Price negotiation: Only latest pending admin offer can be accepted/rejected by customer
- Price negotiation: Admin offers automatically reject previous pending admin offers

### Payment Handling
- `PaymentMethodID` can be NULL for one-time payments (when user doesn't use a saved payment method)
- `PaymentMethodID` can reference a saved payment method from the `PaymentMethod` table
- Users can save payment methods via `/api/payment-methods` endpoints
- When creating an order, users can specify a `paymentMethodId` or use `null` for one-time payment
- `Payment.Amount` should match `PurchaseOrder.TotalAmount`
- Payment status defaults to 'Completed' (simulated payment for demo purposes)
- Saved payment methods store only safe data (last 4 digits, card type) - no full card numbers
- Only one payment method can be set as default per user
- Payment methods used in orders cannot be deleted (to maintain order history integrity)

### Stock Calculation
- Stock is calculated dynamically: `COUNT(*) WHERE ISBN = ? AND Edition = ? AND Status = 'Available'`
- Never store stock quantity in a field
- Always check availability before operations
- Books are grouped by ISBN+Edition in listings (multiple physical copies = one listing entry)
- Each BookID represents one physical book

### Book Listing
- `GET /api/books` returns books grouped by ISBN+Edition
- Price range (minPrice/maxPrice) shows variation across available copies
- `availableConditions` lists all conditions available for that ISBN+Edition
- Only books with `Status = 'Available'` are included in results
- Pagination is simple in-memory pagination after grouping (for school project)

### Sell Submission Status Flow

**Status Values:**
- `Pending Review`: Initial submission, awaiting admin review
- `Approved`: Customer has accepted an admin offer, awaiting admin final approval with selling price
- `Rejected`: Submission has been rejected (either by admin or customer rejecting all offers)
- `Completed`: Submission has been approved, book created in inventory, process complete

**Status Transitions:**
1. **Initial Submission**: `Pending Review`
2. **Admin Negotiates**: Still `Pending Review` (negotiation added)
3. **Customer Accepts Offer**: `Pending Review` → `Approved`
4. **Customer Rejects All Offers**: `Pending Review` → `Rejected` (if last pending offer)
5. **Admin Approves (Final)**: `Pending Review` or `Approved` → `Completed` (book created)
6. **Admin Rejects**: `Pending Review` → `Rejected`

**Important Notes:**
- Once status is `Completed`, the submission cannot be approved again (book already exists)
- Status `Approved` means customer accepted an offer, but admin still needs to set selling price and create book
- Multiple negotiation rounds can occur while status remains `Pending Review`
- Admin can make multiple offers; new offers automatically reject old pending admin offers

---

**Last Updated**: 2024-01-20
**API Version**: 1.1

