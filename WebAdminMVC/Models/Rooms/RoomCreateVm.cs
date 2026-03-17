using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebAdmin.MVC.Models.Rooms;

public class RoomCreateVm
{
    public string RoomCode { get; set; }
    public string? RoomName { get; set; }

    public int? FloorId { get; set; }

    public decimal AreaM2 { get; set; }

    public int MaxOccupants { get; set; }

    public decimal Price { get; set; }

    public string Status { get; set; }

    public string? Description { get; set; }

    public List<int>? AmenityIds { get; set; }

    public List<SelectListItem> Amenities { get; set; } = new();

    public List<SelectListItem> Floors { get; set; } = new();

    // 👇 thêm dòng này
    public List<IFormFile>? Images { get; set; }
}
