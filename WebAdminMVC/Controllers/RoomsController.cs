using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

using WebAdmin.MVC.Models;
using WebAdmin.MVC.Models.Rooms;

namespace WebAdmin.MVC.Controllers;


public class RoomsController : Controller
{
    private readonly IRoomService _roomService;

    public RoomsController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    public async Task<IActionResult> Index()
    {
        var rooms = await _roomService.GetAllAsync();
        return View(rooms);
    }
}
