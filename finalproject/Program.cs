using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using finalproject.Data;
using finalproject.Models;
using finalproject.Services;


var contentRoot = Directory.GetCurrentDirectory();
var wwwrootPath = Path.Combine(contentRoot, "wwwroot");


if (!Directory.Exists(wwwrootPath))
{
   
    var baseDir = AppContext.BaseDirectory;
    var testPaths = new[]
    {
        contentRoot, 
        baseDir, 
        Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..")), 
        Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..")), 
        Path.GetDirectoryName(baseDir) ?? contentRoot 
    };

    foreach (var testPath in testPaths)
    {
        var testWwwroot = Path.Combine(testPath, "wwwroot");
        if (Directory.Exists(testWwwroot))
        {
            contentRoot = testPath;
            wwwrootPath = testWwwroot;
            break;
        }
    }
}


if (!Directory.Exists(wwwrootPath))
{
    Directory.CreateDirectory(wwwrootPath);
}

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    ContentRootPath = contentRoot
});


builder.Services.AddControllersWithViews();


builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CartService>();


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 4;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        
        logger.LogInformation("Initializing database...");
        
        
        if (context.Database.CanConnect())
        {
            logger.LogInformation("Database connection successful.");
        }
        else
        {
            logger.LogWarning("Database connection failed. Attempting to create database...");
        }
        
        
        await SeedData.InitializeAsync(context, userManager, roleManager);
        
        logger.LogInformation("Database initialization completed successfully.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
        
    }
}

app.Run();
