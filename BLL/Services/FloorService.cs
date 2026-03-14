using BLL.DTOs.Property;
using BLL.Services.Interfaces;
using DAL.Data;
using DAL.Entities.Property;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services
{
    public class FloorService : IFloorService
    {
        private readonly AppDbContext _context;

        public FloorService(AppDbContext context)
        {
            _context = context;
        }

        // ================= GET FLOORS BY BLOCK =================
        public async Task<List<FloorListVm>> GetByBlockAsync(int blockId)
        {
            return await _context.Floors
                .AsNoTracking()
                .Where(x => x.BlockId == blockId)
                .Select(x => new FloorListVm
                {
                    Id = x.Id,
                    FloorName = x.FloorName,
                    TotalRooms = x.Rooms.Count()
                })
                .ToListAsync();
        }

        // ================= GET FLOOR BY ID =================
        public async Task<FloorDto?> GetByIdAsync(int id)
        {
            var floor = await _context.Floors
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (floor == null)
                return null;

            return new FloorDto
            {
                Id = floor.Id,
                BlockId = floor.BlockId,
                FloorName = floor.FloorName
            };
        }

        // ================= CREATE FLOOR =================
        public async Task CreateAsync(FloorDto dto)
        {
            var floor = new Floor
            {
                BlockId = dto.BlockId,
                FloorName = dto.FloorName
            };

            await _context.Floors.AddAsync(floor);
            await _context.SaveChangesAsync();
        }

        // ================= UPDATE FLOOR =================
        public async Task UpdateAsync(FloorDto dto)
        {
            var floor = await _context.Floors
                .FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (floor == null)
                return;

            floor.FloorName = dto.FloorName;

            await _context.SaveChangesAsync();
        }

        // ================= DELETE FLOOR =================
        public async Task DeleteAsync(int id)
        {
            var floor = await _context.Floors
                .Include(x => x.Rooms)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (floor == null)
                return;

            if (floor.Rooms.Any())
                throw new Exception("Cannot delete floor because it contains rooms.");

            _context.Floors.Remove(floor);
            await _context.SaveChangesAsync();
        }
    }
}