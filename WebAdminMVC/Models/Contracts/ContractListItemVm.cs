namespace WebAdmin.MVC.Models.Contracts;

public class ContractListItemVm
{
    public int Id { get; set; }
    public string RoomName { get; set; } = null!;
    public string TenantName { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "Active"; // Draft/Active/Terminated/Expired
}
