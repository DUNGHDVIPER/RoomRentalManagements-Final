using BLL.Common;
using BLL.DTOs.Tenant;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebHostRazor.Pages.Host.Tenants;

public class IndexModel : PageModel
{
    private readonly ITenantService _tenantService;

    public IndexModel(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    public List<TenantDto> Tenants { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? GenderFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int TotalPages { get; set; }
    public int PageSize { get; set; } = 5;

    public async Task OnGetAsync()
    {
        var result = await _tenantService.GetTenantsAsync(
            new BLL.DTOs.Common.PagedRequestDto
            {
                PageNumber = 1,
                PageSize = 100
            });

        var query = result.Items.AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            query = query.Where(t =>
                t.FullName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                (t.Phone != null && t.Phone.Contains(SearchTerm)) ||
                (t.CCCD != null && t.CCCD.Contains(SearchTerm)));
        }

        if (!string.IsNullOrWhiteSpace(StatusFilter))
            query = query.Where(t => t.Status == StatusFilter);

        if (!string.IsNullOrWhiteSpace(GenderFilter))
            query = query.Where(t => t.Gender == GenderFilter);

        var totalItems = query.Count();
        TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

        Tenants = query
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();
    }
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            await _tenantService.DeleteAsync(id);
            return RedirectToPage("Index");
        }
        catch (Exceptions.NotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}