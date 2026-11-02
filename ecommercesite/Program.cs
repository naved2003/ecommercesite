using ecommercesite.Data;
using ecommercesite.Repositories;
using ecommercesite.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// MVC + Razor
// ---------------------------------------------------------------------------
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

// ---------------------------------------------------------------------------
// EF Core (SQL Server)
// ---------------------------------------------------------------------------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing."),
        sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

// ---------------------------------------------------------------------------
// Session
// ---------------------------------------------------------------------------
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.Name = ".ecommercesite.session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

// ---------------------------------------------------------------------------
// Services (business layer)
// ---------------------------------------------------------------------------
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IRegisterService, RegisterService>();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<IProductCatService, ProductCatService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IDeliveryPersonService, DeliveryPersonService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

// ---------------------------------------------------------------------------
// Repositories (data access layer)
// ---------------------------------------------------------------------------
builder.Services.AddScoped<IRegisterRepository, RegisterRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ILoginRepository, LoginRepository>();
builder.Services.AddScoped<IProductCatRepository, ProductCatRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IDeliveryPersonRepository, DeliveryPersonRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// ---------------------------------------------------------------------------
// HTTP client factory (handy for future integrations)
// ---------------------------------------------------------------------------
builder.Services.AddHttpClient();

// ---------------------------------------------------------------------------
// Compression + static files
// ---------------------------------------------------------------------------
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
});

// ---------------------------------------------------------------------------
// Authentication — cookie scheme (default) + multiple external providers
// ---------------------------------------------------------------------------
// The app uses a cookie as the default authentication scheme to store the
// user's identity after a successful login (password OR any external
// provider). Each external provider (Google, Microsoft, Facebook, Twitter)
// is only registered when its credentials are configured — so the app runs
// fine with zero, one, or all of them enabled. The login page shows a
// button for every provider whose credentials are real (not placeholders).
var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.Name = ".ecommercesite.auth";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

// ---- Google ---------------------------------------------------------------
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId)
    && !googleClientId.StartsWith("YOUR_GOOGLE_"))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId!;
        options.ClientSecret = googleClientSecret!;
        options.Scope.Add("email");
        options.Scope.Add("profile");
        options.SaveTokens = true;
    });
}

// ---- Microsoft ------------------------------------------------------------
var msClientId = builder.Configuration["Authentication:Microsoft:ClientId"];
var msClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"];
var msTenant = builder.Configuration["Authentication:Microsoft:Tenant"] ?? "common";
if (!string.IsNullOrWhiteSpace(msClientId)
    && !msClientId.StartsWith("YOUR_MICROSOFT_"))
{
    authBuilder.AddMicrosoftAccount(options =>
    {
        options.ClientId = msClientId!;
        options.ClientSecret = msClientSecret!;
        options.AuthorizationEndpoint = $"https://login.microsoftonline.com/{msTenant}/oauth2/v2.0/authorize";
        options.TokenEndpoint = $"https://login.microsoftonline.com/{msTenant}/oauth2/v2.0/token";
        options.SaveTokens = true;
    });
}

// ---- Facebook -------------------------------------------------------------
var fbAppId = builder.Configuration["Authentication:Facebook:AppId"];
var fbAppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
if (!string.IsNullOrWhiteSpace(fbAppId)
    && !fbAppId.StartsWith("YOUR_FACEBOOK_"))
{
    authBuilder.AddFacebook(options =>
    {
        options.AppId = fbAppId!;
        options.AppSecret = fbAppSecret!;
        options.Scope.Add("email");
        options.SaveTokens = true;
    });
}

// ---- Twitter / X ----------------------------------------------------------
var twConsumerKey = builder.Configuration["Authentication:Twitter:ConsumerKey"];
var twConsumerSecret = builder.Configuration["Authentication:Twitter:ConsumerSecret"];
if (!string.IsNullOrWhiteSpace(twConsumerKey)
    && !twConsumerKey.StartsWith("YOUR_TWITTER_"))
{
    authBuilder.AddTwitter(options =>
    {
        options.ConsumerKey = twConsumerKey!;
        options.ConsumerSecret = twConsumerSecret!;
        options.SaveTokens = true;
    });
}

// ---- Generic OAuth2 providers (any provider) -----------------------------
// Reads the Authentication:Providers array and registers each one as a
// generic OAuth2 handler. This lets you plug in ANY OAuth2 provider
// (GitHub, Discord, LinkedIn, Spotify, Apple, GitLab, …) by editing
// appsettings.json — no code changes needed.
var genericProviders = builder.Configuration
    .GetSection("Authentication:Providers")
    .Get<List<ecommercesite.ViewModels.OAuthProviderConfig>>()
    ?? [];

foreach (var p in genericProviders)
{
    // Skip entries whose ClientId is still the placeholder.
    if (string.IsNullOrWhiteSpace(p.ClientId)
        || p.ClientId.StartsWith("YOUR_"))
    {
        continue;
    }

    // Capture the loop variable so the lambda sees the right value.
    var provider = p;
    authBuilder.AddOAuth(provider.Name, provider.DisplayName, options =>
    {
        options.ClientId = provider.ClientId;
        options.ClientSecret = provider.ClientSecret;
        options.AuthorizationEndpoint = provider.AuthorizationEndpoint;
        options.TokenEndpoint = provider.TokenEndpoint;
        options.UserInformationEndpoint = provider.UserInformationEndpoint;
        options.ClaimsIssuer = provider.Name;

        foreach (var scope in provider.Scopes)
        {
            options.Scope.Add(scope);
        }

        // Map common claim fields from the user-info JSON response.
        // Different providers use different field names — we try a few
        // common ones + honor the EmailField config value.
        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "login");
        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "username");
        options.ClaimActions.MapJsonKey(ClaimTypes.Email,
            string.IsNullOrWhiteSpace(provider.EmailField) ? "email" : provider.EmailField);

        options.SaveTokens = true;
    });

    // Log registration so it's visible at startup.
    Console.WriteLine($"[Auth] Registered generic OAuth2 provider: {provider.Name}");
}

var app = builder.Build();

// ---------------------------------------------------------------------------
// Middleware pipeline
// ---------------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Cache static assets for 7 days in production
        if (!app.Environment.IsDevelopment())
        {
            ctx.Context.Response.Headers.CacheControl = "public, max-age=604800";
        }
    }
});

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.Run();