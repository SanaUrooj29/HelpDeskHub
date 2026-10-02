using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace HelpDeskHub.Areas.Identity.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        ILogger<RegisterModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _logger.LogWarning("RegisterModel constructor called");
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(
            100,
            MinimumLength = 6,
            ErrorMessage =
                "Password must be at least {2} characters long.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(
            "Password",
            ErrorMessage = "The password and confirmation password do not match.")]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public void OnGet()
    {
        _logger.LogWarning("Register OnGet called");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        _logger.LogWarning("====== REGISTER ONPOST CALLED ======");
        _logger.LogWarning($"Input object is null: {Input == null}");
        _logger.LogWarning($"Input.Email: '{Input?.Email}'");
        _logger.LogWarning($"Input.Password: '{Input?.Password}'");
        _logger.LogWarning($"Input.ConfirmPassword: '{Input?.ConfirmPassword}'");
        _logger.LogWarning($"ModelState.IsValid: {ModelState.IsValid}");

        if (!ModelState.IsValid)
        {
            _logger.LogWarning($"ModelState is INVALID. Errors:");
            foreach (var modelState in ModelState.Values)
            {
                foreach (var error in modelState.Errors)
                {
                    _logger.LogWarning($"  - {error.ErrorMessage}");
                }
            }
            return Page();
        }

        _logger.LogWarning($"Input.Email: {Input?.Email}");
        _logger.LogWarning($"Input.Password: {Input?.Password?.Length} chars");

        var user = new IdentityUser
        {
            UserName = Input.Email,
            Email = Input.Email
        };

        _logger.LogWarning($"About to create user: {user.Email}");

        var createResult = await _userManager.CreateAsync(
            user,
            Input.Password);

        _logger.LogWarning($"CreateAsync returned. Succeeded: {createResult.Succeeded}");

        if (!createResult.Succeeded)
        {
            _logger.LogError($"User creation failed with {createResult.Errors.Count()} errors:");
            foreach (var error in createResult.Errors)
            {
                _logger.LogError($"  - Code: {error.Code}, Description: {error.Description}");
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return Page();
        }

        _logger.LogInformation($"✓ User created: {user.Email} (Id: {user.Id})");
        _logger.LogInformation($"Attempting to assign Employee role...");

        // Every newly registered user starts as an Employee.
        var roleResult = await _userManager.AddToRoleAsync(
            user,
            "Employee");

        _logger.LogWarning($"AddToRoleAsync returned. Succeeded: {roleResult.Succeeded}");

        if (!roleResult.Succeeded)
        {
            _logger.LogError($"Failed to assign Employee role. Errors: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");

            foreach (var error in roleResult.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    $"Role assignment failed: {error.Description}");
            }

            // Remove the user if role assignment failed.
            _logger.LogWarning($"Deleting user {user.Email}...");
            var deleteResult = await _userManager.DeleteAsync(user);
            _logger.LogWarning($"Delete result: {deleteResult.Succeeded}");

            return Page();
        }

        _logger.LogInformation($"✓ Successfully assigned Employee role to {user.Email}");

        // Automatically log the newly registered employee in.
        await _signInManager.SignInAsync(
            user,
            isPersistent: false);

        _logger.LogInformation($"✓ User signed in, redirecting to /Index");

        return RedirectToPage("/Index");
    }
}