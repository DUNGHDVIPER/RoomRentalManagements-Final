using BLL.DTOs.Room;
using BLL.Services.Interfaces;
using DAL.Data;
using DAL.Entities.Common;
using DAL.Entities.Property;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebAdmin.MVC.Models.Rooms;

namespace WebAdmin.MVC.Controllers;

public class RoomsController : Controller
{
    private readonly IRoomService _roomService;
    private readonly CloudinaryService _cloudinaryService;
    private readonly AppDbContext _context;

    public RoomsController(
        IRoomService roomService,
        CloudinaryService cloudinaryService,
        AppDbContext context)
    {
        _roomService = roomService;
        _cloudinaryService = cloudinaryService;
        _context = context;
    }

    // ===================== LIST =====================

    public async Task<IActionResult> Index(string search, string status, int page = 1)
    {
        var rooms = await _roomService.GetAllAsync();

        page = page < 1 ? 1 : page;

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            rooms = rooms.Where(r =>
                (r.RoomName != null && r.RoomName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (r.RoomCode != null && r.RoomCode.Contains(search, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<RoomStatus>(status, true, out var parsedStatus))
        {
            rooms = rooms.Where(r => r.Status == parsedStatus).ToList();
        }

        int pageSize = 5;

        var pagedRooms = rooms
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)rooms.Count / pageSize);

        var vm = pagedRooms.Select(r => new RoomListItemVm
        {
            Id = r.RoomId,
            Name = r.RoomName ?? r.RoomCode,
            Block = r.BlockName ?? "",
            Floor = r.FloorNumber?.ToString() ?? "",
            Price = r.CurrentBasePrice,
            Status = r.Status.ToString()
        }).ToList();

        return View(vm);
    }

    // ===================== CREATE =====================

    [HttpGet]
    public IActionResult Create()
    {
        var amenities = _context.Amenities.ToList();
        var floors = _context.Floors.ToList();

        var vm = new RoomCreateVm
        {
            Amenities = amenities.Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = a.AmenityName
            }).ToList(),

            Floors = floors.Select(f => new SelectListItem
            {
                Value = f.Id.ToString(),
                Text = f.FloorName
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoomCreateVm vm)
    {
        var floorExists = await _context.Floors.AnyAsync(x => x.Id == vm.FloorId);
        if (!floorExists)
        {
            ModelState.AddModelError(nameof(vm.FloorId), "Floor không tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            vm.Amenities = _context.Amenities.Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = a.AmenityName
            }).ToList();

            vm.Floors = _context.Floors.Select(f => new SelectListItem
            {
                Value = f.Id.ToString(),
                Text = f.FloorName
            }).ToList();

            return View(vm);
        }

        var dto = new CreateRoomDto
        {
            FloorId = vm.FloorId,
            RoomCode = vm.RoomCode,
            RoomName = vm.RoomName,
            AreaM2 = vm.AreaM2,
            MaxOccupants = vm.MaxOccupants,
            CurrentBasePrice = vm.Price,
            Status = Enum.Parse<RoomStatus>(vm.Status),
            Description = vm.Description,
            AmenityIds = vm.AmenityIds?.ToArray() ?? Array.Empty<int>()
        };

        await _roomService.CreateRoomAsync(dto);

        return RedirectToAction(nameof(Index));
    }

    // ===================== EDIT =====================

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var room = await _roomService.GetRoomDetailAsync(id);

        if (room == null)
            return NotFound();

        var amenities = _context.Amenities.ToList();

        var vm = new RoomEditVm
        {
            Id = room.RoomId,
            RoomCode = room.RoomCode,
            RoomName = room.RoomName,
            AreaM2 = room.AreaM2,
            MaxOccupants = room.MaxOccupants,
            CurrentBasePrice = room.CurrentBasePrice,
            Status = room.Status,
            Description = room.Description,

            AmenityIds = room.AmenityIds?.ToList() ?? new List<int>(),

            Amenities = amenities.Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = a.AmenityName,
                Selected = room.AmenityIds != null && room.AmenityIds.Contains(a.Id)
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(RoomEditVm vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Amenities = _context.Amenities.Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = a.AmenityName
            }).ToList();

            return View(vm);
        }

        var dto = new UpdateRoomDto
        {
            RoomCode = vm.RoomCode,
            RoomName = vm.RoomName,
            AreaM2 = vm.AreaM2,
            MaxOccupants = vm.MaxOccupants,
            CurrentBasePrice = vm.CurrentBasePrice,
            Status = vm.Status,
            Description = vm.Description,
            AmenityIds = vm.AmenityIds?.ToArray() ?? Array.Empty<int>()
        };

        await _roomService.UpdateRoomAsync(vm.Id, dto);

        return RedirectToAction(nameof(Index));
    }

    // ===================== DETAILS =====================

    public async Task<IActionResult> Details(int id)
    {
        var room = await _roomService.GetRoomDetailAsync(id);

        if (room == null)
            return NotFound();

        var images = _context.RoomImages
            .Where(x => x.RoomId == id)
            .ToList();

        var amenities = new List<string>();

        if (room.AmenityIds != null && room.AmenityIds.Any())
        {
            amenities = _context.Amenities
                .Where(a => room.AmenityIds.Contains(a.Id))
                .Select(a => a.AmenityName)
                .ToList();
        }

        var vm = new RoomDetailsVm
        {
            Id = room.RoomId,
            RoomCode = room.RoomCode,
            RoomName = room.RoomName,
            Price = room.CurrentBasePrice,
            Status = room.Status.ToString(),
            Amenities = amenities,
            Images = images
                .Select(i => new RoomImageVm
                {
                    ImageId = i.ImageId,
                    ImageUrl = i.ImageUrl,
                    IsPrimary = i.IsPrimary
                })
                .ToList()
        };

        return View(vm);
    }

    // ===================== UPLOAD IMAGE =====================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImage(int roomId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return RedirectToAction("Details", new { id = roomId });

        var imageUrl = await _cloudinaryService.UploadImageAsync(file);

        if (string.IsNullOrEmpty(imageUrl))
            return RedirectToAction("Details", new { id = roomId });

        var image = new RoomImage
        {
            RoomId = roomId,
            ImageUrl = imageUrl,
            IsPrimary = false,
            CreatedAt = DateTime.Now
        };

        _context.RoomImages.Add(image);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", new { id = roomId });
    }

    // ===================== DELETE IMAGE =====================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int imageId, int roomId)
    {
        var image = await _context.RoomImages.FindAsync(imageId);

        if (image != null)
        {
            _context.RoomImages.Remove(image);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Details", new { id = roomId });
    }

    // ===================== DELETE ROOM =====================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _roomService.DeleteRoomAsync(id);

        return RedirectToAction(nameof(Index));
    }

    // ===================== PRICE HISTORY =====================

    public async Task<IActionResult> PriceHistory(int id)
    {
        var history = await _roomService.GetRoomPriceHistoryAsync(id);

        var vm = history.Select(x => new RoomPriceHistoryVm
        {
            OldPrice = x.OldPrice,
            NewPrice = x.NewPrice,
            ChangedAt = x.ChangedAt,
            Note = x.Note
        }).ToList();

        ViewBag.RoomId = id;

        return View(vm);
    }
}