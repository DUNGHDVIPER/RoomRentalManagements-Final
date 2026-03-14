using System;
using System.ComponentModel.DataAnnotations;

namespace WebAdmin.MVC.Models.Contracts;

public class ContractCreateVm
{
    [Required]
    public int RoomId { get; set; }

    [Required]
    public int TenantId { get; set; }

    [Required, DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required, DataType(DataType.Date)]
    public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(6);

    [Required, Range(0, double.MaxValue)]
    public decimal Rent { get; set; }

    [Required, Range(0, double.MaxValue)]
    public decimal Deposit { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
    public bool ActivateNow { get; internal set; }
}