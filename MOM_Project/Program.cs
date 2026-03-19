using Microsoft.EntityFrameworkCore;
using MOM_Project.Filters; // Required to access ApplicationDbContext and DbInitializer

var builder = WebApplication.CreateBuilder(args);

// --- 1. DATABASE CONNECTION ---
// Pulls the connection string from appsettings.json

// --- 2. SESSION CONFIGURATION ---
// Sets up the session storage for Admin Login
// 🌟 ADD THESE LINES 🌟
// 1. Allows our AuthService to access the HttpContext (Session)
builder.Services.AddHttpContextAccessor(); 

// 2. Registers our custom AuthService as a Scoped service
builder.Services.AddScoped<MOM_Project.Services.AuthService>();

// 3. Registers our custom Action Filter
builder.Services.AddScoped<MOM_Project.Filters.CheckAccessFilter>();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session expires after 30 mins
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<MOM_Project.Filters.CheckAccessFilter>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// --- 3. ENABLE SESSION MIDDLEWARE ---
// Must be placed BEFORE MapControllerRoute
app.UseSession();

app.UseAuthorization();

// --- 4. DEFAULT ROUTE ---
// Starts the application at the Login Page
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");


app.Run();