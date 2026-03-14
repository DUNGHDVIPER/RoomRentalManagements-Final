using System.ComponentModel.DataAnnotations;

namespace WebAdmin.MVC.Models.Contracts;

public class ContractEditVm
{
    public int Id { get; set; }
    [Required] public string RoomName { get; set; } = null!;
    [Required] public string TenantName { get; set; } = null!;
    [Required] public DateTime StartDate { get; set; }
    [Required] public DateTime EndDate { get; set; }
    [Required] public string Status { get; set; } = "Active";
    [Range(0, 999999999)] public decimal Deposit { get; set; }
}
