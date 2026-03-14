
namespace BLL.Services
{
    internal class ContractAttachmentDto
    {
        public long AttachmentId { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}