using BLL.DTOs.Contract;
using BLL.Services.Interfaces;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace WebHostRazor.Pages.Host.Contracts.Wizard;

public class Step3Model : PageModel
{
    private readonly IContractService _contractService;
   

    public Step3Model(IContractService contractService) => _contractService = contractService;

    public CreateContractDto Preview { get; set; } = new();

    // ✅ Bind hidden fields when Confirm POST
    [BindProperty] public int RoomId { get; set; }
    [BindProperty] public int TenantId { get; set; }
    [BindProperty] public DateTime StartDate { get; set; }
    [BindProperty] public DateTime EndDate { get; set; }
    [BindProperty] public decimal Rent { get; set; }
    [BindProperty] public decimal Deposit { get; set; }
    [BindProperty] public bool ActivateNow { get; set; }

    // ✅ Read wizard state from query string
    public IActionResult OnGet(int roomId, int tenantId, string start, string end, string rent, string deposit, string activate)
    {
        RoomId = roomId;
        TenantId = tenantId;
        StartDate = DateTime.Parse(start, null, DateTimeStyles.RoundtripKind);
        EndDate = DateTime.Parse(end, null, DateTimeStyles.RoundtripKind);
        Rent = decimal.Parse(rent, CultureInfo.InvariantCulture);
        Deposit = decimal.Parse(deposit, CultureInfo.InvariantCulture);
        ActivateNow = activate == "1";

        Preview = new CreateContractDto
        {
            RoomId = RoomId,
            TenantId = TenantId,
            StartDate = StartDate,
            EndDate = EndDate,
            Rent = Rent,
            Deposit = Deposit,
            ActivateNow = ActivateNow
        };

        return Page();
    }

    public async Task<IActionResult> OnPostConfirm()
    {
        var dto = new CreateContractDto
        {
            RoomId = RoomId,
            TenantId = TenantId,
            StartDate = StartDate,
            EndDate = EndDate,
            Rent = Rent,
            Deposit = Deposit,
            ActivateNow = ActivateNow
        };

        try
        {
            var created = await _contractService.CreateAsync(dto);
            return RedirectToPage("/Host/Contracts/Details", new { id = created.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            Preview = dto; // để render lại preview
            return Page();
        }
    }

}
