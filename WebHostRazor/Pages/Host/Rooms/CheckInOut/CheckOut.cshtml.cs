using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BLL.Services.Interfaces;

namespace WebHostRazor.Pages.Host.Rooms.CheckInOut
{
    public class CheckOutModel : PageModel
    {
        private readonly IStayHistoryService _stayService;

        public CheckOutModel(IStayHistoryService stayService)
        {
            _stayService = stayService;
        }

        [BindProperty]
        public int RoomId { get; set; }

        public IActionResult OnGet(int roomId)
        {
            if (roomId <= 0)
                return RedirectToPage("../Index");

            RoomId = roomId;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (RoomId <= 0)
            {
                ModelState.AddModelError("", "Invalid room.");
                return Page();
            }

            try
            {
                await _stayService.CheckOutAsync(RoomId);
                return RedirectToPage("../Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return Page();
            }
        }
    }
}