using System.Threading;
using System.Threading.Tasks;
using BLL.DTOs.Auth;

namespace BLL.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResultDto> LoginAsync(LoginRequestDto dto, CancellationToken ct = default);
    Task<AuthResultDto> RegisterAsync(RegisterRequestDto dto, CancellationToken ct = default);
    Task LogoutAsync(CancellationToken ct = default);

    Task<ForgotPasswordResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto dto, CancellationToken ct = default);
    Task<ForgotPasswordResponseDto> ResetPasswordAsync(ResetPasswordRequestDto dto, CancellationToken ct = default);

    Task<bool> IsEmailExistsAsync(string email, CancellationToken ct = default);
    Task<AuthResultDto> GoogleLoginStubAsync(string idToken, CancellationToken ct = default);
}