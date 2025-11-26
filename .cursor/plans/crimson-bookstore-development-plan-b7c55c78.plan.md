<!-- b7c55c78-70c8-400f-a4e8-6a840f0d2fa7 3fa8c8e6-564a-4be8-9d59-b2e6a87dfe13 -->
# Crimson BookStore Development Plan

## Phase 1: Authentication System

### Step 1: Create User Models and DTOs

- Create `Models/User.cs` with User entity properties
- Create `Models/RegisterRequest.cs` and `Models/LoginRequest.cs` DTOs
- Create `Models/AuthResponse.cs` for API responses
- Reference database schema from `DATABASE_DESIGN.md`

### Step 2: Implement Authentication Service

- Create `Services/IAuthService.cs` interface
- Create `Services/AuthService.cs` with password hashing (BCrypt or similar)
- Implement Register: Check username/email uniqueness, hash password, insert into User table
- Implement Login: Verify credentials, return user info
- Use `DatabaseService` for SQL queries

### Step 3: Create Authentication Controller

- Create `Controllers/AuthController.cs`
- Implement `POST /api/auth/register` endpoint
- Implement `POST /api/auth/login` endpoint
- Return standardized response format (success/error)
- Handle validation errors (duplicate username/email)

### Step 4: Add Session/Token Management

- Implement simple session storage (in-memory dictionary or JWT)
- Store user ID and user type in session
- Add middleware to extract user from session
- Create `Models/CurrentUser.cs` for authenticated user context

### Step 5: Create Frontend Authentication UI

- Update `frontend/js/auth.js` with register/login functions
- Create login form UI in `frontend/index.html`
- Create register form UI
- Handle form submissions, store auth token, redirect on success
- Show/hide navigation based on auth status

## Phase 2: Book Management (Core Feature)

### Step 6: Create Book Models

- Create `Models/Book.cs` with Book properties
- Create `Models/BookListResponse.cs` for grouped book listings
- Create `Models/BookDetailResponse.cs` for single book details
- Reference `DATABASE_DESIGN.md` for stock calculation logic

### Step 7: Implement Book Service

- Create `Services/IBookService.cs` interface
- Create `Services/BookService.cs`
- Implement `GetAvailableBooks()`: Query books grouped by ISBN+Edition with stock counts
- Implement `GetBookById(int bookId)`: Get single book with stock count
- Implement `SearchBooks(string searchTerm)`: Search by title/author/ISBN/course
- Use stock calculation: `COUNT(*) WHERE ISBN=? AND Edition=? AND Status='Available'`

### Step 8: Create Books Controller

- Create `Controllers/BooksController.cs`
- Implement `GET /api/books`: List all available books (grouped, with stock)
- Implement `GET /api/books/{id}`: Get book details
- Implement `GET /api/books/search?q={term}`: Search books
- Return data in format specified in `API_DOCUMENTATION.md`

### Step 9: Create Frontend Book Listing Page

- Update `frontend/js/books.js` with API calls
- Implement `loadBooks()` function to fetch and display books
- Create Bootstrap cards for each book (title, author, price, stock count)
- Add search functionality with real-time filtering
- Add "Add to Cart" buttons (disabled if out of stock)

### Step 10: Create Book Detail View

- Create book detail page/modal
- Show full book information (ISBN, edition, condition, course/major)
- Display stock count
- Add to cart functionality from detail view

## Phase 3: Shopping Cart

### Step 11: Create Cart Models

- Create `Models/CartItem.cs` and `Models/CartResponse.cs`
- Reference ShoppingCart table structure from database

### Step 12: Implement Cart Service

- Create `Services/ICartService.cs` interface
- Create `Services/CartService.cs`
- Implement `GetCart(int userId)`: Get user's cart with book details
- Implement `AddToCart(int userId, int bookId)`: Validate book available, insert into ShoppingCart
- Implement `RemoveFromCart(int userId, int cartItemId)`: Delete from ShoppingCart
- Implement `ClearCart(int userId)`: Delete all user's cart items
- Check UNIQUE constraint on (UserID, BookID)

### Step 13: Create Cart Controller

