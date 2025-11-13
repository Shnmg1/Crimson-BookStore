-- CrimsonBookStore Database Creation Script (Updated Design)
-- MySQL 8.0+ Compatible
-- Based on CrimsonBookStore4.sql with all improvements

-- Drop database if exists (development/testing only)
DROP DATABASE IF EXISTS crimsonbookstore;
CREATE DATABASE crimsonbookstore;
USE crimsonbookstore;

-- ============================================================================
-- TABLE CREATION
-- ============================================================================

-- ----------------------------------------------------------------------------
-- User Table (Enhanced with FirstName, LastName, Phone, Address)
-- ----------------------------------------------------------------------------
CREATE TABLE User (
    UserID INT AUTO_INCREMENT PRIMARY KEY,
    Username VARCHAR(50) NOT NULL UNIQUE,
    Email VARCHAR(255) NOT NULL UNIQUE,
    Password VARCHAR(255) NOT NULL,
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    Phone VARCHAR(20),
    Address TEXT,
    UserType ENUM('Customer', 'Admin') NOT NULL,
    CreatedDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Indexes
    INDEX idx_username (Username),
    INDEX idx_email (Email),
    INDEX idx_usertype (UserType)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ----------------------------------------------------------------------------
-- Book Table (Updated - No StockQuantity, No ISBN+Edition Unique Constraint)
-- Each BookID represents one physical book
-- Stock quantity is calculated dynamically by counting available books
-- ----------------------------------------------------------------------------
CREATE TABLE Book (
    BookID INT AUTO_INCREMENT PRIMARY KEY,
    SubmissionID INT NULL, -- Links to SellSubmission that created this book
    ISBN VARCHAR(20) NOT NULL,
    Title VARCHAR(255) NOT NULL,
    Author VARCHAR(255) NOT NULL,
    Edition VARCHAR(50) NOT NULL,
    SellingPrice DECIMAL(10,2) NOT NULL,
    AcquisitionCost DECIMAL(10,2) NOT NULL,
    BookCondition ENUM('New', 'Good', 'Fair') NOT NULL,
    CourseMajor VARCHAR(50),
    Status ENUM('Available', 'Sold') NOT NULL DEFAULT 'Available',
    CreatedDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Foreign Keys
    CONSTRAINT fk_book_submission FOREIGN KEY (SubmissionID) 
        REFERENCES SellSubmission(SubmissionID) 
        ON DELETE SET NULL 
        ON UPDATE CASCADE,
    
    -- Constraints
    CONSTRAINT chk_selling_price_positive CHECK (SellingPrice > 0),
    CONSTRAINT chk_acquisition_cost_non_negative CHECK (AcquisitionCost >= 0),
    CONSTRAINT chk_price_margin CHECK (SellingPrice > AcquisitionCost),
    
    -- Indexes
    INDEX idx_isbn (ISBN),
    INDEX idx_status (Status),
    INDEX idx_course_major (CourseMajor),
    INDEX idx_submission_id (SubmissionID),
    INDEX idx_isbn_edition_status (ISBN, Edition, Status) -- For efficient stock counting queries
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ----------------------------------------------------------------------------
-- SellSubmission Table
-- Tracks customer submissions to sell books to the bookstore
-- ----------------------------------------------------------------------------
CREATE TABLE SellSubmission (
    SubmissionID INT AUTO_INCREMENT PRIMARY KEY,
    UserID INT NOT NULL,
    AdminUserID INT NULL,
    ISBN VARCHAR(20) NOT NULL,
    Title VARCHAR(255) NOT NULL,
    Author VARCHAR(255) NOT NULL,
    Edition VARCHAR(50) NOT NULL,
    PhysicalCondition ENUM('New', 'Good', 'Fair') NOT NULL,
    CourseMajor VARCHAR(50),
    AskingPrice DECIMAL(10,2) NOT NULL,
    Status ENUM('Pending Review', 'Approved', 'Rejected') NOT NULL DEFAULT 'Pending Review',
    SubmissionDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Foreign Keys
    CONSTRAINT fk_sellsubmission_user FOREIGN KEY (UserID) 
        REFERENCES User(UserID) 
        ON DELETE RESTRICT 
        ON UPDATE CASCADE,
    CONSTRAINT fk_sellsubmission_admin FOREIGN KEY (AdminUserID) 
        REFERENCES User(UserID) 
        ON DELETE RESTRICT 
        ON UPDATE CASCADE,
    
    -- Constraints
    CONSTRAINT chk_asking_price_positive CHECK (AskingPrice > 0),
    
    -- Indexes
    INDEX idx_user_id (UserID),
    INDEX idx_admin_user_id (AdminUserID),
    INDEX idx_status (Status),
    INDEX idx_submission_date (SubmissionDate)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ----------------------------------------------------------------------------
-- PriceNegotiation Table (NEW)
-- Tracks multi-round price negotiations between customers and admins
-- Each negotiation round creates a new record
-- ----------------------------------------------------------------------------
CREATE TABLE PriceNegotiation (
    NegotiationID INT AUTO_INCREMENT PRIMARY KEY,
    SubmissionID INT NOT NULL,
    OfferedBy ENUM('User', 'Admin') NOT NULL,
    OfferedPrice DECIMAL(10,2) NOT NULL,
    OfferDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    OfferMessage TEXT,
    OfferStatus ENUM('Pending', 'Accepted', 'Rejected') NOT NULL DEFAULT 'Pending',
    RoundNumber INT NOT NULL,
    
    -- Foreign Keys
    CONSTRAINT fk_negotiation_submission FOREIGN KEY (SubmissionID) 
        REFERENCES SellSubmission(SubmissionID) 
        ON DELETE CASCADE 
        ON UPDATE CASCADE,
    
    -- Constraints
    CONSTRAINT chk_offered_price_positive CHECK (OfferedPrice > 0),
    
    -- Indexes
    INDEX idx_submission_id (SubmissionID),
    INDEX idx_offer_status (OfferStatus),
    INDEX idx_submission_round (SubmissionID, RoundNumber)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ----------------------------------------------------------------------------
-- PurchaseOrder Table (Simplified Status)
-- Status flow: New -> Processing -> Fulfilled (or Cancelled)
-- ----------------------------------------------------------------------------
CREATE TABLE PurchaseOrder (
    OrderID INT AUTO_INCREMENT PRIMARY KEY,
    UserID INT NOT NULL,
    OrderDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Status ENUM('New', 'Processing', 'Fulfilled', 'Cancelled') NOT NULL DEFAULT 'New',
    TotalAmount DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    
    -- Foreign Keys
    CONSTRAINT fk_purchaseorder_user FOREIGN KEY (UserID) 
        REFERENCES User(UserID) 
        ON DELETE RESTRICT 
        ON UPDATE CASCADE,
    
    -- Constraints
    CONSTRAINT chk_total_amount_non_negative CHECK (TotalAmount >= 0),
    
    -- Indexes
    INDEX idx_user_id (UserID),
    INDEX idx_status (Status),
    INDEX idx_order_date (OrderDate)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ----------------------------------------------------------------------------
-- OrderLineItem Table (Removed Quantity Field)
-- Each line item represents one unique book (BookID)
-- If customer wants multiple copies, create multiple line items
-- ----------------------------------------------------------------------------
CREATE TABLE OrderLineItem (
    LineItemID INT AUTO_INCREMENT PRIMARY KEY,
    OrderID INT NOT NULL,
    BookID INT NOT NULL,
    PriceAtSale DECIMAL(10,2) NOT NULL,
    
    -- Foreign Keys
    CONSTRAINT fk_orderlineitem_order FOREIGN KEY (OrderID) 
        REFERENCES PurchaseOrder(OrderID) 
        ON DELETE CASCADE 
        ON UPDATE CASCADE,
    CONSTRAINT fk_orderlineitem_book FOREIGN KEY (BookID) 
        REFERENCES Book(BookID) 
        ON DELETE RESTRICT 
        ON UPDATE CASCADE,
    
    -- Constraints
    CONSTRAINT chk_price_at_sale_positive CHECK (PriceAtSale > 0),
    
    -- Indexes
    INDEX idx_order_id (OrderID),
    INDEX idx_book_id (BookID),
    INDEX idx_order_book (OrderID, BookID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ----------------------------------------------------------------------------
-- ShoppingCart Table (NEW)
-- Persists user shopping carts in database
-- Allows users to close browser and return to cart later
-- ----------------------------------------------------------------------------
CREATE TABLE ShoppingCart (
    CartItemID INT AUTO_INCREMENT PRIMARY KEY,
    UserID INT NOT NULL,
    BookID INT NOT NULL,
    AddedDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Foreign Keys
    CONSTRAINT fk_cart_user FOREIGN KEY (UserID) 
        REFERENCES User(UserID) 
        ON DELETE CASCADE 
        ON UPDATE CASCADE,
    CONSTRAINT fk_cart_book FOREIGN KEY (BookID) 
        REFERENCES Book(BookID) 
        ON DELETE CASCADE 
        ON UPDATE CASCADE,
    
    -- Constraints
    UNIQUE KEY uk_user_book (UserID, BookID), -- Prevent duplicate items in cart
    
    -- Indexes
    INDEX idx_user_id (UserID),
    INDEX idx_book_id (BookID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ----------------------------------------------------------------------------
-- PaymentMethod Table (NEW)
-- Stores saved payment methods per user (for demo purposes)
-- ----------------------------------------------------------------------------
CREATE TABLE PaymentMethod (
    PaymentMethodID INT AUTO_INCREMENT PRIMARY KEY,
    UserID INT NOT NULL,
    CardType VARCHAR(50) NOT NULL, -- e.g., 'Visa', 'MasterCard', 'American Express'
    LastFourDigits VARCHAR(4) NOT NULL,
    ExpirationDate VARCHAR(7) NOT NULL, -- Format: MM/YYYY
    IsDefault BOOLEAN NOT NULL DEFAULT FALSE,
    CreatedDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Foreign Keys
    CONSTRAINT fk_paymentmethod_user FOREIGN KEY (UserID) 
        REFERENCES User(UserID) 
        ON DELETE CASCADE 
        ON UPDATE CASCADE,
    
    -- Indexes
    INDEX idx_user_id (UserID),
    INDEX idx_is_default (IsDefault)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ----------------------------------------------------------------------------
-- Payment Table (Updated with PaymentMethodID)
-- Links to PaymentMethod table for saved payment methods
-- ----------------------------------------------------------------------------
CREATE TABLE Payment (
    PaymentID INT AUTO_INCREMENT PRIMARY KEY,
    OrderID INT NOT NULL,
    PaymentMethodID INT NULL, -- Links to saved payment method (nullable for one-time payments)
    PaymentDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Amount DECIMAL(10,2) NOT NULL,
    PaymentStatus ENUM('Pending', 'Completed', 'Failed', 'Refunded') NOT NULL DEFAULT 'Completed',
    TransactionID VARCHAR(100),
    
    -- Foreign Keys
    CONSTRAINT fk_payment_order FOREIGN KEY (OrderID)
        REFERENCES PurchaseOrder(OrderID)
        ON DELETE CASCADE
        ON UPDATE CASCADE,
    CONSTRAINT fk_payment_method FOREIGN KEY (PaymentMethodID)
        REFERENCES PaymentMethod(PaymentMethodID)
        ON DELETE SET NULL
        ON UPDATE CASCADE,
    
    -- Constraints
    CONSTRAINT chk_payment_amount_positive CHECK (Amount > 0),
    
    -- Indexes
    INDEX idx_order_id (OrderID),
    INDEX idx_payment_method_id (PaymentMethodID),
    INDEX idx_payment_date (PaymentDate),
    INDEX idx_payment_status (PaymentStatus)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

