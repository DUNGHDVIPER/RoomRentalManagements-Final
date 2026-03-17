using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BLL.Services.Interfaces;
using DAL.Data;
using DAL.Entities.Common;
using DAL.Entities.Tenanting;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services
{
    public class StayHistoryService : IStayHistoryService
    {
        private readonly AppDbContext _context;

        public StayHistoryService(AppDbContext context)
        {
            _context = context;
        }

        // ===============================
        // CHECK-IN
        // ===============================
        public async Task CheckInAsync(
            int roomId,
            int tenantId,
            string? note = null,
            CancellationToken ct = default)
        {
            // 1️⃣ Validate Room tồn tại
            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.Id == roomId, ct);

            if (room == null)
                throw new Exception("Room not found.");

            // 2️⃣ Room phải AVAILABLE
            if (room.Status != RoomStatus.Available)
                throw new Exception("Room is not available.");

            // 3️⃣ Validate Tenant tồn tại
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

            if (tenant == null)
                throw new Exception("Tenant not found.");

            // 4️⃣ Không cho tenant ở phòng khác
            var isTenantStaying = await _context.StayHistories
                .AnyAsync(s =>
                    s.TenantId == tenantId &&
                    s.CheckOutAt == null,
                    ct);

            if (isTenantStaying)
                throw new Exception("Tenant is already staying in another room.");

            // 5️⃣ Double protection: Room không có active stay
            var roomHasActiveStay = await _context.StayHistories
                .AnyAsync(s =>
                    s.RoomId == roomId &&
                    s.CheckOutAt == null,
                    ct);

            if (roomHasActiveStay)
                throw new Exception("This room already has an active tenant.");

            // 6️⃣ Validate Contract còn hiệu lực
            var validContract = await _context.Contracts
                .AnyAsync(c =>
                    c.RoomId == roomId &&
                    c.TenantId == tenantId &&
                    c.StartDate <= DateTime.Now &&
                    c.EndDate >= DateTime.Now,
                    ct);

            if (!validContract)
                throw new Exception("No valid contract found for this tenant and room.");

            // 7️⃣ Validate Note length (business layer protection)
            if (!string.IsNullOrWhiteSpace(note) && note.Length > 250)
                throw new Exception("Note cannot exceed 250 characters.");

            // 8️⃣ Tạo StayHistory
            var stay = new StayHistory
            {
                RoomId = roomId,
                TenantId = tenantId,
                CheckInAt = DateTime.Now,
                Note = note
            };

            await _context.StayHistories.AddAsync(stay, ct);

            // 9️⃣ Update room status
            room.Status = RoomStatus.Occupied;

            await _context.SaveChangesAsync(ct);
        }

        // ===============================
        // CHECK-OUT
        // ===============================
        public async Task CheckOutAsync(
            int roomId,
            CancellationToken ct = default)
        {
            // 1️⃣ Validate room tồn tại
            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.Id == roomId, ct);

            if (room == null)
                throw new Exception("Room not found.");

            // 2️⃣ Room phải đang OCCUPIED
            if (room.Status != RoomStatus.Occupied)
                throw new Exception("Room is not occupied.");

            // 3️⃣ Tìm active stay
            var stay = await _context.StayHistories
                .Where(s => s.RoomId == roomId && s.CheckOutAt == null)
                .OrderByDescending(s => s.CheckInAt)
                .FirstOrDefaultAsync(ct);

            if (stay == null)
                throw new Exception("No active stay found.");

            // 4️⃣ Double checkout protection
            if (stay.CheckOutAt != null)
                throw new Exception("Tenant already checked out.");

            // 5️⃣ Set checkout time
            stay.CheckOutAt = DateTime.Now;

            // 6️⃣ Update room status
            room.Status = RoomStatus.Available;

            await _context.SaveChangesAsync(ct);
        }

        // ===============================
        // GET ROOM HISTORY
        // ===============================
        public async Task<List<StayHistory>> GetRoomHistoryAsync(
            int roomId,
            CancellationToken ct = default)
        {
            // Validate room tồn tại
            var roomExists = await _context.Rooms
                .AnyAsync(r => r.Id == roomId, ct);

            if (!roomExists)
                throw new Exception("Room not found.");

            return await _context.StayHistories
                .Where(s => s.RoomId == roomId)
                .Include(s => s.Tenant)
                .OrderByDescending(s => s.CheckInAt)
                .ToListAsync(ct);
        }
    }
}