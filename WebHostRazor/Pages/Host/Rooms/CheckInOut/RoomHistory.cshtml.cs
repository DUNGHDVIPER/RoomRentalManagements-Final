using BLL.Services.Interfaces;
using DAL.Entities.Tenanting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebHostRazor.Pages.Host.Rooms.CheckInOut
{
    public class RoomHistoryModel : PageModel
    {
        private readonly IStayHistoryService _stayService;

        public RoomHistoryModel(IStayHistoryService stayService)
        {
            _stayService = stayService;
        }

        public List<StayHistory> Histories { get; set; } = new();

        public async Task OnGetAsync(int roomId)
        {
            Histories = await _stayService.GetRoomHistoryAsync(roomId);
        }
    }
}
