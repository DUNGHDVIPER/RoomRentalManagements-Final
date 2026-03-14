using BLL.DTOs.Auth;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebHostRazor.Pages.Auth;

public class ForgotPasswordModel : PageModel
{
    private readonly IAuthService _authService;
    private readonly ILogger<ForgotPasswordModel> _logger;

    [BindProperty]
    public ForgotPasswordRequestDto ForgotPasswordRequest { get; set; } = new();

    public string Message { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public bool EmailSent { get; set; }

    public ForgotPasswordModel(IAuthService authService, ILogger<ForgotPasswordModel> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public void OnGet()
    {
        // Page load
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var result = await _authService.ForgotPasswordAsync(ForgotPasswordRequest);

            Message = result.Message;
            IsSuccess = result.Succeeded;
            EmailSent = result.Succeeded;

            if (result.Succeeded)
            {
                _logger.LogInformation("Password reset requested for email: {Email}", ForgotPasswordRequest.Email);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset request");
            Message = "An error occurred. Please try again.";
            IsSuccess = false;
        }

        return Page();
    }
}