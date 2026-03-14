namespace BLL.DTOs.Auth;

public class ForgotPasswordResponseDto
{
    public bool Succeeded { get; set; }
    public string Message { get; set; } = string.Empty;
}