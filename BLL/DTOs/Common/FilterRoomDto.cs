namespace BLL.Common;

public class FilterRoomDto
{
    public string? Keyword { get; set; }
    public string? City { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
}
