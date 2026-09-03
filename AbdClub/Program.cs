using AbdClub.Data;
using AbdClub.Services;
using AbdClub.Services.Interfaces;
using AbdClub.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics; // 🌟 Add this for warning definitions
using System.Threading.RateLimiting;
using Resend;
using Serilog;
using Log = Serilog.Log;

var builder = WebApplication.CreateBuilder(args);
// 🌟 1. ENABLE DIAGNOSTICS FIRST: Catches configuration errors on initialization
// 🌟 1. OPTIMIZED DIAGNOSTICS: Catches internal connection drops immediately
Serilog.Debugging.SelfLog.Enable(msg => System.Diagnostics.Debug.WriteLine($"SERILOG DIAGNOSTIC: {msg}"));

// 🌟 2. GENERIC BOOTSTRAPPER: Reads your entire logging setup cleanly from JSON
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();




builder.Host.UseSerilog(); // Instructs ASP.NET Core to route all logging through Serilog

// --- Database ---
//builder.Services.AddDbContext<AbdContext>(options =>
//    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddDbContext<AbdContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));

    // 🌟 TEMPORARY LOOP BREAK: Force EF9 to ignore the pending model blocks during migration commands
    options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
});



// --- Authorization Policies ---
builder.Services.AddAuthorizationBuilder()
                                     // --- Authorization Policies ---
                                     .AddPolicy("isTechAdmin", policy =>
        policy.RequireRole("TechAdmin"))
                                     // --- Authorization Policies ---
                                     .AddPolicy("isAdmin", policy =>
        policy.RequireRole("TechAdmin", "Admin"))
                                     // --- Authorization Policies ---
                                     .AddPolicy("isOfficer", policy =>
        policy.RequireRole("TechAdmin", "Admin", "Officer"));

// --- Razor Pages Folder System Conventions ---
builder.Services.AddRazorPages(options =>
{
    // Member self-service login has been retired. Any remaining member pages are
    // officer-only until they are removed or repurposed.
    options.Conventions.AuthorizeFolder("/Members", "isOfficer");

    // Tier 1: Visible to Officers, Admins, and TechAdmins
    options.Conventions.AuthorizeFolder("/Officers", "isOfficer");

    // Tier 2: Visible to Admins and TechAdmins (Blocks standard Officers)
    options.Conventions.AuthorizeFolder("/Admin", "isAdmin");

    // Tier 3: Restricted strictly to TechAdmins (Blocks standard Officers and Admins)
    options.Conventions.AuthorizeFolder("/Dev", "isTechAdmin");
    // 🌟 OVERRIDE SPECIFIC PAGES TO THE HIGHER PRIVILEGE ADMIN POLICY:
    options.Conventions.AuthorizePage("/Officers/Dances/Create", "isAdmin");
    options.Conventions.AuthorizePage("/Officers/Dances/Edit", "isOfficer");
    options.Conventions.AuthorizePage("/Officers/Dances/Delete", "isAdmin");
    options.Conventions.AuthorizePage("/Officers/Members/Create", "isAdmin");
    options.Conventions.AuthorizePage("/Officers/Members/Edit", "isAdmin");
    options.Conventions.AuthorizePage("/Officers/Meetings/Create", "isAdmin");
    options.Conventions.AuthorizePage("/Officers/Meetings/Edit", "isAdmin");
    options.Conventions.AuthorizePage("/Officers/Meetings/Delete", "isAdmin");
});

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("membership-status", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Register Google credential path provider for centralized resolution
builder.Services.AddSingleton<IGoogleCredentialPathProvider, GoogleCredentialPathProvider>();

// --- Google Authentication ---
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
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

        // Resolve the core Serilog logging service for this security checkpoint pipeline
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<Program>>();

        var email = context.Principal?
            .FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var googleSub = context.Principal?
            .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (email == null)
        {
            _ = logger; // keeps compiler warning checks clear if variable sits unreferenced
            logger.LogWarning("Security Intercept: Google identity ticket creation aborted due to missing email claim parameter attributes.");
            context.Fail("Missing credential profiles.");
            return;
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var officer = await db.OfficerAccounts
            .Include(a => a.Member)
            .FirstOrDefaultAsync(a => a.Email == normalizedEmail);

        if (officer == null || !officer.IsEnabled)
        {
            // 🌟 SECURITY AUDIT LOG: Explicitly flags unauthorized external login attempts
            logger.LogWarning(
                "Security Gateway Alert: Google account {AttemptedEmail} is not an enabled officer account.",
                email);

            context.Fail("Not an authorized officer");
            return;
        }

        // 🌟 HARD BLOCK FOR EXPIRED MEMBERSHIPS
        //if (!member.ExpiryDate.HasValue || member.ExpiryDate.Value.Date < DateTime.UtcNow.Date)
        //{
        //     logger.LogWarning(
        //      "Access Denied: Your annual club membership subscription has lapsed or expired.",
        //       email);
        //    context.Fail("Access Denied: Your annual club membership subscription has lapsed or expired.");
        //    return;
        //}

        // Store GoogleSubId the first time they log in
        if (officer.GoogleSubId == null && googleSub != null)
        {
            officer.GoogleSubId = googleSub;
            await db.SaveChangesAsync();

            // 🌟 COMPLIANCE AUDIT LOG: Tracks when an identity bridge anchor link is generated
            logger.LogInformation(
                "Linked OfficerAccountId {OfficerAccountId} ({Email}) to Google subject {GoogleSubId}",
                officer.Id, email, googleSub);
        }

        logger.LogInformation(
            "Security Checkpoint Cleared for OfficerAccountId {OfficerAccountId} ({Email}).",
            officer.Id, email);

        // Get the COOKIE identity, not the Google identity
        // This is the key fix — we must add claims to the principal that will be serialized into the cookie
        var claimsToAdd = OfficerClaimsFactory.Create(officer);

        // Add to BOTH identities to be safe
        foreach (var identity in context.Principal!.Identities)
        {
            identity.AddClaims(claimsToAdd);
        }
    };
});


