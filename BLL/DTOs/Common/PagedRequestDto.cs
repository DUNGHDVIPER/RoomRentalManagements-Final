namespace BLL.DTOs.Common;

public class PagedRequestDto
{
    public PagedRequestDto() { }

    public PagedRequestDto(int pageNumber, int pageSize)
    {
        PageNumber = pageNumber < 1 ? 1 : pageNumber;
        PageSize = pageSize < 1 ? 12 : pageSize;
    }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 12;

    public string? SortBy { get; set; }
    public string? Keyword { get; set; }   // ✅ set được từ MVC
}