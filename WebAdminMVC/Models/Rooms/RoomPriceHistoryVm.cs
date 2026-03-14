namespace WebAdmin.MVC.Models.Rooms;

public class RoomPriceHistoryVm
{
    public decimal OldPrice { get; set; }

    public decimal NewPrice { get; set; }

    public DateTime ChangedAt { get; set; }

    public string? Note { get; set; }
}