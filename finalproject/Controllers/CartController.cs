using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using finalproject.Data;
using finalproject.Models;
using finalproject.Services;

namespace finalproject.Controllers;

public class CartController : Controller
{
    private readonly CartService _cartService;
    private readonly ApplicationDbContext _context;

    public CartController(CartService cartService, ApplicationDbContext context)
    {
        _cartService = cartService;
        _context = context;
    }

    public IActionResult Index()
    {
        var cartItems = _cartService.GetCart();
        var productIds = cartItems.Select(x => x.ProductId).ToList();
        var products = _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionary(p => p.Id);

        var cartViewModel = cartItems.Select(item =>
        {
            var product = products.GetValueOrDefault(item.ProductId);
            return new CartItemViewModel
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                ProductImage = item.ProductImage,
                Price = item.Price,
                Quantity = item.Quantity,
                Subtotal = item.Subtotal,
                Stock = product?.Stock ?? 0
            };
        }).ToList();

        ViewBag.CartTotal = _cartService.GetCartTotal();
        ViewBag.CartCount = _cartService.GetCartCount();

        return View(cartViewModel);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
    {
        // Prevent admins from adding items to cart
        if (User.IsInRole("Admin"))
        {
            return Json(new { success = false, message = "Admins cannot add items to cart" });
        }

        if (request == null || request.ProductId <= 0)
        {
            return Json(new { success = false, message = "Invalid product ID" });
        }

        // Get product from database with current stock
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId);

        if (product == null)
        {
            return Json(new { success = false, message = "Product not found" });
        }

        var quantity = request.Quantity > 0 ? request.Quantity : 1;

        // Check available stock
        if (product.Stock <= 0)
        {
            return Json(new { success = false, message = "This product is out of stock" });
        }

        if (product.Stock < quantity)
        {
            return Json(new { success = false, message = $"Only {product.Stock} item(s) available in stock" });
        }

        _cartService.AddToCart(product, quantity);
        return Json(new { 
            success = true, 
            cartCount = _cartService.GetCartCount(), 
            message = "Product added to cart!",
            availableStock = product.Stock - quantity
        });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UpdateQuantity([FromBody] UpdateQuantityRequest request)
    {
        if (request == null || request.ProductId <= 0)
        {
            return Json(new { success = false, message = "Invalid request" });
        }

        var productId = request.ProductId;
        var quantity = request.Quantity;

        if (quantity <= 0)
        {
            _cartService.RemoveFromCart(productId);
        }
        else
        {
            // Check stock availability from database
            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId);
                
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            if (product.Stock < quantity)
            {
                return Json(new { 
                    success = false, 
                    message = $"Only {product.Stock} items available in stock" 
                });
            }
            
            _cartService.UpdateQuantity(productId, quantity);
        }

        var cartTotal = _cartService.GetCartTotal();
        var cartCount = _cartService.GetCartCount();
        var item = _cartService.GetCart().FirstOrDefault(x => x.ProductId == productId);
        var itemSubtotal = item?.Subtotal ?? 0;

        return Json(new { 
            success = true, 
            cartTotal = cartTotal.ToString("F2"), 
            cartCount = cartCount,
            itemSubtotal = itemSubtotal.ToString("F2")
        });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult RemoveFromCart([FromBody] RemoveFromCartRequest request)
    {
        if (request == null || request.ProductId <= 0)
        {
            return Json(new { success = false, message = "Invalid request" });
        }

        _cartService.RemoveFromCart(request.ProductId);
        
        var cartTotal = _cartService.GetCartTotal();
        var cartCount = _cartService.GetCartCount();

        return Json(new { 
            success = true, 
            cartTotal = cartTotal.ToString("F2"), 
            cartCount = cartCount,
            message = "Item removed from cart"
        });
    }

    [HttpGet]
    public IActionResult GetCartCount()
    {
        return Json(new { count = _cartService.GetCartCount() });
    }
}

public class CartItemViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductImage { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal Subtotal { get; set; }
    public int Stock { get; set; }
}

public class AddToCartRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class UpdateQuantityRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class RemoveFromCartRequest
{
    public int ProductId { get; set; }
}

