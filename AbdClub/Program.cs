using AbdClub.Data;
using AbdClub.Services;
using AbdClub.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics; // 🌟 Add this for warning definitions
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
    // Open to all registered accounts who passed the email whitelist gate
    options.Conventions.AuthorizeFolder("/Members");

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

        var member = await db.Members
            .FirstOrDefaultAsync(m => m.Email == email);

        if (member == null)
        {
            // 🌟 SECURITY AUDIT LOG: Explicitly flags unauthorized external login attempts
            logger.LogWarning(
                "Security Gateway Alert: Sign-in handshake aborted. Google account {AttemptedEmail} is not present on the club member whitelist.",
                email);

            context.Fail("Not a registered member");
            return;
        }
        // 🌟 HARD BLOCK FOR SUSPENDED ACCOUNTS
        if (member.IsSuspended)
        {
            logger.LogWarning(
               "Access Denied This account has been administratively suspended by a club officer.",
               email);
            context.Fail("Access Denied: This account has been administratively suspended by a club officer.");
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
        if (member.GoogleSubId == null && googleSub != null)
        {
            member.GoogleSubId = googleSub;
            await db.SaveChangesAsync();

            // 🌟 COMPLIANCE AUDIT LOG: Tracks when an identity bridge anchor link is generated
            logger.LogInformation(
                "Identity Mapping Upgrade: Linked MemberId: {MemberId} ({CustomerEmail}) to Google Subject Identifiers: {GoogleSubId}",
                member.Id, email, googleSub);
        }

        logger.LogInformation(
            "Security Checkpoint Cleared: Active session cookies initialized for MemberId: {MemberId} ({CustomerEmail}).",
            member.Id, email);

        // Get the COOKIE identity, not the Google identity
        // This is the key fix — we must add claims to the principal that will be serialized into the cookie
        var claimsToAdd = new List<System.Security.Claims.Claim>
        {
            new("MemberId",    member.Id.ToString()),
            new("IsOfficer",   member.IsOfficer.ToString().ToLower()),
            new("IsAdmin",   member.IsAdmin.ToString().ToLower()),
            new("IsTechAdmin",   member.IsTechAdmin.ToString().ToLower()),
            new("ExpiryDate",  member.ExpiryDate.HasValue
                               ? member.ExpiryDate.Value.ToString("O")
                               : ""),
             // Standardized Member role token added to every registered login principal
            new(System.Security.Claims.ClaimTypes.Role, "Member")
        };

        if (member.OfficerRole != null)
            claimsToAdd.Add(new("OfficerRole", member.OfficerRole));

        if (member.IsOfficer)
            claimsToAdd.Add(new(System.Security.Claims.ClaimTypes.Role, "Officer"));
        if (member.IsAdmin)
            claimsToAdd.Add(new(System.Security.Claims.ClaimTypes.Role, "Admin"));
        if (member.IsTechAdmin)
            claimsToAdd.Add(new(System.Security.Claims.ClaimTypes.Role, "TechAdmin"));

        // Add to BOTH identities to be safe
        foreach (var identity in context.Principal!.Identities)
        {
            identity.AddClaims(claimsToAdd);
        }
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

    // 🌟 REFACTORED DEV TESTING API ROUTE: Fully configured for dynamic configs and structured logs
    app.MapGet("/Dev/test-email", async (
        IEmailService emailService,
        AbdContext db,
        IConfiguration config, // Injected configuration manager
        ILogger<Program> logger) => // Injected Serilog provider engine
    {
        // 1. Fetch the targeted testing recipient email directly out of configurations
        var testTargetEmail = config["Email:AdminEmail"] ?? "kylelhoward@gmail.com"; // Clear fallback backup

        logger.LogInformation(
            "Sandbox Diagnostics: Initiating administrative mock email execution test. TargetRecipient: {TestEmail}",
            testTargetEmail);

        try
        {
            // 2. Query your whitelist table context safely using lowercase normalization rules
            var cleanEmail = testTargetEmail.Trim().ToLower();
            var member = await db.Members
                .FirstOrDefaultAsync(m => m.Email != null && m.Email.ToLower() == cleanEmail);

            if (member == null)
            {
                logger.LogWarning(
                    "Sandbox Diagnostics Failure: Email test aborted. The target configuration address '{TestEmail}' does not exist on your club database roster.",
                    testTargetEmail);
                return $"Error: No member found in database table spaces matching configuration key string: '{testTargetEmail}'";
            }

            // 3. Dispatch the message template envelope pipeline
            await emailService.SendMembershipReminderAsync(member);

            // 4. Log a clean, successful transaction footprint using structured JSON brackets
            logger.LogInformation(
                "Sandbox Diagnostics Success: Automated test notification successfully transferred to outbound SMTP relay thread queues for MemberId: {MemberId} ({TestEmail})",
                member.Id, testTargetEmail);

            return $"Test email successfully dispatched to active roster member account: {member.Email} via configuration mapping keys.";
        }
        catch (Exception ex)
        {
            // 5. CRITICAL TRACE: Catches and logs raw Zoho authentication, connection, or port 587 timeouts
            logger.LogError(ex,
                "Sandbox Diagnostics Exception: A critical network failure occurred while attempting an outbound email test to {TestEmail}.",
                testTargetEmail);

            return $"System Exception Intercepted: {ex.Message}. Check your System Operational Ledger (/Admin/AuditLogs) for the complete stack trace error block.";
        }
    });


    app.MapGet("/Dev/debug-claims", (HttpContext ctx) =>
    {
        var claims = ctx.User.Claims
            .Select(c => new { c.Type, c.Value })
            .ToList();
        return Results.Json(claims);
    }).RequireAuthorization();
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