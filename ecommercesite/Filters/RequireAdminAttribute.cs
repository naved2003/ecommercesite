using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ecommercesite.Filters
{
    /// <summary>
    /// Action filter that restricts access to Admin users only. Reads the
    /// role from <c>HttpContext.Session.GetString("UserRole")</c> (set by
    /// <c>AccountController</c> on login). Non-admin users (customers) are
    /// redirected to a non-admin landing page with an error toast.
    /// </summary>
    /// <remarks>
    /// <para>This attribute assumes <see cref="RequireLoginAttribute"/> has
    /// already run (or the user is otherwise authenticated) — it only checks
    /// the role, not whether the user is logged in. Apply
    /// <c>[RequireLogin]</c> first, then <c>[RequireAdmin]</c> on controllers
    /// that need both.</para>
    /// <para><b>Important:</b> the redirect target for logged-in non-admins
    /// MUST NOT itself be decorated with <c>[RequireAdmin]</c>, otherwise the
    /// filter will redirect again → infinite redirect loop →
    /// "The page isn't redirecting properly" in the browser. We send
    /// customers to <c>Order/Index</c> (their "My Orders" page) which is
    /// customer-accessible.</para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method,
                    AllowMultiple = false, Inherited = true)]
    public class RequireAdminAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var role = context.HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(role) || !role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                // Flash an error toast so the user understands why they were
                // bounced off the admin page.
                context.HttpContext.Session.SetString("TempError", "Admin access required.");

                var isLoggedIn = !string.IsNullOrEmpty(context.HttpContext.Session.GetString("UserName"));
                if (isLoggedIn)
                {
                    // Send customers to their "My Orders" page — NOT to
                    // /Home/Index (which is itself [RequireAdmin] and would
                    // cause an infinite redirect loop).
                    context.Result = new RedirectToActionResult(
                        actionName: "Index",
                        controllerName: "Order",
                        routeValues: null);
                }
                else
                {
                    // Not logged in at all — send to login.
                    context.Result = new RedirectToActionResult(
                        actionName: "Index",
                        controllerName: "Account",
                        routeValues: null);
                }
            }
        }
    }
}