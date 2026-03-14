using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace WebHostRazor.Pages.Host.Rooms.Amenities
{
    public class IndexModel : PageModel
    {
        // Add missing Room property that the Razor view expects
        public RoomAmenityViewModel? Room { get; set; }

        [BindProperty]
        public RoomFormModel Form { get; set; } = new()
        {
            Name = string.Empty,
            City = string.Empty,
            District = string.Empty,
            Status = "Available"
        };

        public void OnGet(int id)
        {
            // Load room data
            Room = GetRoomById(id);
        }

        public IActionResult OnPost(int id, string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                var room = GetRoomById(id);
                if (room != null && !room.Amenities.Contains(name))
                {
                    room.Amenities.Add(name);
                    Room = room;
                    // TODO: Save to database
                }
            }
            return Page();
        }

        public IActionResult OnPostRemove(int id, string name)
        {
            var room = GetRoomById(id);
            if (room != null && room.Amenities.Contains(name))
            {
                room.Amenities.Remove(name);
                Room = room;
                // TODO: Save to database
            }
            return Page();
        }

        private RoomAmenityViewModel? GetRoomById(int id)
        {
            // TODO: Replace with actual database query
            return new RoomAmenityViewModel
            {
                Id = id,
                Name = "Sample Room",
                Amenities = new List<string> { "WiFi", "Air Conditioning", "TV", "Parking" }
            };
        }
    }

    public class RoomAmenityViewModel
    {
        public int Id { get; set; }
        public required string Name { get; set; } = string.Empty;
        public List<string> Amenities { get; set; } = new List<string>();
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