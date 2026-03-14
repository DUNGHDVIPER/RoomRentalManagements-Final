using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;

namespace WebHostRazor.Pages.Host.Rooms.PricingHistory
{
    public class IndexModel : PageModel
    {
        public RoomModel? Room { get; set; }

        public void OnGet(int id)
        {
            // Example data for demonstration purposes
            Room = GetRoomById(id);
        }

        private RoomModel GetRoomById(int id)
        {
            // Replace this with actual data retrieval logic
            return new RoomModel
            {
                Id = id,
                Name = "Sample Room",
                PricingHistory = new List<PricingHistoryModel>
                {
                    new PricingHistoryModel { Date = DateTime.Now.AddDays(-10), Price = 100000 },
                    new PricingHistoryModel { Date = DateTime.Now.AddDays(-5), Price = 120000 }
                }
            };
        }
    }

    public class RoomModel
    {
        public int Id { get; set; }
        public required string Name { get; set; } = string.Empty;
        public required List<PricingHistoryModel> PricingHistory { get; set; } = new();
    }

    public class PricingHistoryModel
    {
        public DateTime Date { get; set; }
        public decimal Price { get; set; }
    }
}
