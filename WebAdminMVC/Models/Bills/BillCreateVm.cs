using System.ComponentModel.DataAnnotations;

namespace WebAdmin.MVC.Models.Billing;

public class BillCreateVm
{
    [Required] public string RoomName { get; set; } = "Room 101";
    [Required] public string Month { get; set; } = DateTime.Today.ToString("yyyy-MM");
    [Range(0, 999999999)] public decimal Total { get; set; }
    [Required] public string Status { get; set; } = "Unpaid";
}
