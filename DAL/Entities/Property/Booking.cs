using DAL.Entities.Common;
using DAL.Entities.Property;

namespace DAL.Entities.Contracts
{
    public class Booking
    {
        public int BookingId { get; set; }

        public int RoomId { get; set; }

        public string CustomerId { get; set; } = null!;

        public DateTime CheckInDate { get; set; }

        public string? Note { get; set; }

        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Room Room { get; set; } = null!;
    }
}