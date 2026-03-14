using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAdminMVC.Models.Roles;

namespace WebAdminMVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class RolesController(
    RoleManager<IdentityRole> roleManager,
    UserManager<IdentityUser> userManager,
    ILogger<RolesController> logger) : Controller
{
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;
    private readonly UserManager<IdentityUser> _userManager = userManager;
    private readonly ILogger<RolesController> _logger = logger;

    private static readonly string[] SystemRoles = ["SuperAdmin", "Admin", "Host", "Tenant"];

    // GET: /Roles/Index
    public async Task<IActionResult> Index()
    {
        try
        {
            var roles = await _roleManager.Roles.ToListAsync();
            var roleVms = new List<RoleListItemVm>();

            foreach (var role in roles)
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);

                roleVms.Add(new RoleListItemVm
                {
                    Id = role.Id,
                    Name = role.Name!,
                    UserCount = usersInRole.Count,
                    CreatedAt = DateTime.UtcNow 
                });
            }

            return View(roleVms);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading roles");
            TempData["Error"] = "Error loading roles: " + ex.Message;
            return View(new List<RoleListItemVm>());
        }
    }

    // GET: /Roles/Create
    public IActionResult Create()
    {
        return View(new RoleCreateVm());
    }

    // POST: /Roles/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoleCreateVm model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var roleExists = await _roleManager.RoleExistsAsync(model.Name);
            if (roleExists)
            {
                ModelState.AddModelError("Name", "Role already exists");
                return View(model);
            }

            var role = new IdentityRole(model.Name);
            var result = await _roleManager.CreateAsync(role);

            if (result.Succeeded)
            {
                _logger.LogInformation("Created role: {RoleName}", model.Name);
                TempData["Success"] = "Role created successfully";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role: {RoleName}", model.Name);
            ModelState.AddModelError(string.Empty, "An error occurred while creating the role");
        }

        return View(model);
    }

    // GET: /Roles/Edit/5
    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
        {
            return NotFound();
        }

        var model = new RoleEditVm
        {
            Id = role.Id,
            Name = role.Name!
        };

        return View(model);
    }

    // POST: /Roles/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, RoleEditVm model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            role.Name = model.Name;
            var result = await _roleManager.UpdateAsync(role);

            if (result.Succeeded)
            {
                _logger.LogInformation("Updated role: {RoleName}", model.Name);
                TempData["Success"] = "Role updated successfully";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role: {RoleName}", model.Name);
            ModelState.AddModelError(string.Empty, "An error occurred while updating the role");
        }

        return View(model);
    }

    // POST: /Roles/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                TempData["Error"] = "Role not found";
                return RedirectToAction(nameof(Index));
            }

            // Check if role is being used by any users
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            if (usersInRole.Count != 0)
            {
                TempData["Error"] = "Cannot delete role that is assigned to users";
                return RedirectToAction(nameof(Index));
            }

            // Prevent deleting system roles
            if (SystemRoles.Contains(role.Name))
            {
                TempData["Error"] = "Cannot delete system roles";
                return RedirectToAction(nameof(Index));
            }

            var result = await _roleManager.DeleteAsync(role);
            if (result.Succeeded)
            {
                _logger.LogInformation("Deleted role: {RoleName}", role.Name);
                TempData["Success"] = "Role deleted successfully";
            }
            else
            {
                TempData["Error"] = "Error deleting role: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role with id: {RoleId}", id);
            TempData["Error"] = "An error occurred while deleting the role";
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: /Roles/Details/5
    public async Task<IActionResult> Details(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
        {
            return NotFound();
        }

        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);

        var model = new RoleDetailsVm
        {
            Id = role.Id,
            Name = role.Name!,
            Users = usersInRole.Select(u => new UserInRoleVm
            {
                Id = u.Id,
                Email = u.Email!,
                UserName = u.UserName!
            }).ToList()
        };

        return View(model);
    }
}