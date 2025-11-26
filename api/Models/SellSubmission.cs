namespace CrimsonBookStore.Models;

/// <summary>
/// Entity model for SellSubmission table
/// Represents a customer submission to sell a book to the bookstore
/// </summary>
public class SellSubmission
{
    public int SubmissionID { get; set; }
    public int UserID { get; set; }
    public int? AdminUserID { get; set; }
    public string ISBN { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty;
    public string PhysicalCondition { get; set; } = string.Empty; // 'New', 'Good', 'Fair'
    public string? CourseMajor { get; set; }
    public decimal AskingPrice { get; set; }
    public string Status { get; set; } = "Pending Review"; // 'Pending Review', 'Approved', 'Rejected'
    public DateTime SubmissionDate { get; set; }
}

