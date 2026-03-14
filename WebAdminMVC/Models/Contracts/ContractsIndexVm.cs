using System;
using System.Collections.Generic;

namespace WebAdmin.MVC.Models.Contracts;

public class ContractsIndexVm
{
    public string? Query { get; set; }
    public int? Status { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public int Total { get; set; }
    public int TotalPages => (int)Math.Ceiling(Total / (double)PageSize);

    public List<ContractRowVm> Items { get; set; } = new();
    public int? RoomId { get; set; }
    public int? TenantId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

public class ContractRowVm
{
    public long ContractId { get; set; }

    public string? ContractCode { get; set; }
    public int RoomId { get; set; }
    public int TenantId { get; set; }
    public int? Status { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public decimal Rent { get; set; }      // ✅ thêm
    public decimal Deposit { get; set; }   // ✅ thêm

    // nếu Status DB là string thì giữ string; nếu là int thì đổi sang int
    /*public string Status { get; set; } = "";*/
    public bool IsActive { get; set; }
    public string? BlockName { get; set; }
    public string? FloorName { get; set; }
    public string? RoomNo { get; set; }
}