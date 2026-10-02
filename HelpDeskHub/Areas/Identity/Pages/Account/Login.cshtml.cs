using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HelpDeskHub.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, ILogger<LoginModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

    [BindProperty]
    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        // prefer bound ReturnUrl but accept parameter
        ReturnUrl = ReturnUrl ?? returnUrl;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Find user by email to ensure we check the correct account
        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user == null)
        {
            _logger?.LogWarning("Login failed: no user found with email {Email}", Input.Email);
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return Page();
        }

        var check = await _signInManager.CheckPasswordSignInAsync(user, Input.Password, lockoutOnFailure: false);
        if (!check.Succeeded)
        {
            if (check.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "User account locked out.");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            _logger?.LogWarning("Invalid password for user {UserId}", user.Id);
            return Page();
        }

        // Sign in the user
        await _signInManager.SignInAsync(user, Input.RememberMe);
        _logger?.LogInformation("=== LOGIN SUCCESS ===");
        _logger?.LogInformation("User {UserId} logged in.", user.Id);
        _logger?.LogInformation("ReturnUrl value: '{ReturnUrl}'", ReturnUrl ?? "(null)");

        // Log roles for diagnostics
        var roles = await _userManager.GetRolesAsync(user);
        _logger?.LogInformation("User roles: {Roles}", string.Join(',', roles));

        // If ReturnUrl provided and local, redirect there
        if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            _logger?.LogInformation("BRANCH: ReturnUrl redirect to {ReturnUrl}", ReturnUrl);
            return LocalRedirect(ReturnUrl);
        }

        // Role-based redirect
        if (await _userManager.IsInRoleAsync(user, "Administrator"))
        {
            _logger?.LogInformation("BRANCH: Administrator redirect");
            return RedirectToAction("Index", "Admin");
        }

        if (await _userManager.IsInRoleAsync(user, "SupportAgent"))
        {
            _logger?.LogInformation("BRANCH: SupportAgent redirect");
            return RedirectToAction("AgentWorklist", "Tickets");
        }

        // Default redirect for any authenticated user - go to tickets index
        _logger?.LogInformation("BRANCH: Employee redirect to /Tickets/Index");
        return Redirect("/Tickets/Index");
    }
}
