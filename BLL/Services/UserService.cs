using BLL.Common;
using BLL.DTOs.Common;
using BLL.DTOs.Identity;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BLL.Services;

public class UserService : IUserService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<UserService> _logger;

    public UserService(UserManager<IdentityUser> userManager, ILogger<UserService> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<UserDto> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            throw new InvalidOperationException("User not found");

        var roles = await _userManager.GetRolesAsync(user);
        var isLocked = await _userManager.IsLockedOutAsync(user);

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            UserName = user.UserName!,
            IsLocked = isLocked,
            Roles = roles.ToArray()
        };
    }

    public async Task<UserDto> UpdateAsync(string id, UpdateUserDto dto, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            throw new InvalidOperationException("User not found");

        // Update basic info (chỉ cho phép update UserName, không update Email)
        user.UserName = dto.Email; // Use email as username

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Update failed: {errors}");
        }

        // Handle lock/unlock
        if (dto.IsLocked)
        {
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
        }
        else
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
        }

        // Update roles
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRolesAsync(user, dto.Roles);

        return await GetByIdAsync(id, ct);
    }

    // Implement other methods as needed...
    public Task<UserDto> CreateAsync(CreateUserDto dto, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(string id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<PagedResultDto<UserDto>> GetUsersAsync(PagedRequestDto req, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task LockAsync(string id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task UnlockAsync(string id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}