- Create `Controllers/CartController.cs`
- Implement `GET /api/cart`: Get current user's cart (requires auth)
- Implement `POST /api/cart`: Add book to cart (requires auth)
- Implement `DELETE /api/cart/{cartItemId}`: Remove item (requires auth)
- Implement `DELETE /api/cart`: Clear cart (requires auth)
- Add authorization check (user must be logged in)

### Step 14: Create Frontend Cart UI

- Update `frontend/js/cart.js` with cart API calls
- Create cart page showing items with prices
- Implement remove item functionality
- Calculate and display cart total
- Add "Proceed to Checkout" button
- Update cart badge count in navigation

## Phase 4: Order Processing

### Step 15: Create Order Models

- Create `Models/Order.cs`, `Models/OrderLineItem.cs`
- Create `Models/CreateOrderRequest.cs` and `Models/OrderResponse.cs`
- Reference PurchaseOrder and OrderLineItem tables

### Step 16: Implement Order Service

- Create `Services/IOrderService.cs` interface
- Create `Services/OrderService.cs`
- Implement `CreateOrder(int userId, int? paymentMethodId)`: 
- Start transaction
- Validate all cart items still available
- Create PurchaseOrder record
- Create OrderLineItem for each cart item
- Update Book.Status = 'Sold' for each book
- Calculate TotalAmount
- Create Payment record
- Clear shopping cart
- Commit transaction (rollback on error)
- Implement `GetUserOrders(int userId)`: Get order history
- Implement `GetOrderDetails(int orderId, int userId)`: Get order with line items

### Step 17: Create Orders Controller

- Create `Controllers/OrdersController.cs`
- Implement `POST /api/orders`: Create order from cart (requires auth)
- Implement `GET /api/orders`: Get user's order history (requires auth)
- Implement `GET /api/orders/{id}`: Get order details (requires auth, verify ownership)
- Handle payment method (nullable PaymentMethodID for one-time payments)

### Step 18: Create Frontend Checkout Flow

- Update `frontend/js/orders.js` with order API calls
- Create checkout page showing cart summary
- Add payment method selection (saved methods or one-time)
- Implement order creation on checkout confirmation
- Show success message with order ID
- Redirect to order history

### Step 19: Create Order History Page

- Create order history UI showing list of orders
- Display order date, status, total amount
- Add click to view order details
- Show order line items with book information

## Phase 5: Sell Submissions

### Step 20: Create Sell Submission Models

- Create `Models/SellSubmission.cs`, `Models/PriceNegotiation.cs`
- Create `Models/CreateSubmissionRequest.cs` and `Models/SubmissionResponse.cs`
- Reference SellSubmission and PriceNegotiation tables

### Step 21: Implement Sell Submission Service

- Create `Services/ISellSubmissionService.cs` interface
- Create `Services/SellSubmissionService.cs`
- Implement `CreateSubmission(int userId, CreateSubmissionRequest)`: Insert into SellSubmission
- Implement `GetUserSubmissions(int userId)`: Get user's submissions
- Implement `GetSubmissionDetails(int submissionId, int userId)`: Get submission with negotiation history
- Implement `RespondToNegotiation(int submissionId, string action, decimal? counterPrice)`: Handle accept/reject/counter

### Step 22: Create Sell Submissions Controller

- Create `Controllers/SellSubmissionsController.cs`
- Implement `POST /api/sell-submissions`: Create submission (requires auth, Customer only)
- Implement `GET /api/sell-submissions`: Get user's submissions (requires auth)
- Implement `GET /api/sell-submissions/{id}`: Get submission details (requires auth, verify ownership)
- Implement `POST /api/sell-submissions/{id}/negotiate`: Respond to negotiation (requires auth)

### Step 23: Create Frontend Sell Submission UI

- Update `frontend/js/sellSubmission.js` with API calls
- Create "Sell a Book" form with all required fields
- Create submission list page showing user's submissions
- Create submission detail page with negotiation history
- Add accept/reject/counter-offer functionality

## Phase 6: Admin Features

### Step 24: Create Admin Book Management

