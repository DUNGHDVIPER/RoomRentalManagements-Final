using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebHostRazor.Pages.Host.Rooms
{
    public class EditModel : PageModel
    {
        [BindProperty]
        public Room Form { get; set; } = new()
        {
            Name = string.Empty,
            City = string.Empty,
            District = string.Empty,
            Status = "Available"
        };

        public Room? Room { get; private set; }

        public IActionResult OnGet(int id)
        {
            // Simulate fetching the room from a database or service
            Room = GetRoomById(id);

            if (Room == null)
            {
                return NotFound();
            }

            Form = new Room
            {
                Id = Room.Id,
                Name = Room.Name,
                City = Room.City,
                District = Room.District,
                Area = Room.Area,
                Price = Room.Price,
                Status = Room.Status
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Simulate saving the updated room to a database or service
            SaveRoom(Form);

            return RedirectToPage("/Host/Rooms");
        }

        private Room? GetRoomById(int id)
        {
            // Replace with actual data fetching logic
            return new Room
            {
                Id = id,
                Name = "Sample Room",
                City = "Sample City",
                District = "Sample District",
                Area = 50,
                Price = 1000000,
                Status = "Available"
            };
        }

        private void SaveRoom(Room room)
        {
            // Replace with actual data saving logic
        }
    }

    public class Room
    {
        public int Id { get; set; }
        public required string Name { get; set; } = string.Empty;
        public required string City { get; set; } = string.Empty;
        public required string District { get; set; } = string.Empty;
        public double Area { get; set; }
        public decimal Price { get; set; }
        public required string Status { get; set; } = string.Empty;
    }
}