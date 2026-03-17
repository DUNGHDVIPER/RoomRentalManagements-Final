using System;
using System.ComponentModel.DataAnnotations;

namespace WebAdmin.MVC.Models.Contracts;

public class RenewVm
{
    [Required]
    public int ContractId { get; set; }   // ✅ int

    [Required, DataType(DataType.Date)]
    public DateTime NewStartDate { get; set; }

    [Required, DataType(DataType.Date)]
    public DateTime NewEndDate { get; set; }

    [Required, Range(0, double.MaxValue)]
    public decimal NewRent { get; set; }

    [Required, Range(0, double.MaxValue)]
    public decimal NewDeposit { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }

    // Header fields (display-only)
    public int CurrentStatus { get; set; } // ✅ int
    public DateTime CurrentStartDate { get; set; }
    public DateTime CurrentEndDate { get; set; }
    public decimal CurrentRent { get; set; }
    public decimal CurrentDeposit { get; set; }
}
public class TerminateVm
{
    [Required]
    public int ContractId { get; set; }   // ✅ int

    [Required, DataType(DataType.Date)]
    public DateTime TerminateDate { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }

    // Header fields
    public int CurrentStatus { get; set; } // ✅ int
    public DateTime CurrentStartDate { get; set; }
    public DateTime CurrentEndDate { get; set; }
    public decimal CurrentRent { get; set; }
    public decimal CurrentDeposit { get; set; }
}