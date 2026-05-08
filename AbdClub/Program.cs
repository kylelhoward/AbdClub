using AbdClub.Data;
using AbdClub.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Database ---
builder.Services.AddDbContext<AbdContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- Authorization policies ---
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OfficerOnly", policy =>
        policy.RequireClaim("IsOfficer", "true"));
});

// --- Razor Pages ---
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Members");
    options.Conventions.AuthorizeFolder("/Officers", "OfficerOnly");
});

// --- Google Authentication ---
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "Google";
})
.AddCookie("Cookies", options =>
{
    options.LoginPath = "/Auth/Login";
    options.AccessDeniedPath = "/Auth/AccessDenied";
})
.AddGoogle("Google", options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;

    options.Events.OnCreatingTicket = async context =>
    {
        var db = context.HttpContext.RequestServices
            .GetRequiredService<AbdContext>();

        var email = context.Principal?
            .FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var googleSub = context.Principal?
            .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (email == null) return;

        var member = await db.Members
            .FirstOrDefaultAsync(m => m.Email == email);

        if (member == null)
        {
            // Email not in our Members table — deny access
            context.Fail("Not a registered member");
            return;
        }

        // Store GoogleSubId the first time they log in
        if (member.GoogleSubId == null && googleSub != null)
        {
            member.GoogleSubId = googleSub;
            await db.SaveChangesAsync();
        }

        // Add our custom claims to their login session
        var identity = (System.Security.Claims.ClaimsIdentity)context.Principal!.Identity!;
        identity.AddClaim(new System.Security.Claims.Claim("MemberId", member.Id.ToString()));
        identity.AddClaim(new System.Security.Claims.Claim("IsOfficer", member.IsOfficer.ToString().ToLower()));
        identity.AddClaim(new System.Security.Claims.Claim("ExpiryDate", member.ExpiryDate.ToString("O")));

        if (member.OfficerRole != null)
            identity.AddClaim(new System.Security.Claims.Claim("OfficerRole", member.OfficerRole));
    };
});

// --- App Services ---
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHostedService<ReminderService>();

var app = builder.Build();

// --- Middleware pipeline ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();  // who are you?
app.UseAuthorization();   // what can you do?
app.MapRazorPages();

// Auto-run migrations on startup (handy for a small app)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AbdContext>();
    db.Database.Migrate();
}

app.Run();