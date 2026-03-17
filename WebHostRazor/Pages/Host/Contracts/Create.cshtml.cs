using System.ComponentModel.DataAnnotations;
using BLL.DTOs.Contract;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebHostRazor.Pages.Host.Contracts;

public class CreateModel : PageModel
{
    private readonly IContractService _service;

    public CreateModel(IContractService service)
    {
        _service = service;
    }

    [BindProperty] public CreateVm Vm { get; set; } = new();

    public void OnGet()
    {
        // default
        if (Vm.StartDate == default) Vm.StartDate = DateTime.Today;
        if (Vm.EndDate == default) Vm.EndDate = DateTime.Today.AddMonths(6);
        Vm.ActivateNow = true;

    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) return Page();
        
        try
        {
            var dto = new CreateContractDto
            {
                RoomId = Vm.RoomId,
                TenantId = Vm.TenantId,
                StartDate = Vm.StartDate,
                EndDate = Vm.EndDate,
                Rent = Vm.Rent,
                Deposit = Vm.Deposit,
                ActivateNow = Vm.ActivateNow
            };

            var created = await _service.CreateAsync(dto, actorUserId: null, ct);

            TempData["Ok"] = "Created successfully.";
            TempData["Success"] = TempData["Ok"]; // tương thích UI cũ

            return RedirectToPage("./Details", new { id = created.Id });
        }
        catch (ValidationException ex)
        {
            TempData["Err"] = ex.Message;
            TempData["Error"] = TempData["Err"]; // tương thích UI cũ
            return RedirectToPage(); // PRG về lại Create
        }
        catch (InvalidOperationException ex)
        {
            TempData["Err"] = ex.Message;
            TempData["Error"] = TempData["Err"];
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["Err"] = ex.Message;
            TempData["Error"] = TempData["Err"];
            return RedirectToPage();
        }
    }

    public class CreateVm
    {
        [Required] public int RoomId { get; set; }
        [Required] public int TenantId { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required, Range(0, double.MaxValue)]
        public decimal Rent { get; set; }

        [Required, Range(0, double.MaxValue)]
        public decimal Deposit { get; set; }

        public bool ActivateNow { get; set; } = true;
    }
}