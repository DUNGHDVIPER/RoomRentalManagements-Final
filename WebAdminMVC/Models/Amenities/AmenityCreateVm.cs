using System.ComponentModel.DataAnnotations;

namespace WebAdmin.MVC.Models.Amenities;

public class AmenityCreateVm
{
    [Required]
    [StringLength(100)]
    public string AmenityName { get; set; } = string.Empty;
}