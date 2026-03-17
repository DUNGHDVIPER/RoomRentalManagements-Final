using System.ComponentModel.DataAnnotations;
using BLL.DTOs.Contract;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebHostRazor.Pages.Host.Contracts;

public class TerminateModel : PageModel
{
    private readonly IContractService _service;

    public TerminateModel(IContractService service)
    {
        _service = service;
    }

    [BindProperty] public TerminateVm Vm { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken ct)
    {
        try
        {
            var c = await _service.GetByIdAsync(id, ct);

            if (!c.IsActive)
            {
                TempData["Error"] = "Không thể Terminate vì hợp đồng chưa ACTIVE. Hãy Activate trước.";
                return RedirectToPage("./Details", new { id });
            }

            Vm.ContractId = c.Id;

            var today = DateTime.Today;
            Vm.TerminateDate = today < c.StartDate ? c.StartDate
                             : today > c.EndDate ? c.EndDate
                             : today;

            return Page();
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("./Details", new { id });
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) return Page();

        try
        {
            var dto = new TerminateContractDto
            {
                ContractId = Vm.ContractId,
                TerminateDate = Vm.TerminateDate,
                Reason = Vm.Reason
            };

            await _service.TerminateAsync(dto, actorUserId: null, ct);

            TempData["Success"] = "Terminated successfully.";
            return RedirectToPage("./Details", new { id = Vm.ContractId });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("./Details", new { id = Vm.ContractId });
        }
    }

    public class TerminateVm
    {
        [Required] public int ContractId { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime TerminateDate { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }
    }
}