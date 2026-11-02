using ecommercesite.Services;
using ecommercesite.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ecommercesite.Controllers
{
    /// <summary>
    /// User registration controller. The class name keeps the original
    /// <c>Register</c> name so existing routes and DI bindings
    /// continue to work, but the implementation has been fully rewritten.
    /// </summary>
    public class RegisterController(
        IRegisterService userService,
        ILogger<RegisterController> logger) : Controller
    {
        private readonly IRegisterService _userService = userService;
        private readonly ILogger<RegisterController> _logger = logger;

        // GET: /Register
        [HttpGet]
        public IActionResult Index()
        {
            return View(new RegisterViewModel());
        }

        // GET: /Register/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new RegisterViewModel());
        }

        // POST: /Register/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (success, error) = await _userService.AddRegisterAsync(model);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Registration failed.");
                return View(model);
            }

            _logger.LogInformation("User registered: {Email}", model.Email);
            TempData["Success"] = "Account created successfully. Please log in.";
            return RedirectToAction("Index", "Account");
        }
    }
}