namespace BLL.DTOs.Billing;

public class GenerateBillsRequestDto
{
    public int Period { get; set; } // YYYYMM
    public DateTime DueDate { get; set; }

    // Optional: chỉ generate cho 1 block/floor
    public int? BlockId { get; set; }
    public int? FloorId { get; set; }
}
