using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebAdmin.MVC.Models.Contracts;

public class UploadAttachmentVm
{
    [Required]
    public long ContractId { get; set; }

    public List<AttachmentRowVm> Attachments { get; set; } = new();
}

public class AttachmentRowVm
{
    public string FileName { get; set; } = "";
    public string Url { get; set; } = "";
    public string? ContentType { get; set; }
    public long Size { get; set; }
    public DateTime UploadedAt { get; set; }
}