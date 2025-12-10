namespace finalproject.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // SHIRTS, PANTS, SHOES, DRESS
    public string SubCategory { get; set; } = string.Empty; // MENS, WOMENS, MAXI DRESS, etc.
    public decimal Price { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Stock { get; set; } = 100;
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}

