//using BLL.Services.Interfaces;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.RazorPages;

//namespace WebHostRazor.Pages.Host.Contracts;

//public class UploadAttachmentModel(IContractService service, IWebHostEnvironment env) : PageModel
//{
//    private readonly IContractService _service = service;
//    private readonly IWebHostEnvironment _env = env;

//    [BindProperty]
//    public new IFormFile? File { get; set; }

//    public IActionResult OnGet() => Page();

//    public async Task<IActionResult> OnPost(long id)
//    {
//        if (File == null || File.Length == 0)
//        {
//            ModelState.AddModelError("", "Please choose a file.");
//            return Page();
//        }

//        if (File.Length > 10 * 1024 * 1024)
//        {
//            ModelState.AddModelError("", "File too large (max 10MB).");
//            return Page();
//        }

//        // shared folder: ../SharedUploads/contracts
//        var sharedContractsDir = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "..", "SharedUploads", "contracts"));
//        Directory.CreateDirectory(sharedContractsDir);

//        var safeFileName = Path.GetFileName(File.FileName);
//        var storedName = $"{Guid.NewGuid():N}_{safeFileName}";
//        var fullPath = Path.Combine(sharedContractsDir, storedName);

//        await using var stream = System.IO.File.Create(fullPath);
//        await File.CopyToAsync(stream);

//        var url = $"/uploads/contracts/{storedName}"; // ✅ URL chung
//        await _service.AddAttachmentStubAsync((int)id, safeFileName, url);

//        return RedirectToPage("./Details", new { id });
//    }
//}

using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebHostRazor.Pages.Host.Contracts;

public class UploadAttachmentModel(
    IContractService service,
    IWebHostEnvironment env,
    ILogger<UploadAttachmentModel> logger) : PageModel
{
    private readonly IContractService _service = service;
    private readonly IWebHostEnvironment _env = env;
    private readonly ILogger<UploadAttachmentModel> _logger = logger;

    [BindProperty]
    public new IFormFile? File { get; set; }

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPost(long id)
    {
        if (id <= 0)
        {
            ModelState.AddModelError("", "Invalid contract id.");
            return Page();
        }

        if (File == null || File.Length == 0)
        {
            ModelState.AddModelError("", "Please choose a file.");
            return Page();
        }

        if (File.Length > 10 * 1024 * 1024)
        {
            ModelState.AddModelError("", "File too large (max 10MB).");
            return Page();
        }

        var extension = Path.GetExtension(File.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".pdf", ".png", ".jpg", ".jpeg", ".webp", ".doc", ".docx" };

        if (!allowedExtensions.Contains(extension))
        {
            ModelState.AddModelError("", "Only pdf, image, doc, docx files are allowed.");
            return Page();
        }

        try
        {
            var sharedContractsDir = Path.GetFullPath(
                Path.Combine(_env.ContentRootPath, "..", "SharedUploads", "contracts"));

            Directory.CreateDirectory(sharedContractsDir);

            var safeFileName = Path.GetFileName(File.FileName);
            var storedName = $"{Guid.NewGuid():N}_{safeFileName}";
            var fullPath = Path.Combine(sharedContractsDir, storedName);

            await using (var stream = System.IO.File.Create(fullPath))
            {
                await File.CopyToAsync(stream);
            }

            var url = $"/uploads/contracts/{storedName}";

            await _service.AddAttachmentStubAsync((int)id, safeFileName, url);

            TempData["SuccessMessage"] = "Contract file uploaded successfully.";
            return RedirectToPage("./Details", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload attachment failed for contract {ContractId}", id);
            ModelState.AddModelError("", $"Upload failed: {ex.Message}");
            return Page();
        }
    }
}