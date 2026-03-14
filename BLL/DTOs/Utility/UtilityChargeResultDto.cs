namespace BLL.DTOs.Utility;

public class UtilityChargeResultDto
{
    public int RoomId { get; set; }
    public int Period { get; set; }

    public decimal ElectricKwh { get; set; }
    public decimal WaterM3 { get; set; }

    public decimal ElectricAmount { get; set; }
    public decimal WaterAmount { get; set; }

    public decimal Total => ElectricAmount + WaterAmount;
}
