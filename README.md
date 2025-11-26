# Crimson BookStore

A web-based point-of-sale system for buying and selling used textbooks and general reading materials.

## 🚀 Quick Start (For Classmates)

**Follow these steps to get the project running:**

### 1. Database Setup
- Make sure MySQL Server is installed and running
- Create the database by running the SQL script:
  ```bash
  mysql -u root -p < CrimsonBookStore5.sql
  ```
  (Enter your MySQL root password when prompted)

### 2. Configure Database Connection
- Open `api/appsettings.json`
- Update the connection string with **YOUR** MySQL password:
  ```json
  {
    "ConnectionStrings": {
      "DefaultConnection": "Server=localhost;Database=crimsonbookstore;User=root;Password=YOUR_PASSWORD_HERE;Port=3306;"
    }
  }
  ```
  ⚠️ **Important**: Replace `YOUR_PASSWORD_HERE` with your actual MySQL root password!

### 3. Start the Backend API
Open a terminal and run:
```bash
cd api
dotnet restore
dotnet run
```
The API will start on `http://localhost:5000` or `https://localhost:5001`

### 4. Start the Frontend
Open a **new terminal** (keep the backend running) and run:
```bash
cd frontend
python -m http.server 8000
```
Or if you don't have Python, you can:
- Use VS Code's Live Server extension
- Or simply open `frontend/index.html` directly in your browser

Then navigate to: `http://localhost:8000`

### 5. Test It Works
- You should see the Crimson BookStore homepage
- Try registering a new account
- The API should be running in the background (check the terminal for any errors)

### ⚠️ Important Notes:
- **Passwords**: This project uses **plain text passwords** for demo/educational purposes only. Do NOT use real passwords!
- **Two Terminals**: You need BOTH the backend (API) and frontend running at the same time
- **Database**: Make sure MySQL is running before starting the backend
- **Port Conflicts**: If ports 5000/5001 or 8000 are in use, you may need to change them

### Troubleshooting:
- **"Connection refused"**: Make sure MySQL is running and the password in `appsettings.json` is correct
- **"Cannot connect to API"**: Make sure the backend is running (check the terminal)
- **CORS errors**: Make sure the frontend is accessing the API from the correct port

---

## Project Overview

CrimsonBookStore is a local business specializing in buying and selling used textbooks. This system provides a fully functional web application with front-end user experience and a well-structured back-end database to manage inventory, customer transactions, and administrative operations.

## Tech Stack

- **Frontend**: HTML/CSS/JavaScript (ES6+), Bootstrap 5.x from CDN
  - Single-page application with one `index.html` file
  - No frameworks (React, Vue, Angular)
  - All UI changes via JavaScript DOM manipulation
  
- **Backend**: .NET 8.0 Web API (ASP.NET Core)
  - RESTful API design
  - C# language
  
- **Database**: MySQL Server
  - Direct SQL queries (no ORM)
  - Database file location: `/api` folder
  - Connection string in `appsettings.json`

## Project Structure

```
Crimson-BookStore/
├── api/                    # Backend .NET API
│   ├── Controllers/        # API endpoints
│   ├── Models/             # Data models
│   ├── Services/           # Business logic
│   └── appsettings.json    # Configuration (DB connection)
├── frontend/               # Frontend files
│   ├── index.html          # Main HTML file
│   ├── js/                 # JavaScript files
│   └── css/                # Custom CSS (if needed)
├── CrimsonBookStore5.sql  # Database creation script
├── DATABASE_DESIGN.md      # Complete database documentation
├── API_DOCUMENTATION.md    # API endpoint documentation
└── README.md               # This file
```

## Getting Started

### Prerequisites

- MySQL Server installed and running
- .NET 8.0 SDK installed
- Web browser (for frontend)

### Database Setup

1. **Create the database:**
   ```bash
   mysql -u root -p < CrimsonBookStore5.sql
   ```
   
   Or open MySQL and run:
   ```sql
   source CrimsonBookStore5.sql;
   ```

2. **Configure connection string** in `api/appsettings.json`:
   - The file should already exist with a placeholder `YOUR_PASSWORD_HERE`
   - Replace `YOUR_PASSWORD_HERE` with your actual MySQL root password
   - Example:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=crimsonbookstore;User=root;Password=yourpassword;Port=3306;"
     }
   }
   ```
   ⚠️ **Security Note**: Never commit your actual password to git! The repository uses a placeholder to prevent accidental password commits.

### Backend Setup

1. Navigate to the API directory:
   ```bash
   cd api
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Run the API:
   ```bash
   dotnet run
   ```

   The API will typically run on `https://localhost:5001` or `http://localhost:5000`

