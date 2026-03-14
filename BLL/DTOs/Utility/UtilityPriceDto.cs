namespace BLL.DTOs.Utility;

public class UtilityPriceDto
{
    public DateTime EffectiveFrom { get; set; }
    public decimal ElectricPerKwh { get; set; }
    public decimal WaterPerM3 { get; set; }
}
