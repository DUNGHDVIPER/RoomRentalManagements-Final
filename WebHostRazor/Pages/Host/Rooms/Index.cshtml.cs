using BLL.Dtos;
using BLL.DTOs;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace WebHostRazor.Pages.Host.Rooms;

public class IndexModel : PageModel
{
    private readonly IRoomService _roomService;

    public IndexModel(IRoomService roomService)
    {
        _roomService = roomService;
    }

    public IReadOnlyList<RoomDto> Rooms { get; private set; } = [];

 public async Task OnGetAsync()
    {
        Rooms = await _roomService.GetAllAsync();
    }
}
