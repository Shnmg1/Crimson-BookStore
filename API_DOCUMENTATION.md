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
Get list of all available books with stock counts.

**Query Parameters:**
- `search` (optional) - Search by title, author, ISBN, or course/major
- `isbn` (optional) - Filter by ISBN
- `courseMajor` (optional) - Filter by course/major
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
      "availableConditions": ["New", "Good"]
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

**Status Codes:**
- `200 OK` - Success
- `500 Internal Server Error` - Server error

---

#### GET `/api/books/{bookId}`
Get detailed information about a specific book.

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

**Status Codes:**
- `200 OK` - Success
- `404 Not Found` - Book not found
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
- `201 Created` - Order created
- `400 Bad Request` - Cart empty or items no longer available
- `401 Unauthorized` - Not logged in
- `500 Internal Server Error` - Server error

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
- `status` (optional) - Filter by status (Pending Review, Approved, Rejected)

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

**Response:**
```json
{
  "success": true,
  "data": {
    "submissionId": 50,
    "status": "Approved",
    "message": "Price accepted. Book will be added to inventory."
  }
}
```

**Status Codes:**
- `200 OK` - Action processed
- `400 Bad Request` - Invalid action or price
- `401 Unauthorized` - Not logged in
- `403 Forbidden` - Not your submission
- `404 Not Found` - Submission or negotiation not found
- `500 Internal Server Error` - Server error

---

### Admin - Sell Submissions

#### GET `/api/admin/sell-submissions`
Get all sell submissions (Admin Only).

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
      "submissionId": 50,
      "userId": 5,
      "username": "johndoe",
      "isbn": "978-0123456789",
      "title": "Database Systems",
      "askingPrice": 30.00,
      "status": "Pending Review",
      "submissionDate": "2024-01-15T10:30:00Z"
    }
  ]
}
```

**Status Codes:**
- `200 OK` - Success
- `403 Forbidden` - Admin access required
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

**Status Codes:**
- `201 Created` - Counter-offer created
- `400 Bad Request` - Validation error
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

**Note**: `AcquisitionCost` comes from the accepted negotiation price.

**Response:**
```json
{
  "success": true,
  "data": {
    "submissionId": 50,
    "bookId": 100,
    "status": "Approved"
  }
}
```

**Status Codes:**
- `200 OK` - Submission approved, book created
- `400 Bad Request` - No accepted negotiation or sellingPrice <= acquisitionCost
- `403 Forbidden` - Admin access required
- `404 Not Found` - Submission not found
- `500 Internal Server Error` - Server error

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
      "isDefault": true
    }
  ]
}
```

**Status Codes:**
- `200 OK` - Success
- `401 Unauthorized` - Not logged in
- `500 Internal Server Error` - Server error

---

#### POST `/api/payment-methods`
Add a new payment method.

**Request Body:**
```json
{
  "cardType": "Visa",
  "lastFourDigits": "1234",
  "expirationDate": "12/2025",
  "isDefault": false
}
```

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
- `201 Created` - Payment method added
- `400 Bad Request` - Validation error
- `401 Unauthorized` - Not logged in
- `500 Internal Server Error` - Server error

---

#### DELETE `/api/payment-methods/{paymentMethodId}`
Delete a payment method.

**Status Codes:**
- `200 OK` - Payment method deleted
- `401 Unauthorized` - Not logged in
- `403 Forbidden` - Not your payment method
- `404 Not Found` - Payment method not found
- `500 Internal Server Error` - Server error

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
- Approve sell submission (update submission, create book)

### Validation
- Stock availability must be checked before adding to cart
- Stock must be verified again before checkout
- `SellingPrice > AcquisitionCost` must be enforced
- Order status transitions must be validated

### Payment Handling
- `PaymentMethodID` can be NULL for one-time payments
- `Payment.Amount` should match `PurchaseOrder.TotalAmount`
- Payment status defaults to 'Completed' (simulated payment)

### Stock Calculation
- Stock is calculated dynamically: `COUNT(*) WHERE ISBN = ? AND Edition = ? AND Status = 'Available'`
- Never store stock quantity in a field
- Always check availability before operations

---

**Last Updated**: 2024-01-15
**API Version**: 1.0

