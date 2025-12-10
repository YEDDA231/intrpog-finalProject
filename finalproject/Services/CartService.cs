using System.Text.Json;
using finalproject.Models;
using Microsoft.AspNetCore.Http;

namespace finalproject.Services;

public class CartService
{
    private const string CartSessionKey = "Cart";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CartService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ISession Session => _httpContextAccessor.HttpContext!.Session;

    public List<CartItem> GetCart()
    {
        var cartJson = Session.GetString(CartSessionKey);
        if (string.IsNullOrEmpty(cartJson))
        {
            return new List<CartItem>();
        }
        return JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
    }

    public void SaveCart(List<CartItem> cart)
    {
        var cartJson = JsonSerializer.Serialize(cart);
        Session.SetString(CartSessionKey, cartJson);
    }

    public void AddToCart(Product product, int quantity = 1)
    {
        var cart = GetCart();
        var existingItem = cart.FirstOrDefault(x => x.ProductId == product.Id);

        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            cart.Add(new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductImage = product.ImagePath,
                Price = product.Price,
                Quantity = quantity
            });
        }

        SaveCart(cart);
    }

    public void UpdateQuantity(int productId, int quantity)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(x => x.ProductId == productId);
        
        if (item != null)
        {
            if (quantity <= 0)
            {
                cart.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
            }
            SaveCart(cart);
        }
    }

    public void RemoveFromCart(int productId)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(x => x.ProductId == productId);
        
        if (item != null)
        {
            cart.Remove(item);
            SaveCart(cart);
        }
    }

    public void ClearCart()
    {
        Session.Remove(CartSessionKey);
    }

    public int GetCartCount()
    {
        return GetCart().Sum(x => x.Quantity);
    }

    public decimal GetCartTotal()
    {
        return GetCart().Sum(x => x.Subtotal);
    }
}

