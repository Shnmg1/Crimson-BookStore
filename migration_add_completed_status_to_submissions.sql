-- Migration: Add 'Completed' status to SellSubmission.Status ENUM
-- Run this SQL command to update your database schema

ALTER TABLE SellSubmission 
MODIFY COLUMN Status ENUM('Pending Review', 'Approved', 'Rejected', 'Completed') NOT NULL DEFAULT 'Pending Review';

