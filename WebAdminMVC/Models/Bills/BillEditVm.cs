using System.ComponentModel.DataAnnotations;

namespace WebAdmin.MVC.Models.Billing;

public class BillEditVm
{
    public int Id { get; set; }
    [Required] public string RoomName { get; set; } = null!;
    [Required] public string Month { get; set; } = null!;
    [Range(0, 999999999)] public decimal Total { get; set; }
    [Required] public string Status { get; set; } = "Unpaid";
}
