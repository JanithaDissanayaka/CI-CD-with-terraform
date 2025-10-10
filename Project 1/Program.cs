using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Project_1.Data.Services;
using Project_1.Data;
using Microsoft.AspNetCore.Identity.UI.Services;
using Project_1.Hubs; // ⬅️ New: Add the namespace for your BidHub

var builder = WebApplication.CreateBuilder(args);

// ----------------- Database -----------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// ----------------- Identity -----------------
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

// ----------------- SignalR Service -----------------
builder.Services.AddSignalR(); // ⬅️ New: Add SignalR services to the container

// ----------------- Application Services -----------------
builder.Services.AddScoped<IListingsService, ListingsService>();
builder.Services.AddScoped<IBidsService, BidsService>();
// 🛑 FIX: Changed the second type argument from ICommentsService to the concrete CommentsService class
builder.Services.AddScoped<ICommentsService, CommentsService>();

// ----------------- Fake Email Sender (no SMTP needed) -----------------
builder.Services.AddSingleton<IEmailSender, NoEmailSender>();

// ----------------- MVC / Razor -----------------
builder.Services.AddControllersWithViews();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var app = builder.Build();

// ----------------- Middleware -----------------
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ----------------- SignalR Hub Mapping -----------------
app.MapHub<BidHub>("/bidHub"); // ⬅️ New: Map the Hub before controllers/pages

app.UseAuthentication();
app.UseAuthorization();


// ----------------- Routes -----------------
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// ----------------- Run -----------------
app.Run();


// ----------------- Fake Email Sender Class -----------------
public class NoEmailSender : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        // 🚫 Do nothing (skip sending emails in development)
        return Task.CompletedTask;
    }
}

// ----------------- Hub Class Stub -----------------
// NOTE: You should move this to a separate file (e.g., Hubs/BidHub.cs)
// but defining it here temporarily ensures the code compiles for testing.
// You must define the actual BidHub logic in your project.
namespace Project_1.Hubs
{
    using Microsoft.AspNetCore.SignalR;
    using System.Threading.Tasks;

    public class BidHub : Hub
    {
        public async Task UpdateHighestBid(int listingId, string newHighestBidAmount)
        {
            await Clients.Group($"listing-{listingId}").SendAsync("ReceiveNewHighestBid", newHighestBidAmount);
        }

        public async Task JoinListingGroup(int listingId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"listing-{listingId}");
        }
    }
}