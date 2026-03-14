using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace WebHostRazor.Pages.Host.Rooms.Images
{
    public class IndexModel : PageModel
    {
        // Tạo simple model cho Images page
        public RoomImageViewModel Room { get; set; } = new()
        {
            Name = string.Empty,
            Images = new List<string>()
        };

        public void OnGet(int id)
        {
            Room = GetRoomById(id);
        }

        public IActionResult OnPost(int id, string url)
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                var room = GetRoomById(id);
                room.Images.Add(url);
                Room = room;
                // TODO: Save to database
            }

            return Page();
        }

        public IActionResult OnPostRemove(int id, int index)
        {
            var room = GetRoomById(id);
            if (index >= 0 && index < room.Images.Count)
            {
                room.Images.RemoveAt(index);
                Room = room;
                // TODO: Save to database
            }

            return Page();
        }

        private RoomImageViewModel GetRoomById(int id)
        {
            // TODO: Replace with actual database query
            return new RoomImageViewModel
            {
                Id = id,
                Name = "Sample Room",
                Images = new List<string>
                {
                    "https://via.placeholder.com/300x200/0066cc/ffffff?text=Room+Image+1",
                    "https://via.placeholder.com/300x200/cc6600/ffffff?text=Room+Image+2",
                    "https://via.placeholder.com/300x200/006600/ffffff?text=Room+Image+3"
                }
            };
        }
    }

    public class RoomImageViewModel
    {
        public int Id { get; set; }
        public required string Name { get; set; } = string.Empty;
        public List<string> Images { get; set; } = new List<string>();
    }
}