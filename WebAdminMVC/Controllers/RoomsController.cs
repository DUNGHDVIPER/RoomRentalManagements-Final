using BLL.DTOs.Property;
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
            Floor = r.FloorName ?? "",
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

        var floors = _context.Floors
            .Include(f => f.Block)
            .ToList();

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
                Text = $"{f.Block.BlockName} - {f.FloorName}"
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

            vm.Floors = _context.Floors
                .Include(f => f.Block)
                .Select(f => new SelectListItem
                {
                    Value = f.Id.ToString(),
                    Text = $"{f.Block.BlockName} - {f.FloorName}"
                }).ToList();

            return View(vm);
        }

        var dto = new CreateRoomDto
        {
            FloorId = vm.FloorId.Value,
            RoomCode = vm.RoomCode.Trim(),
            RoomName = vm.RoomName?.Trim(),
            AreaM2 = vm.AreaM2,
            MaxOccupants = vm.MaxOccupants,
            CurrentBasePrice = vm.Price,
            Status = Enum.TryParse<RoomStatus>(vm.Status, out var status)
                ? status
                : RoomStatus.Available,
            Description = vm.Description,
            AmenityIds = vm.AmenityIds?.ToArray() ?? Array.Empty<int>()
        };

        RoomDto room;

        try
        {
            room = await _roomService.CreateRoomAsync(dto);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("RoomCode", ex.Message);

            vm.Amenities = _context.Amenities.Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = a.AmenityName
            }).ToList();

            vm.Floors = _context.Floors
                .Include(f => f.Block)
                .Select(f => new SelectListItem
                {
                    Value = f.Id.ToString(),
                    Text = $"{f.Block.BlockName} - {f.FloorName}"
                }).ToList();

            return View(vm);
        }

        var roomId = room.RoomId;

        if (vm.Images != null && vm.Images.Any())
        {
            var urls = new List<string>();

            foreach (var file in vm.Images)
            {
                var url = await _cloudinaryService.UploadImageAsync(file);
                urls.Add(url);
            }

            await _roomService.AddRoomImagesAsync(roomId, urls.ToArray());
        }

        return RedirectToAction(nameof(Index));
    }

    // ===================== EDIT =====================

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var room = await _roomService.GetRoomDetailAsync(id);

        if (room == null)
            return NotFound();

        var amenities = await _context.Amenities.ToListAsync();

        var images = await _context.RoomImages
            .Where(x => x.RoomId == id)
            .ToListAsync();

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
            }).ToList(),

            Images = images.Select(i => new RoomImageVm
            {
                ImageId = i.ImageId,
                ImageUrl = i.ImageUrl,
                IsPrimary = i.IsPrimary
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
            vm.Amenities = await _context.Amenities
                .Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = a.AmenityName
                })
                .ToListAsync();

            return View(vm);
        }

        var dto = new UpdateRoomDto
        {
            RoomCode = vm.RoomCode.Trim(),
            RoomName = vm.RoomName?.Trim(),
            AreaM2 = vm.AreaM2,
            MaxOccupants = vm.MaxOccupants,
            CurrentBasePrice = vm.CurrentBasePrice,
            Status = vm.Status,
            Description = vm.Description,
            AmenityIds = vm.AmenityIds?.ToArray() ?? Array.Empty<int>()
        };

        await _roomService.UpdateRoomAsync(vm.Id, dto);

        if (vm.NewImages != null && vm.NewImages.Any())
        {
            var urls = new List<string>();

            foreach (var file in vm.NewImages)
            {
                var url = await _cloudinaryService.UploadImageAsync(file);
                urls.Add(url);
            }

            await _roomService.AddRoomImagesAsync(vm.Id, urls.ToArray());
        }

        return RedirectToAction(nameof(Index));
    }

    // ===================== DETAILS =====================

    public async Task<IActionResult> Details(int id)
    {
        var room = await _roomService.GetRoomDetailAsync(id);

        if (room == null)
            return NotFound();

        var images = await _context.RoomImages
            .Where(x => x.RoomId == id)
            .ToListAsync();

        var amenities = new List<string>();

        if (room.AmenityIds != null && room.AmenityIds.Any())
        {
            amenities = await _context.Amenities
                .Where(a => room.AmenityIds.Contains(a.Id))
                .Select(a => a.AmenityName)
                .ToListAsync();
        }

        var vm = new RoomDetailsVm
        {
            Id = room.RoomId,
            RoomCode = room.RoomCode,
            RoomName = room.RoomName,
            Block = room.BlockName,
            Floor = room.FloorName,
            Price = room.CurrentBasePrice,
            Status = room.Status.ToString(),
            Amenities = amenities,
            Images = images.Select(i => new RoomImageVm
            {
                ImageId = i.ImageId,
                ImageUrl = i.ImageUrl,
                IsPrimary = i.IsPrimary
            }).ToList()
        };

        return View(vm);
    }

    // ===================== DELETE ROOM =====================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _roomService.DeleteRoomAsync(id);
        return RedirectToAction(nameof(Index));
    }

    // ===================== UPLOAD IMAGE =====================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImage(int roomId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return RedirectToAction(nameof(Details), new { id = roomId });

        var url = await _cloudinaryService.UploadImageAsync(file);

        await _roomService.AddRoomImagesAsync(roomId, new[] { url });

        return RedirectToAction(nameof(Details), new { id = roomId });
    }

    // ===================== DELETE IMAGE =====================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int imageId, int roomId)
    {
        await _roomService.RemoveRoomImageAsync(imageId);

        return RedirectToAction(nameof(Details), new { id = roomId });
    }
}