- Update `Services/BookService.cs` with admin methods:
- `AddBook(Book book)`: Insert new book (validate SellingPrice > AcquisitionCost)
- `UpdateBook(int bookId, Book book)`: Update book details
- `DeleteBook(int bookId)`: Delete book (check if in active order)
- Update `Controllers/BooksController.cs`:
- `POST /api/books`: Add book (Admin only)
- `PUT /api/books/{id}`: Update book (Admin only)
- `DELETE /api/books/{id}`: Delete book (Admin only)
- Add admin authorization check

### Step 25: Create Admin Sell Submission Management

- Create `Services/AdminSellSubmissionService.cs`
- Implement `GetAllSubmissions()`: Get all submissions with filters
- Implement `MakeCounterOffer(int submissionId, decimal price, string message)`: Create PriceNegotiation entry
- Implement `ApproveSubmission(int submissionId, decimal sellingPrice)`: 
- Get accepted negotiation price (AcquisitionCost)
- Create Book record with SubmissionID link
- Update SellSubmission status
- Implement `RejectSubmission(int submissionId, string reason)`: Update status to Rejected
- Create `Controllers/AdminSellSubmissionsController.cs` with endpoints:
- `GET /api/admin/sell-submissions`: List all (Admin only)
- `POST /api/admin/sell-submissions/{id}/negotiate`: Make counter-offer (Admin only)
- `PUT /api/admin/sell-submissions/{id}/approve`: Approve and create book (Admin only)
- `PUT /api/admin/sell-submissions/{id}/reject`: Reject submission (Admin only)

### Step 26: Create Admin Order Management

- Create `Services/AdminOrderService.cs`
- Implement `GetAllOrders()`: Get all orders with filters
- Implement `UpdateOrderStatus(int orderId, string status)`: 
- Validate status transition (New → Processing → Fulfilled, or → Cancelled)
- If Cancelled: Update Book.Status = 'Available' for all books in order
- Create `Controllers/AdminOrdersController.cs`:
- `GET /api/admin/orders`: List all orders (Admin only)
- `PUT /api/admin/orders/{id}/status`: Update status (Admin only)

### Step 27: Create Admin User Management

- Create `Services/AdminUserService.cs`
- Implement `GetAllUsers()`: Get all users with filters
- Create `Controllers/AdminUsersController.cs`:
- `GET /api/admin/users`: List all users (Admin only)

### Step 28: Create Admin Frontend Pages

- Create admin dashboard page
- Create admin book management page (add/edit/delete)
- Create admin sell submission review page (list, approve/reject, negotiate)
- Create admin order management page (list, update status)
- Create admin user list page
- Add admin-only navigation items

## Phase 7: Payment Methods

### Step 29: Create Payment Method Models and Service

- Create `Models/PaymentMethod.cs`
- Create `Services/IPaymentMethodService.cs` and `Services/PaymentMethodService.cs`
- Implement CRUD operations for payment methods
- Enforce single default payment method per user (application logic)

### Step 30: Create Payment Methods Controller

- Create `Controllers/PaymentMethodsController.cs`
- Implement `GET /api/payment-methods`: Get user's saved methods
- Implement `POST /api/payment-methods`: Add payment method
- Implement `DELETE /api/payment-methods/{id}`: Delete payment method
- All endpoints require authentication

### Step 31: Create Frontend Payment Methods UI

- Create payment methods management page
- Add form to save new payment method
- Display saved methods with option to delete
- Set default payment method functionality

## Phase 8: Polish and Testing

### Step 32: Add Error Handling and Validation

- Add try-catch blocks in all controllers
- Return proper HTTP status codes
- Add input validation (required fields, data types, ranges)
- Validate business rules (SellingPrice > AcquisitionCost, stock availability)
- Add error messages to frontend

### Step 33: Improve Frontend UX

- Add loading indicators for API calls
- Add success/error toast notifications
- Improve form validation and error display
- Add empty state messages (empty cart, no orders, etc.)
- Make UI responsive and polished

### Step 34: Add Authorization Middleware

- Create authorization attribute for Admin-only endpoints
- Verify user type (Customer vs Admin) in controllers
- Return 403 Forbidden for unauthorized access
- Update all admin endpoints with authorization

