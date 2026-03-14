using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAdmin.MVC.Models.Users;

namespace WebAdmin.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class UsersController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var users = await _userManager.Users.ToListAsync();
            var userVms = new List<UserListItemVm>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var isLocked = await _userManager.IsLockedOutAsync(user);

                userVms.Add(new UserListItemVm
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    Role = roles.FirstOrDefault() ?? "No Role",
                    Status = isLocked ? "Locked" : "Active",
                    CreatedAt = DateTime.UtcNow.AddDays(-7)
                });
            }

            return View(userVms);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading users");
            return View(new List<UserListItemVm>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        try
        {
            ViewBag.Roles = await GetRolesAsync();
            return View(new UserCreateVm());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading create user page");
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateVm model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = await GetRolesAsync();
            return View(model);
        }

        try
        {
            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                ViewBag.Roles = await GetRolesAsync();
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, model.Role);

            if (!model.IsActive)
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            }

            TempData["SuccessMessage"] = "User created successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            ModelState.AddModelError("", "An error occurred while creating the user.");
            ViewBag.Roles = await GetRolesAsync();
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            var roles = await _userManager.GetRolesAsync(user);
            var isLocked = await _userManager.IsLockedOutAsync(user);

            var model = new UserEditVm
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Role = roles.FirstOrDefault() ?? "Host",
                IsActive = !isLocked
            };

            ViewBag.Roles = await GetRolesAsync();
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user for edit");
            TempData["ErrorMessage"] = "An error occurred while loading the user.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditVm model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = await GetRolesAsync();
            return View(model);
        }

        try
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            // Update basic user properties only
            user.Email = model.Email;
            user.UserName = model.Email;
            user.PhoneNumber = model.PhoneNumber;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                ViewBag.Roles = await GetRolesAsync();
                return View(model);
            }

            // Update user role
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }
            await _userManager.AddToRoleAsync(user, model.Role);

            // Update lockout status
            if (model.IsActive)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            }

            TempData["SuccessMessage"] = "User updated successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user");
            ModelState.AddModelError("", "An error occurred while updating the user.");
            ViewBag.Roles = await GetRolesAsync();
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lock(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            var isCurrentlyLocked = await _userManager.IsLockedOutAsync(user);

            if (isCurrentlyLocked)
            {
                // Unlock the user
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["SuccessMessage"] = "User unlocked successfully!";
            }
            else
            {
                // Lock the user
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                TempData["SuccessMessage"] = "User locked successfully!";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling user lock status");
            TempData["ErrorMessage"] = "An error occurred while updating the user status.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            // Unlock user by setting lockout end to null
            await _userManager.SetLockoutEndDateAsync(user, null);
            
            TempData["SuccessMessage"] = "User unlocked successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking user");
            TempData["ErrorMessage"] = "An error occurred while unlocking the user.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                result.Succeeded ? "User deleted successfully!" : "Failed to delete user.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user");
            TempData["ErrorMessage"] = "An error occurred while deleting the user.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<List<string>> GetRolesAsync()
    {
        try
        {
            return await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading roles");
            return new List<string> { "Admin", "Host", "Customer" }; // Fallback
        }
    }
}