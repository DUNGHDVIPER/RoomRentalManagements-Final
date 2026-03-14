namespace BLL.DTOs.Utility;

public class BulkUtilityReadingDto
{
    public int Period { get; set; } // YYYYMM
    public List<RoomReadingItem> Items { get; set; } = new();

    public class RoomReadingItem
    {
        public int RoomId { get; set; }
        public decimal ElectricKwh { get; set; }
        public decimal WaterM3 { get; set; }
    }
}
