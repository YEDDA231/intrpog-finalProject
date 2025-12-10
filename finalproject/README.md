# JOEB N FRIENDS - Fashion Store

A modern e-commerce fashion store built with ASP.NET Core MVC, featuring user authentication, product catalog, and admin panel.

## Features

- **Product Catalog**: Browse products by category (Shirts, Pants, Shoes, Dresses)
- **User Authentication**: Login and registration system
- **Admin Panel**: Manage products, view statistics
- **Modern UI**: Responsive design inspired by Forever 21
- **Product Images**: All product images from the PICTURES folder

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- Visual Studio 2022 or VS Code

### Running the Application

1. Navigate to the `finalproject` directory
2. Run the application:
   ```bash
   dotnet run
   ```
3. Open your browser and navigate to `https://localhost:5001` or `http://localhost:5000`

### Default Accounts

**Admin Account:**
- Email: `admin@joebnfriends.com`
- Password: `admin123`

**User Account:**
- Email: `user@test.com`
- Password: `user123`

## Project Structure

- `Controllers/` - MVC controllers (Home, Product, Account, Admin)
- `Models/` - Data models (Product, ApplicationUser)
- `Views/` - Razor views for all pages
- `Data/` - Database context and seed data
- `wwwroot/images/` - Product images
- `wwwroot/css/` - Custom styles

## Features Breakdown

### Public Pages
- **Home Page**: Featured products and category navigation
- **Product Catalog**: Browse and filter products by category
- **Product Details**: View individual product information

### User Features
- **Registration**: Create a new account
- **Login**: Access your account
- **Product Browsing**: View all available products

### Admin Features
- **Dashboard**: View product statistics
- **Product Management**: Create, edit, and delete products
- **Stock Management**: Monitor product inventory

## Technologies Used

- ASP.NET Core 9.0 MVC
- Entity Framework Core (In-Memory Database)
- ASP.NET Core Identity
- Bootstrap 5
- Font Awesome Icons

## Database

The application uses **SQLite** database stored in `fashionstore.db` file in the project root.

- The database is automatically created on first run using `EnsureCreatedAsync()`
- All product data is seeded automatically on first run
- Data persists between application restarts (unlike in-memory database)

### Database Location
- Database file: `fashionstore.db` (in project root)
- Connection string: `Data Source=fashionstore.db` (configured in `appsettings.json`)

### Using Migrations (Optional)

If you want to use Entity Framework migrations instead of `EnsureCreatedAsync()`:

1. **Stop the running application**

2. **Create the initial migration:**
   ```bash
   dotnet ef migrations add InitialCreate
   ```

3. **Apply the migration:**
   ```bash
   dotnet ef database update
   ```

4. **Update Program.cs** to use migrations instead of `EnsureCreatedAsync()`:
   - Replace `await context.Database.EnsureCreatedAsync();` with `await context.Database.MigrateAsync();`

## Notes

- Product images are stored in `wwwroot/images/`
- All product data is seeded automatically on first run
- The database file can be viewed using SQLite browser tools

