using AbdClub.Data;
using AbdClub.Services;
using Microsoft.EntityFrameworkCore;
using Resend;

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
        identity.AddClaim(new System.Security.Claims.Claim(
            "ExpiryDate",
            member.ExpiryDate.HasValue ? member.ExpiryDate.Value.ToString("O") : ""
        ));

        if (member.OfficerRole != null)
            identity.AddClaim(new System.Security.Claims.Claim("OfficerRole", member.OfficerRole));

        // Also add an actual Role claim so [Authorize(Roles = "Officer")] works
        if (member.IsOfficer)
            identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Officer"));
    };
});

// --- App Services ---
// Choose one email service: Resend or SMTP (SMTP2GO, SendGrid, etc.)
var emailProvider = builder.Configuration["Email:Provider"] ?? "Resend";

if (emailProvider.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IEmailService, SmtpEmailService>();
}
else
{
    // Register Resend client
    var resendApiKey = builder.Configuration["Email:ResendApiKey"]!;
    builder.Services.Configure<ResendClientOptions>(options => options.ApiToken = resendApiKey);
    builder.Services.AddHttpClient<ResendClient>();
    builder.Services.AddScoped<IEmailService, ResendEmailService>();
}

builder.Services.AddScoped<DanceService>();
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

app.MapGet("/test-email", async (IEmailService emailService, AbdContext db) =>
{
    var member = await db.Members
    .SingleOrDefaultAsync(m => m.Email == "kylelhoward@gmail.com");
    if (member == null) return "No members found";

    await emailService.SendMembershipReminderAsync(member);
    return $"Test email sent to {member.Email}";
});
app.Run();