### Step 35: Final Testing and Bug Fixes

- Test all user flows (register, browse, cart, checkout)
- Test admin flows (manage books, review submissions, process orders)
- Test edge cases (out of stock, cancelled orders, etc.)
- Fix any bugs or issues found
- Verify database constraints are working correctly

## Implementation Notes

- All database queries use direct SQL via `DatabaseService` (no ORM)
- Stock is calculated dynamically, never stored
- Use transactions for critical operations (checkout, order cancellation)
- Reference `DATABASE_DESIGN.md` for query patterns and business logic
- Follow response format from `API_DOCUMENTATION.md`
- Use Bootstrap 5 for all frontend styling
- Vanilla JavaScript only (no frameworks)

### To-dos

- [ ] Create User models and DTOs (User.cs, RegisterRequest.cs, LoginRequest.cs, AuthResponse.cs)
- [ ] Implement authentication service with password hashing, register, and login methods
- [ ] Create AuthController with POST /api/auth/register and POST /api/auth/login endpoints
- [ ] Add session/token management for authenticated users
- [ ] Create frontend authentication UI (login/register forms, handle auth state)
- [ ] Create Book models (Book.cs, BookListResponse.cs, BookDetailResponse.cs)
- [ ] Implement BookService with GetAvailableBooks, GetBookById, and SearchBooks methods using stock calculation
- [ ] Create BooksController with GET /api/books, GET /api/books/{id}, and GET /api/books/search endpoints
- [ ] Create frontend book listing page with search and Add to Cart functionality
- [ ] Create book detail view page/modal with full information
- [ ] Create Cart models (CartItem.cs, CartResponse.cs)
- [ ] Implement CartService with GetCart, AddToCart, RemoveFromCart, and ClearCart methods
- [ ] Create CartController with GET, POST, and DELETE endpoints (requires authentication)
- [ ] Create frontend cart UI with item display, remove functionality, and checkout button
- [ ] Create Order models (Order.cs, OrderLineItem.cs, CreateOrderRequest.cs, OrderResponse.cs)
- [ ] Implement OrderService with CreateOrder (transaction-based), GetUserOrders, and GetOrderDetails
- [ ] Create OrdersController with POST /api/orders, GET /api/orders, and GET /api/orders/{id} endpoints
- [ ] Create frontend checkout flow with payment method selection and order creation
- [ ] Create order history page showing user orders and details
- [ ] Create SellSubmission models (SellSubmission.cs, PriceNegotiation.cs, request/response DTOs)
- [ ] Implement SellSubmissionService with CreateSubmission, GetUserSubmissions, GetSubmissionDetails, and RespondToNegotiation
- [ ] Create SellSubmissionsController with POST, GET endpoints and negotiation response endpoint
- [ ] Create frontend sell submission UI (form, list, detail, negotiation)
- [ ] Add admin book management (add/edit/delete) to BookService and BooksController with authorization
- [ ] Create AdminSellSubmissionService with GetAllSubmissions, MakeCounterOffer, ApproveSubmission, RejectSubmission
- [ ] Create AdminSellSubmissionsController with admin-only endpoints for managing submissions
- [ ] Create AdminOrderService with GetAllOrders and UpdateOrderStatus (handle cancellation restocking)
- [ ] Create AdminOrdersController with admin-only endpoints for managing orders
- [ ] Create AdminUserService and AdminUsersController for viewing all users (Admin only)
- [ ] Create admin frontend pages (dashboard, book management, submission review, order management, user list)
- [ ] Create PaymentMethodService with CRUD operations and single default enforcement
- [ ] Create PaymentMethodsController with GET, POST, DELETE endpoints
- [ ] Create frontend payment methods management UI
- [ ] Add comprehensive error handling, validation, and proper HTTP status codes throughout
- [ ] Improve frontend UX with loading indicators, notifications, form validation, and responsive design
- [ ] Add authorization middleware/attributes for Admin-only endpoints and user verification
- [ ] Test all user flows, admin flows, edge cases, and fix any bugs