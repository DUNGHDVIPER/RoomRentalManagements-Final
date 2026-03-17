namespace BLL.Dtos;

public class RoomDto
{


    public int Id { get; set; }// doi Guid thanh int de cho giong voi DAL
    public string RoomNo { get; set; } = null!;

    public string? Name { get; set; }
    public decimal Price { get; set; }
    public required string Status { get; set; }
}
