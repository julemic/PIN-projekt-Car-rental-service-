using CarRentalService.Data;
using CarRentalService.Models;
using CarRentalService.Services;
using CarRentalService.Services.Pdf;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using CarRentalService.Constants;

QuestPDF.Settings.License = LicenseType.Community;






var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<AccidentReportPdfGenerator>();
builder.Services.AddTransient<IEmailSender, FakeEmailSender>();



builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;

        options.Password.RequiredLength = 7;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

builder.Services.AddControllersWithViews(options =>
    options.Filters.Add<CarRentalService.Filters.DbExceptionFilter>());
builder.Services.AddRazorPages();
builder.Services.AddScoped<IRentalPricingService, RentalPricingService>();
builder.Services.AddScoped<Ganss.Xss.HtmlSanitizer>();


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

//  Seed admin korisnika i role 
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var config = services.GetRequiredService<IConfiguration>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    var adminEmail = config["AdminSettings:Email"];
    var adminPassword = config["AdminSettings:Password"];
    if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        throw new InvalidOperationException("Admin credentials missing from secrets.json");

    if (!await roleManager.RoleExistsAsync(Roles.Admin))
    {
        await roleManager.CreateAsync(new IdentityRole(Roles.Admin));
    }

    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,

            // Minimal required fields
            FirstName = "Admin",
            LastName = "User",
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            ResidenceAddress = "Admin Address",
            City = "Admin City",
            Nationality = "HR",
            Oib = "12345678901",
            DriverLicenseNumber = "12345678",
            IdCardNumber = "ABC123456",
            IsVerified = true,
            VerifiedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);

        if (!result.Succeeded)
        {
            throw new Exception("Admin creation failed: " +
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    if (!await userManager.IsInRoleAsync(adminUser, Roles.Admin))
    {
        await userManager.AddToRoleAsync(adminUser, Roles.Admin);
    }
}



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

await DbInitializer.SeedAsync(app.Services);

app.Run();
