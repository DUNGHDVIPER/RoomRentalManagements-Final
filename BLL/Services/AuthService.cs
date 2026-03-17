using BLL.DTOs.Auth;              // ✅ đổi namespace DTO theo project bạn
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace BLL.Services;

public class AuthService : IAuthService
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IEmailService _emailService;

    public AuthService(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IEmailService emailService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _emailService = emailService;
    }

    public async Task<AuthResultDto> LoginAsync(LoginRequestDto dto, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return new AuthResultDto { Succeeded = false, Error = "Invalid email or password" };

        var result = await _signInManager.PasswordSignInAsync(
            user, dto.Password, dto.RememberMe, lockoutOnFailure: true);

        if (result.IsLockedOut)
            return new AuthResultDto { Succeeded = false, Error = "Account is locked. Please try again later." };

        if (!result.Succeeded)
            return new AuthResultDto { Succeeded = false, Error = "Invalid email or password" };

        var roles = await _userManager.GetRolesAsync(user);
        return new AuthResultDto
        {
            Succeeded = true,
            UserId = user.Id,
            Email = user.Email,
            Roles = roles.ToArray(),
            Message = "Login successful"
        };
    }

    public async Task<AuthResultDto> RegisterAsync(RegisterRequestDto dto, CancellationToken ct = default)
    {
        if (await IsEmailExistsAsync(dto.Email, ct))
            return new AuthResultDto { Succeeded = false, Error = "An account with this email already exists" };

        var user = new IdentityUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return new AuthResultDto { Succeeded = false, Error = errors };
        }

        // role mặc định (tuỳ bạn: Host/Customer)
        var roleResult = await _userManager.AddToRoleAsync(user, "Host");
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return new AuthResultDto { Succeeded = false, Error = "Failed to assign role" };
        }

        await _signInManager.SignInAsync(user, isPersistent: false);

        var roles = await _userManager.GetRolesAsync(user);
        return new AuthResultDto
        {
            Succeeded = true,
            UserId = user.Id,
            Email = user.Email,
            Roles = roles.ToArray(),
            Message = "Registration successful"
        };
    }

    public async Task<bool> IsEmailExistsAsync(string email, CancellationToken ct = default)
        => await _userManager.FindByEmailAsync(email) != null;

    public Task LogoutAsync(CancellationToken ct = default)
        => _signInManager.SignOutAsync();

    public async Task<ForgotPasswordResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto dto, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
        {
            return new ForgotPasswordResponseDto
            {
                Succeeded = true,
                Message = "If your email is registered, you will receive a password reset link shortly."
            };
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetLink =
            $"https://localhost:7121/Auth/ResetPassword?email={Uri.EscapeDataString(dto.Email)}&token={Uri.EscapeDataString(token)}";

        await _emailService.SendPasswordResetEmailAsync(dto.Email, resetLink, ct);

        return new ForgotPasswordResponseDto
        {
            Succeeded = true,
            Message = "If your email is registered, you will receive a password reset link shortly."
        };
    }

    public async Task<ForgotPasswordResponseDto> ResetPasswordAsync(ResetPasswordRequestDto dto, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return new ForgotPasswordResponseDto { Succeeded = false, Message = "Invalid request" };

        var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return new ForgotPasswordResponseDto { Succeeded = false, Message = errors };
        }

        return new ForgotPasswordResponseDto
        {
            Succeeded = true,
            Message = "Your password has been reset successfully."
        };
    }

    public Task<AuthResultDto> GoogleLoginStubAsync(string idToken, CancellationToken ct = default)
        => Task.FromResult(new AuthResultDto { Succeeded = false, Error = "Google login not implemented" });
}