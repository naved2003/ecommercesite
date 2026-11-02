using ecommercesite.Models;
using ecommercesite.Services;
using ecommercesite.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ecommercesite.Controllers
{
    public class AccountController(
        ILoginService loginService,
        IOrderService orderService,
        IRegisterService registerService,
        ILogger<AccountController> logger) : Controller
    {
        private readonly ILoginService _loginService = loginService;
        private readonly IOrderService _orderService = orderService;
        private readonly IRegisterService _registerService = registerService;
        private readonly ILogger<AccountController> _logger = logger;

        // GET: /Account
        // GET: /Account/Index?returnUrl=/Order/Create
        [HttpGet]
        public IActionResult Index(string? returnUrl)
        {
            // If already logged in, go straight to the destination
            // (returnUrl if safe, otherwise the dashboard).
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UserName")))
            {
                return SafeRedirect(returnUrl);
            }

            // Stash the returnUrl so the login form can echo it back in a
            // hidden field and the POST action knows where to send the user
            // after a successful login.
            ViewBag.ReturnUrl = SanitizeReturnUrl(returnUrl);

            // Build the list of external providers whose credentials are
            // configured (not placeholders). The login view renders one
            // button per enabled provider.
            ViewBag.ExternalProviders = GetEnabledProviders();

            return View(new LoginViewModel());
        }

        // -------------------------------------------------------------------
        // External login (Google / Microsoft / Facebook / Twitter)
        // -------------------------------------------------------------------

        // GET: /Account/ExternalLogin?provider=Google&returnUrl=...
        // Issues a Challenge to the named external OAuth provider. The
        // provider redirects the user to its consent screen; after consent
        // it calls back to ExternalLoginCallback.
        [HttpGet]
        public IActionResult ExternalLogin(string provider, string? returnUrl)
        {
            // Whitelist the provider name so an attacker can't trick the
            // app into challenging an arbitrary scheme.
            var enabled = GetEnabledProviders();
            if (!enabled.Any(p => p.Scheme.Equals(provider, StringComparison.OrdinalIgnoreCase)))
            {
                TempData["Error"] = $"Unknown or disabled login provider '{provider}'.";
                return RedirectToAction(nameof(Index));
            }

            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account",
                new { returnUrl })!;
            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUrl,
                Items = { { "returnUrl", returnUrl ?? "/" } }
            };
            return Challenge(properties, provider);
        }

        // GET: /Account/ExternalLoginCallback?returnUrl=...
        // Called by ANY external provider after the user consents. We read
        // the email claim, find-or-create a Register account, then sign the
        // user into our session (same session keys the rest of the app
        // expects).
        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl)
        {
            // AuthenticateAsync returns the identity the external provider
            // sent back. If null, the user cancelled or something went wrong.
            var authenticateResult = await HttpContext.AuthenticateAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            if (!authenticateResult.Succeeded || authenticateResult.Principal is null)
            {
                TempData["Error"] = "External authentication failed or was cancelled.";
                return RedirectToAction(nameof(Index));
            }

            var email = authenticateResult.Principal.FindFirstValue(ClaimTypes.Email)
                        // Microsoft sometimes returns the email in preferred_username instead.
                        ?? authenticateResult.Principal.FindFirstValue("preferred_username");
            var name = authenticateResult.Principal.FindFirstValue(ClaimTypes.Name)
                       ?? authenticateResult.Principal.FindFirstValue("name");

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "The login provider did not return an email address. Please grant email permission.";
                return RedirectToAction(nameof(Index));
            }

            email = email.Trim().ToLowerInvariant();

            // Find-or-create a Register account for this email.
            var user = await _registerService.GetRegisterByEmailAsync(email);
            if (user is null)
            {
                // Auto-create a Customer account. Password is a random
                // 40-char string — the user can never log in with it
                // directly, only via the external provider.
                var randomPassword = string.Concat(Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N").AsSpan(0, 8));
                var firstName = "User";
                var lastName = "External";
                if (!string.IsNullOrWhiteSpace(name))
                {
                    var parts = name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    firstName = parts[0];
                    lastName = parts.Length > 1 ? parts[1] : "User";
                }

                var (success, error) = await _registerService.AddAccountAsync(
                    firstName, lastName, email, randomPassword,
                    "000-000-0000", "Customer");

                if (!success)
                {
                    _logger.LogError("Failed to auto-create external account for {Email}: {Error}", email, error);
                    TempData["Error"] = error ?? "Failed to create account from external login.";
                    return RedirectToAction(nameof(Index));
                }

                user = await _registerService.GetRegisterByEmailAsync(email);
                if (user is null)
                {
                    TempData["Error"] = "Account creation succeeded but the account could not be found.";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("Auto-created Customer account {Email} from external login.", email);
            }

            if (!user.IsActive)
            {
                TempData["Error"] = "This account has been deactivated. Contact an administrator.";
                return RedirectToAction(nameof(Index));
            }

            // Sign in: set the session keys the rest of the app expects
            // (RequireLoginAttribute checks "UserName") + issue the auth
            // cookie so HttpContext.User is populated for any future
            // policy-based checks.
            await SignInUserAsync(user);

            _logger.LogInformation("User {Email} logged in via external provider.", user.Email);

            return SafeRedirect(returnUrl);
        }

        /// <summary>
        /// Sets the session keys + issues the auth cookie. Shared by the
        /// password login flow and the external login flow.
        /// </summary>
        private async Task SignInUserAsync(Register user)
        {
            HttpContext.Session.SetString("UserName", user.Email);
            HttpContext.Session.SetString("UserFullName", user.FullName);
            HttpContext.Session.SetString("UserId", user.UserId);
            HttpContext.Session.SetString("UserRole", user.Role ?? "Customer");

            // Issue the auth cookie with the user's claims so
            // HttpContext.User.Identity.IsAuthenticated is true for any
            // future middleware / policy checks.
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId),
                new(ClaimTypes.Name, user.Email),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role ?? "Customer"),
                new("FullName", user.FullName)
            };
            var identity = new ClaimsIdentity(claims,
                CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });
        }

        // POST: /Account/Index
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginViewModel model, string? returnUrl)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = SanitizeReturnUrl(returnUrl);
                ViewBag.ExternalProviders = GetEnabledProviders();
                return View(model);
            }

            var user = await _loginService.LoginAsync(model.Email, model.Password);
            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                ViewBag.ReturnUrl = SanitizeReturnUrl(returnUrl);
                ViewBag.ExternalProviders = GetEnabledProviders();
                return View(model);
            }

            await SignInUserAsync(user);

            _logger.LogInformation("User {Email} ({Role}) logged in.", user.Email, user.Role);

            // Send the user back to the page they originally requested
            // (e.g. /Order/Create) — fall back to the dashboard if there
            // is no safe returnUrl.
            return SafeRedirect(returnUrl);
        }

        // GET: /Account/Dashboard (admin)
        [HttpGet]
        public IActionResult Dashboard()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToAction(nameof(Index));
            }

            ViewBag.UserName = userName;
            ViewBag.UserFullName = HttpContext.Session.GetString("UserFullName") ?? userName;
            return View();
        }

        // GET: /Account/CustomerDashboard (customer landing page)
        [HttpGet]
        public async Task<IActionResult> CustomerDashboard()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToAction(nameof(Index));
            }

            var userId = HttpContext.Session.GetString("UserId");
            var recentOrders = await _orderService.GetRecentOrdersForUserAsync(userId ?? "", count: 5);

            // KPIs
            var allOrders = await _orderService.GetOrdersByCustomerAsync(userId ?? "");
            ViewBag.UserName = userName;
            ViewBag.UserFullName = HttpContext.Session.GetString("UserFullName") ?? userName;
            ViewBag.TotalOrders = allOrders.Count;
            ViewBag.TotalSpent = allOrders.Sum(o => o.TotalAmount);
            ViewBag.PendingOrders = allOrders.Count(o => o.Status == "Pending" || o.Status == "Confirmed" || o.Status == "Shipped");
            ViewBag.DeliveredOrders = allOrders.Count(o => o.Status == "Delivered");
            ViewBag.RecentOrders = recentOrders;

            return View();
        }

        // GET: /Account/Logout
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            var userName = HttpContext.Session.GetString("UserName");
            _logger.LogInformation("User {Email} logged out.", userName);

            // Sign out of the cookie auth scheme (clears the .ecommercesite.auth
            // cookie) AND clear the session (clears UserName / UserRole etc.).
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            TempData["Success"] = "You have been logged out.";
            return RedirectToAction(nameof(Index));
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------

        /// <summary>
        /// Returns the list of external login providers whose credentials
        /// are configured (not placeholders). Includes the four first-class
        /// providers (Google, Microsoft, Facebook, Twitter) when configured,
        /// PLUS any generic OAuth2 providers from the Authentication:Providers
        /// array. The login view renders one button per enabled provider.
        /// </summary>
        private List<ExternalProvider> GetEnabledProviders()
        {
            var config = HttpContext.RequestServices.GetService<IConfiguration>();
            var providers = new List<ExternalProvider>();

            static bool IsConfigured(string? value, string placeholderPrefix) =>
                !string.IsNullOrWhiteSpace(value) && !value.StartsWith(placeholderPrefix);

            if (IsConfigured(config?["Authentication:Google:ClientId"], "YOUR_GOOGLE_"))
                providers.Add(new ExternalProvider("Google", "Google", null));

            if (IsConfigured(config?["Authentication:Microsoft:ClientId"], "YOUR_MICROSOFT_"))
                providers.Add(new ExternalProvider("Microsoft", "Microsoft", null));

            if (IsConfigured(config?["Authentication:Facebook:AppId"], "YOUR_FACEBOOK_"))
                providers.Add(new ExternalProvider("Facebook", "Facebook", null));

            if (IsConfigured(config?["Authentication:Twitter:ConsumerKey"], "YOUR_TWITTER_"))
                providers.Add(new ExternalProvider("Twitter", "Twitter", null));

            // Generic OAuth2 providers from the Providers array.
            var generic = config?.GetSection("Authentication:Providers")
                .Get<List<OAuthProviderConfig>>() ?? [];
            foreach (var p in generic)
            {
                if (!string.IsNullOrWhiteSpace(p.ClientId) && !p.ClientId.StartsWith("YOUR_"))
                {
                    providers.Add(new ExternalProvider(
                        p.Name,
                        string.IsNullOrWhiteSpace(p.DisplayName) ? p.Name : p.DisplayName,
                        p.Color));
                }
            }

            return providers;
        }

        /// <summary>
        /// Lightweight DTO describing one enabled external login provider
        /// for the login view.
        /// </summary>
        public sealed class ExternalProvider(string scheme, string displayName, string? color)
        {
            public string Scheme { get; } = scheme;
            public string DisplayName { get; } = displayName;
            public string? Color { get; } = color;
        }

        /// <summary>
        /// Only allow return URLs that are local (start with "/" and are not
        /// protocol-relative "//..."). This is the standard anti-open-redirect
        /// guard — without it an attacker could craft a link like
        /// /Account/Index?returnUrl=https://evil.com and phish users after
        /// they log in.
        /// </summary>
        private static string? SanitizeReturnUrl(string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                return null;
            }
            if (!returnUrl.StartsWith('/') || returnUrl.StartsWith("//"))
            {
                return null;
            }
            return returnUrl;
        }

        private IActionResult SafeRedirect(string? returnUrl)
        {
            var safe = SanitizeReturnUrl(returnUrl);
            if (safe is not null)
            {
                return Redirect(safe);
            }

            // No return URL — send the user to a sensible role-aware landing
            // page. Admins go to the Dashboard; customers go to their own
            // CustomerDashboard.
            var role = HttpContext.Session.GetString("UserRole") ?? "Customer";
            return role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                ? RedirectToAction(nameof(Dashboard))
                : RedirectToAction(nameof(CustomerDashboard));
        }
    }
}