### Frontend Setup

1. Open `frontend/index.html` in a web browser
2. Or use a local development server:
   ```bash
   cd frontend
   python -m http.server 8000
   ```
   Then navigate to `http://localhost:8000`

## Key Features

### Customer Features
- User registration and authentication
- Browse and search books (by title, author, ISBN, course/major)
- Shopping cart management
- Checkout and order placement
- Purchase history
- Submit books for sale
- Price negotiation with admins

### Admin Features
- Book inventory management (Add, Edit, Delete)
- Review and approve/reject sell submissions
- Process purchase orders (update status)
- User management
- View all orders and submissions

## Important Database Concepts

### Stock Management
- **Stock is calculated dynamically** - NOT stored as a field
- Query: `SELECT COUNT(*) FROM Book WHERE ISBN = ? AND Edition = ? AND Status = 'Available'`
- Each `BookID` represents one physical book
- Multiple books can share the same ISBN+Edition

### Book Status
- `Available`: Book is in stock and can be purchased
- `Sold`: Book has been purchased and is no longer available

### Order Status Flow
- `New` → `Processing` → `Fulfilled` (or `Cancelled`)
- Only admins can update order status
- Cancelled orders automatically restock books

### Price Negotiation
- Multi-round negotiation between customers and admins
- Tracked in `PriceNegotiation` table
- Final accepted price becomes `AcquisitionCost` in Book table

## Documentation

- **[DATABASE_DESIGN.md](DATABASE_DESIGN.md)** - Complete database schema, relationships, business logic, and query examples
- **[API_DOCUMENTATION.md](API_DOCUMENTATION.md)** - All API endpoints, request/response formats, and examples

## Development Guidelines

### Frontend
- Use Bootstrap 5 classes for styling (minimal custom CSS)
- Vanilla JavaScript only (no frameworks)
- All API calls use `async/await`
- DOM manipulation for all UI updates

### Backend
- RESTful API design
- Proper HTTP status codes
- Error handling and validation
- Direct SQL queries (no ORM)

### Database
- All constraints enforced at database level
- Use transactions for critical operations (checkout, order cancellation)
- Indexes on frequently queried columns

## Common Tasks

### Adding a New Book
1. Admin creates book via API or directly in database
2. Book starts with `Status = 'Available'`
3. Stock count automatically calculated when queried

### Processing a Sale
1. Customer adds books to cart
2. Customer checks out → creates `PurchaseOrder`
3. Creates `OrderLineItem` for each book
4. Updates `Book.Status = 'Sold'` for each book
5. Creates `Payment` record
6. Clears shopping cart

### Approving a Sell Submission
1. Admin reviews `SellSubmission`
2. Price negotiation occurs (if needed)
3. When approved, creates `Book` record with `AcquisitionCost` from negotiation
4. Admin sets `SellingPrice` (must be > `AcquisitionCost`)

## Notes for Future Developers/AI

### Critical Business Rules
- `SellingPrice > AcquisitionCost` (enforced by constraint)
- Stock quantity is **calculated**, never stored
- Each `BookID` = one physical book
- Shopping cart persists in database (survives browser close)
- Orders track individual books (no quantity field in OrderLineItem)

### Important Constraints
- `PaymentMethod.IsDefault` - Only one default per user (enforced in application logic)
- `Payment.Amount` should match `PurchaseOrder.TotalAmount` (validate in application)
- `Payment.PaymentMethodID` is nullable (supports one-time payments)

### Query Patterns
- Stock count: `COUNT(*) WHERE ISBN = ? AND Edition = ? AND Status = 'Available'`
- Cart total: `SUM(SellingPrice) FROM ShoppingCart JOIN Book`
- Order history: `SELECT PurchaseOrder JOIN OrderLineItem JOIN Book`

## Troubleshooting

### Database Connection Issues
- Verify MySQL Server is running
- Check connection string in `appsettings.json`
- Ensure database `crimsonbookstore` exists

### Stock Count Issues
- Remember: stock is calculated, not stored
- Always query with `Status = 'Available'`
- Check that books aren't marked as `Sold` incorrectly

### Payment Issues
- `PaymentMethodID` can be NULL (one-time payments)
- Ensure `Payment.Amount` matches `PurchaseOrder.TotalAmount`

## License

This is a class project for MIS 330 at The University of Alabama.

