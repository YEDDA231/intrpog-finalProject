using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using finalproject.Data;
using finalproject.Models;
using System.Text;
using System.IO;

namespace finalproject.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    public async Task<IActionResult> ProductsList()
    {
        var products = await _context.Products.OrderByDescending(p => p.CreatedDate).ToListAsync();
        return View(products);
    }

    [HttpGet]
    public async Task<IActionResult> Orders()
    {
        var orders = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> OrderDetails(int id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        return PartialView("_OrderDetails", order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStock([FromForm] int productId, [FromForm] int? stockChange, [FromForm] int? newStock)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            return Json(new { success = false, message = "Product not found" });
        }

        if (newStock.HasValue)
        {
            // Set stock to specific value
            product.Stock = Math.Max(0, newStock.Value);
        }
        else if (stockChange.HasValue)
        {
            // Adjust stock by change amount
            product.Stock = Math.Max(0, product.Stock + stockChange.Value);
        }
        else
        {
            return Json(new { success = false, message = "No stock value provided" });
        }

        _context.Update(product);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Stock updated successfully", newStock = product.Stock });
    }

    public async Task<IActionResult> Index()
    {
        try
        {
        var products = await _context.Products.ToListAsync();
        var users = await _context.Users.ToListAsync();
        var orders = await _context.Orders.ToListAsync();

        // User Stats (all users including admin)
        var totalCustomers = users.Count;
        var newCustomersToday = users.Count(u => u.CreatedDate.Date == DateTime.Today);
        var newCustomersWeek = users.Count(u => u.CreatedDate >= DateTime.Today.AddDays(-7));

        // Sales Overview
        var todaySales = orders.Where(o => o.OrderDate.Date == DateTime.Today).Sum(o => o.TotalAmount);
        var weekSales = orders.Where(o => o.OrderDate >= DateTime.Today.AddDays(-7)).Sum(o => o.TotalAmount);
        var monthSales = orders.Where(o => o.OrderDate.Month == DateTime.Now.Month && o.OrderDate.Year == DateTime.Now.Year).Sum(o => o.TotalAmount);
        var totalSales = orders.Sum(o => o.TotalAmount);

        // Orders Overview
        var totalOrders = orders.Count;
        var pendingOrders = orders.Count(o => o.Status == "Pending");
        var processingOrders = orders.Count(o => o.Status == "Processing");
        var completedOrders = orders.Count(o => o.Status == "Delivered" || o.Status == "Completed");

        // Pending Orders for Approval - Query separately with Include
        var pendingOrdersList = await _context.Orders
            .Where(o => o.Status == "Pending")
            .Include(o => o.User)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.OrderDate)
            .Take(10)
            .ToListAsync();

        // Processing Orders - Orders that need to be shipped or delivered
        var processingOrdersList = await _context.Orders
            .Where(o => o.Status == "Processing")
            .Include(o => o.User)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.OrderDate)
            .Take(10)
            .ToListAsync();

        // Purchased Products - Get all order items with product info
        var purchasedProducts = await _context.OrderItems
            .Include(oi => oi.Product)
            .Include(oi => oi.Order)
            .Where(oi => oi.Order.Status != "Cancelled")
            .GroupBy(oi => new { oi.ProductId, oi.Product.Name, oi.Product.Category, oi.Product.Price })
            .Select(g => new
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.Name,
                Category = g.Key.Category,
                Price = g.Key.Price,
                TotalQuantitySold = g.Sum(oi => oi.Quantity),
                TotalRevenue = g.Sum(oi => oi.Quantity * oi.Price),
                NumberOfOrders = g.Select(oi => oi.OrderId).Distinct().Count()
            })
            .OrderByDescending(p => p.TotalQuantitySold)
            .ToListAsync();

        // Low Stock Alerts
        var lowStockProducts = products.Where(p => p.Stock < 10).ToList();

        // Notifications
        var notifications = new List<object>
        {
            new { Type = "Stock", Message = $"{lowStockProducts.Count} products are low in stock", Time = "1 hour ago", Priority = "medium" },
            new { Type = "Customer", Message = $"{newCustomersToday} new customers today", Time = "2 hours ago", Priority = "low" },
            new { Type = "Orders", Message = $"{pendingOrders} orders pending approval", Time = "30 mins ago", Priority = pendingOrders > 0 ? "high" : "low" }
        };

        ViewBag.TotalCustomers = totalCustomers;
        ViewBag.NewCustomersToday = newCustomersToday;
        ViewBag.NewCustomersWeek = newCustomersWeek;
        ViewBag.TodaySales = todaySales;
        ViewBag.WeekSales = weekSales;
        ViewBag.MonthSales = monthSales;
        ViewBag.TotalSales = totalSales;
        ViewBag.TotalOrders = totalOrders;
        ViewBag.PendingOrders = pendingOrders;
        ViewBag.ProcessingOrders = processingOrders;
        ViewBag.CompletedOrders = completedOrders;
        ViewBag.PendingOrdersList = pendingOrdersList;
        ViewBag.ProcessingOrdersList = processingOrdersList;
        ViewBag.PurchasedProducts = purchasedProducts;
        ViewBag.LowStockProducts = lowStockProducts;
        ViewBag.Notifications = notifications;
        ViewBag.AllProducts = products;

            return View();
        }
        catch (Exception)
        {
            // Log error and return view with empty data
            ViewBag.Error = "An error occurred while loading dashboard data.";
            return View();
        }
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product, IFormFile? imageFile, List<IFormFile>? imageFiles)
    {
        if (ModelState.IsValid)
        {
            product.CreatedDate = DateTime.Now;

            // Handle single image upload
            if (imageFile != null && imageFile.Length > 0)
            {
                var imagePath = await SaveImage(imageFile, product.Category, product.SubCategory);
                if (!string.IsNullOrEmpty(imagePath))
                {
                    product.ImagePath = imagePath;
                }
            }

            // Handle multiple images (folder upload)
            if (imageFiles != null && imageFiles.Count > 0)
            {
                // For folder upload, use the first image as the main image
                var firstImage = imageFiles[0];
                var imagePath = await SaveImage(firstImage, product.Category, product.SubCategory);
                if (!string.IsNullOrEmpty(imagePath))
                {
                    product.ImagePath = imagePath;
                }
            }

            // If no image uploaded and ImagePath is empty, set a default
            if (string.IsNullOrEmpty(product.ImagePath))
            {
                product.ImagePath = "/images/placeholder.png";
            }

            _context.Add(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(product);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Product product, IFormFile? imageFile, List<IFormFile>? imageFiles)
    {
        if (id != product.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existingProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                if (existingProduct == null)
                {
                    return NotFound();
                }

                // Handle single image upload
                if (imageFile != null && imageFile.Length > 0)
                {
                    var imagePath = await SaveImage(imageFile, product.Category, product.SubCategory);
                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        product.ImagePath = imagePath;
                    }
                }
                else if (imageFiles != null && imageFiles.Count > 0)
                {
                    // Handle multiple images (folder upload)
                    var firstImage = imageFiles[0];
                    var imagePath = await SaveImage(firstImage, product.Category, product.SubCategory);
                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        product.ImagePath = imagePath;
                    }
                }
                else
                {
                    // Keep existing image path if no new image uploaded
                    product.ImagePath = existingProduct.ImagePath;
                }

                _context.Update(product);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(product.Id))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(product);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }
        return View(product);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product != null)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }


    [HttpGet]
    public async Task<IActionResult> ExportUsers()
    {
        var users = await _context.Users.ToListAsync();
        var csv = new StringBuilder();
        
        // CSV Header - Note: Password is hashed, plaintext cannot be retrieved
        csv.AppendLine("Username,Email,Password,Password_Hash,Role,Date_Created");
        
        // CSV Data
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "User";
            
            // Escape commas and quotes in CSV values
            var username = EscapeCsvField(user.UserName ?? "");
            var email = EscapeCsvField(user.Email ?? "");
            var passwordHash = EscapeCsvField(user.PasswordHash ?? "");
            var password = passwordHash; // Password hash (plaintext cannot be retrieved)
            var roleField = EscapeCsvField(role);
            var dateCreated = user.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss");
            
            csv.AppendLine($"{username},{email},{password},{passwordHash},{roleField},{dateCreated}");
        }
        
        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"users_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    }

    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";
        
        // If field contains comma, quote, or newline, wrap in quotes and escape quotes
        if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
        {
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }
        
        return field;
    }

    private async Task<string> SaveImage(IFormFile file, string category, string subCategory)
    {
        if (file == null || file.Length == 0)
            return string.Empty;

        try
        {
            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                return string.Empty;
            }

            // Create directory structure: wwwroot/images/CATEGORY/SUBCATEGORY/
            var uploadPath = Path.Combine("wwwroot", "images", category, subCategory);
            var fullUploadPath = Path.Combine(Directory.GetCurrentDirectory(), uploadPath);

            // Ensure directory exists
            if (!Directory.Exists(fullUploadPath))
            {
                Directory.CreateDirectory(fullUploadPath);
            }

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(fullUploadPath, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return relative path for web access
            return $"/images/{category}/{subCategory}/{fileName}";
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatus([FromForm] int orderId, [FromForm] string status)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
        {
            return Json(new { success = false, message = "Order not found" });
        }

        // Validate status
        var validStatuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled", "Completed" };
        if (!validStatuses.Contains(status))
        {
            return Json(new { success = false, message = "Invalid status" });
        }

        order.Status = status;
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = $"Order status updated to {status}" });
    }

    private bool ProductExists(int id)
    {
        return _context.Products.Any(e => e.Id == id);
    }
}

