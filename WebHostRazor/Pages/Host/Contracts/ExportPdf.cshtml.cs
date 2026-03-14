using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BLL.Services.Interfaces;
using WebHostRazor.Common;

namespace WebHostRazor.Pages.Host.Contracts;

public class ExportPdfModel : PageModel
{
    private readonly IContractService _service;

    public ExportPdfModel(IContractService service)
    {
        _service = service;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            var actorUserId = User.GetActorUserId();
            var bytes = await _service.ExportPdfStubAsync(id, actorUserId);

            // nếu muốn tên file theo ContractCode, bạn có thể load dto trước,
            // hoặc trong service return thêm code. Tạm dùng id:
            return File(bytes, "application/pdf", $"Contract_{id}.pdf");
        }
        catch (Exception ex)
        {
            TempData["Err"] = ex.Message;
            return RedirectToPage("./Details", new { id });
        }
    }
}