using System.Collections.Generic;
using BLL.DTOs.Contract; // chỉnh namespace nếu ContractDto ở namespace khác

namespace WebAdmin.MVC.Models.Contracts;

public class ContractDetailsVm
{
    public ContractDto Contract { get; set; } = default!;
    public List<AttachmentRowVm> Attachments { get; set; } = new();
}