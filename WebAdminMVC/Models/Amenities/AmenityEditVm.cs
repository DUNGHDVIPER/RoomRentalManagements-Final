using System.ComponentModel.DataAnnotations;

namespace WebAdmin.MVC.Models.Amenities;

public class AmenityEditVm
{
    public int AmenityId { get; set; }

    [Required]
    [StringLength(100)]
    public string AmenityName { get; set; } = string.Empty;
}