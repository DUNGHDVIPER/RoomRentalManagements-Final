using System.ComponentModel.DataAnnotations;
using DAL.Entities.Common;

namespace BLL.DTOs.Notification;

public class BroadcastNotificationDto
{
    [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập nội dung")]
    public string Content { get; set; } = null!;

    public List<int> ContractIds { get; set; } = new();

    public int? BlockId { get; set; }
    public int? FloorId { get; set; }
    public bool SendToHost { get; set; } = false;
    public SourceType SourceType { get; set; } = SourceType.Manual;
}