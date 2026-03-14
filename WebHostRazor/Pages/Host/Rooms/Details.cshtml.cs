using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace WebHostRazor.Pages.Host.Rooms
{
    public class DetailsModel : PageModel
    {
        public RoomModel? Room { get; set; }

        public void OnGet(int id)
        {
            // Example data fetching logic
            Room = GetRoomById(id);
        }

        private RoomModel GetRoomById(int id)
        {
            // Replace this with actual data fetching logic
            return new RoomModel
            {
                Id = id,
                Name = "Sample Room",
                District = "Sample District",
                City = "Sample City",
                Area = 50,
                Price = 1000000,
                Status = "Available",
                Images = new List<string>
                {
                    "/images/sample1.jpg",
                    "/images/sample2.jpg",
                    "/images/sample3.jpg"
                }
            };
        }
    }

    public class RoomModel
    {
        public int Id { get; set; }
        public required string Name { get; set; } = string.Empty;
        public required string District { get; set; } = string.Empty;
        public required string City { get; set; } = string.Empty;
        public double Area { get; set; }
        public decimal Price { get; set; }
        public required string Status { get; set; } = string.Empty;
        public List<string> Images { get; set; } = new();
    }
}