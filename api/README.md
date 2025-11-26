# Crimson BookStore API

## Setup Instructions

### 1. Install Dependencies

```bash
cd api
dotnet restore
```

### 2. Configure Database Connection

Edit `appsettings.json` and update the connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=crimsonbookstore;User=root;Password=YOUR_PASSWORD;Port=3306;"
  }
}
```

### 3. Create Database

Make sure MySQL is running, then create the database:

```bash
mysql -u root -p < ../CrimsonBookStore5.sql
```

Or in MySQL:
```sql
source ../CrimsonBookStore5.sql;
```

### 4. Run the API

```bash
dotnet run
```

The API will run on:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`

### 5. Test the API

Visit: `http://localhost:5000/api/health`

You should see:
```json
{
  "status": "healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "database": "connected"
}
```

## Project Structure

- `Controllers/` - API endpoints
- `Models/` - Data models/DTOs
- `Services/` - Business logic and database operations
- `Data/` - Data access layer (if needed)

## Development Notes

- Uses direct SQL queries (no ORM)
- MySQL connection via MySql.Data package
- CORS enabled for frontend communication
- Health check endpoint for testing

