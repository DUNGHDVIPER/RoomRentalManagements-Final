using BLL.DTOs;
using DAL.Data;
using DAL.Entities.Contracts;
using DAL.Entities.Common;
using Microsoft.EntityFrameworkCore;
using BLL.DTOs.Room.Public;

namespace BLL.Services
{
    public class BookingService
    {
        private readonly AppDbContext _context;

        public BookingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CreateBooking(string customerId, BookingCreateDTO dto)
        {
            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.RoomId == dto.RoomId);

            if (room == null)
                throw new Exception("Room not found");

         

            var existed = await _context.Bookings
                .AnyAsync(b =>
                    b.RoomId == dto.RoomId &&
                    b.Status == BookingStatus.Approved);

            if (existed)
                throw new Exception("Room already booked");

            var userBooking = await _context.Bookings
                .AnyAsync(b =>
                    b.CustomerId == customerId &&
                    b.RoomId == dto.RoomId &&
                    b.Status == BookingStatus.Pending);

            if (userBooking)
                throw new Exception("You already booked this room");

            var booking = new Booking
            {
                RoomId = dto.RoomId,
                CustomerId = customerId,
              
                Note = dto.Note,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}