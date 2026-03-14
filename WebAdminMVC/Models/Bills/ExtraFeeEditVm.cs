using System.ComponentModel.DataAnnotations;

namespace WebAdmin.MVC.Models.Billing;

public class ExtraFeeEditVm
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = null!;

    [Range(0, 1_000_000_000)]
    public decimal DefaultAmount { get; set; }

    public bool IsActive { get; set; } = true;
}