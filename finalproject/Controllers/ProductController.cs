using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using finalproject.Data;
using finalproject.Models;

namespace finalproject.Controllers;

public class ProductController : Controller
{
    private readonly ApplicationDbContext _context;

    public ProductController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? category = null, string? subCategory = null, string? gender = null)
    {
        var products = _context.Products.AsQueryable();

        if (!string.IsNullOrEmpty(category))
        {
            products = products.Where(p => p.Category == category);
        }

        if (!string.IsNullOrEmpty(subCategory))
        {
            products = products.Where(p => p.SubCategory == subCategory);
        }

        // Gender filtering: MENS or WOMENS
        if (!string.IsNullOrEmpty(gender))
        {
            if (gender.ToUpper() == "MENS")
            {
                // For MENS: only show products with SubCategory "MENS" (excludes WOMENS and all DRESS items)
                products = products.Where(p => p.SubCategory == "MENS");
            }
            else if (gender.ToUpper() == "WOMENS")
            {
                // For WOMENS: include WOMENS subcategory OR any DRESS category (all dresses are women's)
                products = products.Where(p => p.SubCategory == "WOMENS" || p.Category == "DRESS");
            }
        }

        ViewBag.Category = category;
        ViewBag.SubCategory = subCategory;
        ViewBag.Gender = gender;
        ViewBag.Categories = await _context.Products.Select(p => p.Category).Distinct().ToListAsync();
        ViewBag.SubCategories = await _context.Products.Select(p => p.SubCategory).Distinct().ToListAsync();

        return View(await products.ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        // Get product with current stock from database
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
            
        if (product == null)
        {
            return NotFound();
        }

        // Get related products
        var relatedProducts = await _context.Products
            .Where(p => p.Category == product.Category && p.Id != product.Id)
            .Take(4)
            .ToListAsync();

        ViewBag.RelatedProducts = relatedProducts;
        return View(product);
    }
}

