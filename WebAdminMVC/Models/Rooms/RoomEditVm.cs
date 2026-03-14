using System.ComponentModel.DataAnnotations;
using DAL.Entities.Common;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebAdmin.MVC.Models.Rooms;

public class RoomEditVm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Room code is required")]
    [Display(Name = "Room Code")]
    public string RoomCode { get; set; } = null!;

    [Display(Name = "Room Name")]
    public string? RoomName { get; set; }

    [Range(0, 10000, ErrorMessage = "Area must be between 0 and 10000 m²")]
    [Display(Name = "Area (m²)")]
    public decimal? AreaM2 { get; set; }

    [Range(1, 20, ErrorMessage = "Max occupants must be between 1 and 20")]
    [Display(Name = "Max Occupants")]
    public int MaxOccupants { get; set; }

    [Range(0, 999999999, ErrorMessage = "Price must be >= 0")]
    [Display(Name = "Base Price")]
    public decimal CurrentBasePrice { get; set; }

    [Required]
    public RoomStatus Status { get; set; } = RoomStatus.Available;

    public string? Description { get; set; }

    // ===== AMENITIES =====
    public List<int> AmenityIds { get; set; } = new();

    public List<SelectListItem> Amenities { get; set; } = new();
}