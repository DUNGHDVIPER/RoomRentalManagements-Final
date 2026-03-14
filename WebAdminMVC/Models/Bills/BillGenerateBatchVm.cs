using System.ComponentModel.DataAnnotations;

namespace WebAdmin.MVC.Models.Billing;

public class BillGenerateBatchVm
{
    [Required]
    public string Month { get; set; } = DateTime.Today.ToString("yyyy-MM");

    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(7);

    public bool IncludeRent { get; set; } = true;
    public bool IncludeUtilities { get; set; } = true;

    public List<int> ExtraFeeIds { get; set; } = new();
}