namespace CrimsonBookStore.Models;

public class CreateBookRequest
{
    public string ISBN { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty;
    public decimal SellingPrice { get; set; }
    public decimal AcquisitionCost { get; set; }
    public string BookCondition { get; set; } = string.Empty; // 'New', 'Good', 'Fair'
    public string? CourseMajor { get; set; }
}

