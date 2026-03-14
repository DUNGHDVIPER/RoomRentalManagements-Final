using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace WebHostRazor.Pages.Host.Rooms
{
    public class CreateModel : PageModel
    {
        [BindProperty]
        public RoomFormModel Form { get; set; } = new()
        {
            Name = string.Empty,
            City = string.Empty,
            District = string.Empty,
            Status = "Available"
        };

        public void OnGet()
        {
            // This method is intentionally left blank for the initial page load.
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // TODO: Handle form submission logic here (e.g., save to database)

            return RedirectToPage("/Host/Rooms");
        }
    }

    public class RoomFormModel
    {
        [Required]
        [StringLength(100)]
        public required string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public required string City { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public required string District { get; set; } = string.Empty;

        [Range(1, 10000)]
        public double Area { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        public required string Status { get; set; } = "Available";
    }
}