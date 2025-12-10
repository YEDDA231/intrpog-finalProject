using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using finalproject.Models;
using System.IO;

namespace finalproject.Data;

public static class SeedData
{
    public static async Task InitializeAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        // Ensure database exists and is migrated
        try
        {
            if (!context.Database.CanConnect())
            {
                await context.Database.EnsureCreatedAsync();
            }
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            // If migration fails, try to create database
            try
            {
                await context.Database.EnsureCreatedAsync();
            }
            catch
            {
                // If both fail, log and continue - database might need manual setup
                throw new Exception("Database initialization failed. Please check your connection string.", ex);
            }
        }
        
        // Create roles
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }
        if (!await roleManager.RoleExistsAsync("User"))
        {
            await roleManager.CreateAsync(new IdentityRole("User"));
        }

        // Create admin user
        if (await userManager.FindByEmailAsync("admin@joebnfriends.com") == null)
        {
            var admin = new ApplicationUser
            {
                UserName = "admin@joebnfriends.com",
                Email = "admin@joebnfriends.com",
                FullName = "Admin User",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(admin, "admin123");
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        // Create test user
        if (await userManager.FindByEmailAsync("user@test.com") == null)
        {
            var user = new ApplicationUser
            {
                UserName = "user@test.com",
                Email = "user@test.com",
                FullName = "Test User",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(user, "user123");
            await userManager.AddToRoleAsync(user, "User");
        }

        // Seed products if database is empty
        if (!context.Products.Any())
        {
            var products = new List<Product>();

            // Shirts - Men's
            for (int i = 1; i <= 10; i++)
            {
                products.Add(new Product
                {
                    Name = $"Men's Shirt {i}",
                    Category = "SHIRTS",
                    SubCategory = "MENS",
                    Price = 29.99m + (i * 5),
                    ImagePath = $"/images/SHIRTS/MENS/{i}.PNG",
                    Description = $"Stylish men's shirt #{i}",
                    Stock = 50
                });
            }

            // Shirts - Women's
            for (int i = 1; i <= 10; i++)
            {
                products.Add(new Product
                {
                    Name = $"Women's Shirt {i}",
                    Category = "SHIRTS",
                    SubCategory = "WOMENS",
                    Price = 24.99m + (i * 5),
                    ImagePath = $"/images/SHIRTS/WOMENS/{i}.PNG",
                    Description = $"Elegant women's shirt #{i}",
                    Stock = 50
                });
            }

            // Pants - Men's
            for (int i = 1; i <= 10; i++)
            {
                products.Add(new Product
                {
                    Name = $"Men's Pants {i}",
                    Category = "PANTS",
                    SubCategory = "MENS",
                    Price = 49.99m + (i * 5),
                    ImagePath = $"/images/PANTS/MENS/{i}.PNG",
                    Description = $"Comfortable men's pants #{i}",
                    Stock = 40
                });
            }

            // Pants - Women's
            for (int i = 1; i <= 10; i++)
            {
                products.Add(new Product
                {
                    Name = $"Women's Pants {i}",
                    Category = "PANTS",
                    SubCategory = "WOMENS",
                    Price = 44.99m + (i * 5),
                    ImagePath = $"/images/PANTS/WOMENS/{i}.PNG",
                    Description = $"Stylish women's pants #{i}",
                    Stock = 40
                });
            }

            // Shoes - Men's
            for (int i = 1; i <= 10; i++)
            {
                products.Add(new Product
                {
                    Name = $"Men's Shoes {i}",
                    Category = "SHOES",
                    SubCategory = "MENS",
                    Price = 79.99m + (i * 10),
                    ImagePath = $"/images/SHOES/MENS/{i}.PNG",
                    Description = $"Quality men's shoes #{i}",
                    Stock = 30
                });
            }

            // Shoes - Women's
            for (int i = 1; i <= 10; i++)
            {
                products.Add(new Product
                {
                    Name = $"Women's Shoes {i}",
                    Category = "SHOES",
                    SubCategory = "WOMENS",
                    Price = 69.99m + (i * 10),
                    ImagePath = $"/images/SHOES/WOMENS/{i}.PNG",
                    Description = $"Elegant women's shoes #{i}",
                    Stock = 30
                });
            }

            // Dresses - Maxi
            var maxiDresses = new[] { "BLACK MAXI", "BLUEMAXIDRESS", "MAXIFLORAL DRESS", "PINK MAXI", "WHITEMAXIDRESS", "YELLOW MAXI" };
            foreach (var dress in maxiDresses)
            {
                products.Add(new Product
                {
                    Name = dress.Replace("MAXI", "Maxi Dress").Replace("DRESS", "").Trim(),
                    Category = "DRESS",
                    SubCategory = "MAXI DRESS",
                    Price = 59.99m,
                    ImagePath = $"/images/DRESS/MAXI DRESS/{dress}.PNG",
                    Description = $"Beautiful maxi dress - {dress}",
                    Stock = 25
                });
            }

            // Dresses - Midi
            var midiDresses = new[] { "BLACKMIDI DRESS", "BROWNMIDI", "MIDIBLUETUBE DRESS", "MIDIGREEN DRESS", "RED MIDI", "WHITE MIDI" };
            foreach (var dress in midiDresses)
            {
                products.Add(new Product
                {
                    Name = dress.Replace("MIDI", "Midi Dress").Replace("DRESS", "").Trim(),
                    Category = "DRESS",
                    SubCategory = "MIDI DRESS",
                    Price = 54.99m,
                    ImagePath = $"/images/DRESS/MIDI DRESS/{dress}.PNG",
                    Description = $"Elegant midi dress - {dress}",
                    Stock = 25
                });
            }

            // Dresses - Mini
            var miniDresses = new[] { "BABYPINKDRESS", "BLACKDRESS", "BLUEDRESS", "BROWN LEATHER DRESS", "DENIMSTYLEDRESS", "REDDRESS" };
            foreach (var dress in miniDresses)
            {
                products.Add(new Product
                {
                    Name = dress.Replace("DRESS", "Mini Dress").Trim(),
                    Category = "DRESS",
                    SubCategory = "MINI DRESS",
                    Price = 49.99m,
                    ImagePath = $"/images/DRESS/MINI DRESS/{dress}.PNG",
                    Description = $"Chic mini dress - {dress}",
                    Stock = 25
                });
            }

            context.Products.AddRange(products);
            await context.SaveChangesAsync();
        }
    }
}

