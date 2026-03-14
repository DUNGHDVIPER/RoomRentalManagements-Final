using System.ComponentModel.DataAnnotations;
using BLL.DTOs.Common;
using BLL.DTOs.Tenant;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebHostRazor.Pages.Host.Rooms.CheckInOut
{
    public class CheckInModel : PageModel
    {
        private readonly IStayHistoryService _stayService;
        private readonly ITenantService _tenantService;

        public CheckInModel(
            IStayHistoryService stayService,
            ITenantService tenantService)
        {
            _stayService = stayService;
            _tenantService = tenantService;
        }

        [BindProperty]
        public int RoomId { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Please select a tenant.")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid tenant.")]
        public int TenantId { get; set; }

        [BindProperty]
        [StringLength(250, ErrorMessage = "Note cannot exceed 250 characters.")]
        public string? Note { get; set; }

        public List<TenantDto> Tenants { get; set; } = new();

        public async Task OnGetAsync(int roomId)
        {
            RoomId = roomId;
            await LoadTenants();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (RoomId <= 0)
            {
                ModelState.AddModelError("", "Invalid room.");
            }

            if (!ModelState.IsValid)
            {
                await LoadTenants();
                return Page();
            }

            try
            {
                await _stayService.CheckInAsync(RoomId, TenantId, Note);
                return RedirectToPage("../Index");
            }
            catch (Exception ex)
            {
                await LoadTenants();
                ModelState.AddModelError("", ex.Message);
                return Page();
            }
        }

        private async Task LoadTenants()
        {
            var result = await _tenantService.GetTenantsAsync(
                new PagedRequestDto
                {
                    PageNumber = 1,
                    PageSize = 100
                });

            Tenants = result.Items.ToList();
        }
    }
}