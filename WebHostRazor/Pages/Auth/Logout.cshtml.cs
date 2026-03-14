using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebHostRazor.Pages.Auth;

public class LogoutModel : PageModel
{
    private readonly SignInManager<IdentityUser> _signIn;
    public LogoutModel(SignInManager<IdentityUser> signIn) => _signIn = signIn;

    public async Task<IActionResult> OnGet()
    {
        await _signIn.SignOutAsync();
        return Redirect("/Auth/Login");
    }
}
