namespace CrimsonBookStore.Models;

public class Book
{
    public int BookID { get; set; }
    public int? SubmissionID { get; set; }
    public string ISBN { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty;
    public decimal SellingPrice { get; set; }
    public decimal AcquisitionCost { get; set; }
    public string BookCondition { get; set; } = string.Empty; // 'New', 'Good', 'Fair'
    public string? CourseMajor { get; set; }
    public string Status { get; set; } = "Available"; // 'Available' or 'Sold'
    public DateTime CreatedDate { get; set; }
}

