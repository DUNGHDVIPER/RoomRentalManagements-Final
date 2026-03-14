using System.ComponentModel.DataAnnotations;

namespace WebAdmin.MVC.Models.Reports;

public class ReportCreateVm
{
    [Required] public string Name { get; set; } = "Monthly Revenue";
    [Required] public string Period { get; set; } = DateTime.Today.ToString("yyyy-MM");
    [Required] public string Type { get; set; } = "Revenue";
}
