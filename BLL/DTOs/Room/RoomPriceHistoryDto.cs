namespace BLL.DTOs.Room;

public class RoomPriceHistoryDto
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public decimal Price { get; set; }
}