#region --- App Services ---

#region EmailService
// Choose one email service:Sandbox(fake email), Resend or SMTP (SMTP2GO, SendGrid, etc.)
var emailProvider = builder.Configuration["Email:Provider"] ?? "Smtp";

if (emailProvider.Equals("Sandbox", StringComparison.OrdinalIgnoreCase))
{
    // Uses full SmtpEmailService logic & templates, but sends via FakeSmtpSender
    builder.Services.AddScoped<ISmtpSender, FakeSmtpSender>();
    builder.Services.AddScoped<IEmailService, SmtpEmailService>();
}
else if (emailProvider.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
{
    // Real SMTP delivery
    builder.Services.AddScoped<ISmtpSender, RealSmtpSender>();
    builder.Services.AddScoped<IEmailService, SmtpEmailService>();
}
else
{
    // Resend API delivery
    var resendApiKey = builder.Configuration["Email:ResendApiKey"]!;
    builder.Services.Configure<ResendClientOptions>(options => options.ApiToken = resendApiKey);
    builder.Services.AddHttpClient<ResendClient>();
    builder.Services.AddScoped<IEmailService, ResendEmailService>();
}
#endregion EmailService


builder.Services.AddScoped<IStripeService, StripeService>();  // ← add this
builder.Services.AddScoped<IMagicLinkService, MagicLinkService>();

// --- Session ---
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<DanceService>();
builder.Services.AddHostedService<ReminderService>();
builder.Services.AddScoped<IGoogleSheetExportService, GoogleSheetExportService>();
builder.Services.AddSingleton<BuildInfoService>();
#endregion --- App Services ---


try
{
    Log.Information("Starting up Austin Ballroom Dancers web application platform...");
    var app = builder.Build();
    // 🌟 REFACTORED REQUEST LOGGING: Intercepts and filters out infrastructure polling noise
    app.UseSerilogRequestLogging(options =>
    {
        // Custom filter evaluator logic
        options.GetLevel = (httpContext, elapsedMs, authException) =>
        {
            var requestPath = httpContext.Request.Path.Value ?? string.Empty;

            // 1. SILENCE REPETITIVE STATUS TASKS: Ignore explicit health monitoring routes
            if (requestPath.Equals("/health", StringComparison.OrdinalIgnoreCase) ||
                requestPath.StartsWith("/healthz", StringComparison.OrdinalIgnoreCase))
            {
                return Serilog.Events.LogEventLevel.Verbose; // Drop floor below Information level
            }

            // 2. SILENCE STATIC DESIGN ASSETS (Optional): Keep file logs out of your ledger table
            if (requestPath.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
                requestPath.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
                requestPath.StartsWith("/lib", StringComparison.OrdinalIgnoreCase))
            {
                return Serilog.Events.LogEventLevel.Verbose;
            }

            // Return standard logging tracking values if an explicit exception occurred or regular page hit
            return authException != null ? Serilog.Events.LogEventLevel.Error : Serilog.Events.LogEventLevel.Information;
        };
    });

    // --- Middleware pipeline ---
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseRateLimiter();
    app.UseSession();        // ← add this
    app.UseAuthentication();  // who are you?
    app.UseAuthorization();   // what can you do?
    app.MapRazorPages();

    //// Auto-run migrations on startup (handy for a small app)
    //using (var scope = app.Services.CreateScope())
    //{
    //    var db = scope.ServiceProvider.GetRequiredService<AbdContext>();
    //    db.Database.Migrate();
    //}

    app.MapGet("/Dev/test-email", async (
        IEmailService emailService,
        IConfiguration config,
        ILogger<Program> logger) =>
    {
        var testTargetEmail = config["Email:AdminEmail"];

        if (string.IsNullOrWhiteSpace(testTargetEmail))
            return Results.Problem("Email:AdminEmail is not configured.");

        logger.LogInformation(
            "UAT email diagnostic started for {TestEmail}.",
            testTargetEmail);

        try
        {
            await emailService.SendMembershipStatusAsync(
                testTargetEmail,
                Array.Empty<Member>());

            logger.LogInformation(
                "UAT email diagnostic completed for {TestEmail}.",
                testTargetEmail);

            return Results.Ok(
                $"Test email dispatched to the configured UAT address: {testTargetEmail}");
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "UAT email diagnostic failed for {TestEmail}.",
                testTargetEmail);

            return Results.Problem(
                "The UAT email test failed. Check the server log for details.");
        }
    })
    .RequireAuthorization("isTechAdmin");

    app.MapGet("/Dev/debug-claims", (HttpContext ctx) =>
    {
        var claims = ctx.User.Claims
            .Select(c => new { c.Type, c.Value })
            .ToList();
        return Results.Json(claims);
    }).RequireAuthorization("isTechAdmin");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "The application host terminated unexpectedly during bootstrap compilation thread execution.");
}
finally
{
    Log.CloseAndFlush(); // Clears memory buffers and writes remaining entries to disk/database
}
