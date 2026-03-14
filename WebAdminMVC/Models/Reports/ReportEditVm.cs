using System.ComponentModel.DataAnnotations;

namespace WebAdmin.MVC.Models.Reports;

public class ReportEditVm
{
    public int Id { get; set; }
    [Required] public string Name { get; set; } = null!;
    [Required] public string Period { get; set; } = null!;
    [Required] public string Type { get; set; } = null!;
}
