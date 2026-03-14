using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebAdmin.MVC.Models.Auth;

namespace WebAdmin.MVC.Controllers;

public class AccountController : Controller
{
    private const string LoginViewPath = "~/Views/Auth/Login.cshtml";

    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;

    public AccountController(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(LoginViewPath, new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(LoginViewPath, vm);

        var user = await _userManager.FindByEmailAsync(vm.Email);
        if (user == null)
        {
            vm.ErrorMessage = "Invalid email or password.";
            return View(LoginViewPath, vm);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,
            vm.Password,
            vm.RememberMe,
            lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            vm.ErrorMessage = result.IsLockedOut
                ? "This account is locked."
                : "Invalid email or password.";

            return View(LoginViewPath, vm);
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Contracts");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
        => View("~/Views/Auth/AccessDenied.cshtml");
}