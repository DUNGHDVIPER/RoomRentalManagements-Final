using System;
using System.Collections.Generic;

namespace WebAdmin.MVC.Models.Contracts;

public class ContractVersionsVm
{
    public int ContractId { get; set; }
    public List<ContractVersionVm> Versions { get; set; } = new();
}

public class ContractVersionVm
{
    public long VersionId { get; set; }
    public int VersionNumber { get; set; }
    public DateTime ChangedAt { get; set; }
    public int? ChangedByUserId { get; set; }
    public string? ChangeNote { get; set; }
    public string SnapshotJson { get; set; } = "";
}