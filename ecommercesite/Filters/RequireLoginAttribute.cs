using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ecommercesite.Filters
{
    /// <summary>
    /// Action filter that redirects unauthenticated users to the login page.
    /// Authentication is session-based: a user is considered logged in when
    /// <c>HttpContext.Session.GetString("UserName")</c> returns a non-empty
    /// value (set by <c>AccountController.Index</c> on a successful login).
    /// </summary>
    /// <remarks>
    /// The original request path + query string is forwarded to the login
    /// page as <c>returnUrl</c>, so <c>AccountController</c> can send the
    /// user straight back to the page they were trying to reach after a
    /// successful login.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method,
                    AllowMultiple = false, Inherited = true)]
    public class RequireLoginAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Session is always non-null once app.UseSession() has run.
            var userName = context.HttpContext.Session.GetString("UserName");

            if (string.IsNullOrEmpty(userName))
            {
                // Build the URL the user was trying to reach so we can
                // return them there after a successful login.
                var request = context.HttpContext.Request;
                var path = request.Path.Value ?? "/";
                var query = request.QueryString.Value ?? string.Empty;
                var returnUrl = path + query;

                context.Result = new RedirectToActionResult(
                    actionName: "Index",
                    controllerName: "Account",
                    routeValues: new { returnUrl });
            }
        }
    }
}