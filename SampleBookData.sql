USE crimsonbookstore;

/*
3 copies of "Database Systems" (5th edition) with different conditions and prices
2 copies of "Introduction to Programming" (3rd edition)
1 copy each of "Calculus I" and "Organic Chemistry" */
-- Insert sample books
INSERT INTO Book (ISBN, Title, Author, Edition, SellingPrice, AcquisitionCost, BookCondition, CourseMajor, Status, CreatedDate)
VALUES 
    ('978-0123456789', 'Database Systems', 'John Smith', '5th', 49.99, 25.00, 'Good', 'MIS 301', 'Available', NOW()),
    ('978-0123456789', 'Database Systems', 'John Smith', '5th', 54.99, 25.00, 'New', 'MIS 301', 'Available', NOW()),
    ('978-0123456789', 'Database Systems', 'John Smith', '5th', 45.99, 25.00, 'Fair', 'MIS 301', 'Available', NOW()),
    ('978-0987654321', 'Introduction to Programming', 'Jane Doe', '3rd', 39.99, 20.00, 'Good', 'CS 101', 'Available', NOW()),
    ('978-0987654321', 'Introduction to Programming', 'Jane Doe', '3rd', 39.99, 20.00, 'Good', 'CS 101', 'Available', NOW()),
    ('978-1111111111', 'Calculus I', 'Robert Johnson', '10th', 89.99, 45.00, 'New', 'MATH 125', 'Available', NOW()),
    ('978-2222222222', 'Organic Chemistry', 'Sarah Williams', '8th', 79.99, 40.00, 'Good', 'CHEM 101', 'Available', NOW());