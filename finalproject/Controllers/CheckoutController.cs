using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using finalproject.Data;
using finalproject.Models;
using finalproject.Services;

namespace finalproject.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly CartService _cartService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CheckoutController(
        CartService cartService, 
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _cartService = cartService;
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var cartItems = _cartService.GetCart();
        if (!cartItems.Any())
        {
            TempData["Error"] = "Your cart is empty.";
            return RedirectToAction("Index", "Cart");
        }

        var userId = _userManager.GetUserId(User);
        var user = _userManager.GetUserAsync(User).Result;

        var checkoutViewModel = new CheckoutViewModel
        {
            CartItems = cartItems,
            TotalAmount = _cartService.GetCartTotal(),
            ShippingAddress = user?.Address ?? string.Empty,
            FullName = user?.FullName ?? string.Empty,
            Email = user?.Email ?? string.Empty
        };

        return View(checkoutViewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
    {
        var cartItems = _cartService.GetCart();
        if (!cartItems.Any())
        {
            TempData["Error"] = "Your cart is empty.";
            return RedirectToAction("Index", "Cart");
        }

        // Validate stock availability
        var productIds = cartItems.Select(x => x.ProductId).ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        foreach (var cartItem in cartItems)
        {
            if (!products.ContainsKey(cartItem.ProductId))
            {
                TempData["Error"] = $"Product {cartItem.ProductName} is no longer available.";
                return RedirectToAction("Index", "Cart");
            }

            var product = products[cartItem.ProductId];
            if (product.Stock < cartItem.Quantity)
            {
                TempData["Error"] = $"Insufficient stock for {cartItem.ProductName}. Only {product.Stock} available.";
                return RedirectToAction("Index", "Cart");
            }
        }

        // Create order using transaction to ensure data consistency
        var userId = _userManager.GetUserId(User);
        int orderId;
        
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = new Order
            {
                UserId = userId!,
                OrderDate = DateTime.Now,
                TotalAmount = _cartService.GetCartTotal(),
                Status = "Pending",
                ShippingAddress = model.ShippingAddress
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            orderId = order.Id;

            // Create order items and update stock
            foreach (var cartItem in cartItems)
            {
                var product = products[cartItem.ProductId];
                
                // Double-check stock before updating (in case it changed)
                if (product.Stock < cartItem.Quantity)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = $"Insufficient stock for {cartItem.ProductName}. Only {product.Stock} available.";
                    return RedirectToAction("Index", "Cart");
                }
                
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity,
                    Price = cartItem.Price
                };

                _context.OrderItems.Add(orderItem);

                // Update product stock
                product.Stock -= cartItem.Quantity;
                _context.Products.Update(product);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Update user address if provided
            var user = await _userManager.GetUserAsync(User);
            if (user != null && !string.IsNullOrEmpty(model.ShippingAddress))
            {
                user.Address = model.ShippingAddress;
                await _userManager.UpdateAsync(user);
            }

            // Clear cart only after successful order creation
            _cartService.ClearCart();

            TempData["Success"] = $"Order placed successfully! Order ID: #{orderId}";
            return RedirectToAction("OrderConfirmation", new { orderId = orderId });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = "An error occurred while processing your order. Please try again.";
            return RedirectToAction("Index", "Cart");
        }
    }

    [HttpGet]
    public async Task<IActionResult> OrderConfirmation(int orderId)
    {
        var userId = _userManager.GetUserId(User);
        var order = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }
}

public class CheckoutViewModel
{
    public List<CartItem> CartItems { get; set; } = new();
    public decimal TotalAmount { get; set; }
    
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;
    
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.Display(Name = "Shipping Address")]
    public string ShippingAddress { get; set; } = string.Empty;
}

