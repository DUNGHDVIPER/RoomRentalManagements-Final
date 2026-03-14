using System.ComponentModel.DataAnnotations;
using BLL.DTOs.Contract;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebHostRazor.Pages.Host.Contracts;

public class RenewModel : PageModel
{
    private readonly IContractService _service;

    public RenewModel(IContractService service)
    {
        _service = service;
    }

    [BindProperty] public RenewVm Vm { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken ct)
    {
        try
        {
            var c = await _service.GetByIdAsync(id, ct);

            // Giống MVC: nếu chưa Active thì báo và quay lại Details
            if (!c.IsActive)
            {
                TempData["Error"] = "Không thể Renew vì hợp đồng chưa ACTIVE. Hãy Activate trước.";
                return RedirectToPage("./Details", new { id });
            }

            Vm.ContractId = c.Id;
            Vm.NewStartDate = c.EndDate.AddDays(1);
            Vm.NewEndDate = c.EndDate.AddMonths(12);
            Vm.NewRent = c.Rent;
            Vm.NewDeposit = c.Deposit;

            return Page();
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("./Index");
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (Vm.NewEndDate < Vm.NewStartDate)
            ModelState.AddModelError(nameof(Vm.NewEndDate), "New end date must be after start date.");

        if (!ModelState.IsValid) return Page();

        try
        {
            var dto = new RenewContractDto
            {
                ContractId = Vm.ContractId,
                NewStartDate = Vm.NewStartDate,
                NewEndDate = Vm.NewEndDate,
                NewRent = Vm.NewRent,
                NewDeposit = Vm.NewDeposit,
                Reason = Vm.Reason,
                RequireStartNextDay = true
            };

            var renewed = await _service.RenewAsync(dto, actorUserId: null, ct);

            TempData["Ok"] = "Renewed successfully.";
            TempData["Success"] = TempData["Ok"];
            return RedirectToPage("./Details", new { id = renewed.Id });
        }
        catch (ValidationException ex)
        {
            TempData["Err"] = ex.Message;
            TempData["Error"] = TempData["Err"];
            return RedirectToPage(new { id = Vm.ContractId }); // PRG về lại Renew để sửa
        }
        catch (InvalidOperationException ex)
        {
            TempData["Err"] = ex.Message;
            TempData["Error"] = TempData["Err"];
            return RedirectToPage(new { id = Vm.ContractId });
        }
        catch (Exception ex)
        {
            TempData["Err"] = ex.Message;
            TempData["Error"] = TempData["Err"];
            return RedirectToPage(new { id = Vm.ContractId });
        }
    }

    public class RenewVm
    {
        [Required] public int ContractId { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime NewStartDate { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime NewEndDate { get; set; }

        [Required, Range(0, double.MaxValue)]
        public decimal NewRent { get; set; }

        [Required, Range(0, double.MaxValue)]
        public decimal NewDeposit { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }
    }
}