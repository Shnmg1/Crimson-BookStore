namespace CrimsonBookStore.Models;

public class UpdateBookRequest
{
    public decimal? SellingPrice { get; set; }
    public decimal? AcquisitionCost { get; set; }
    public string? BookCondition { get; set; } // 'New', 'Good', 'Fair'
    public string? CourseMajor { get; set; }
    public string? Status { get; set; } // 'Available' or 'Sold'
}

