namespace BLL.DTOs.Room;

public class RoomPriceHistoryDto
{
    public decimal OldPrice { get; set; }

    public decimal NewPrice { get; set; }

    public DateTime ChangedAt { get; set; }

    public int? ChangedByUserId { get; set; }

    public string? Note { get; set; }
}