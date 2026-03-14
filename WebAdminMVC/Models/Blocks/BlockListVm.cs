namespace WebAdminMVC.Models.Blocks;

public class BlockListVm
{
    public int BlockId { get; set; }

    public string BlockName { get; set; } = null!;

    public string? Address { get; set; }

    public string? Note { get; set; }

    public string Status { get; set; } = "Active";

    public int TotalFloors { get; set; }

    public int TotalRooms { get; set; }
}