using BLL.DTOs.Utility;
using BLL.Services.Interfaces;
using DAL.Data;
using DAL.Entities.Billing;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services;

public class UtilityService : IUtilityService
{
    private readonly AppDbContext _db;

    public UtilityService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UtilityPriceDto> GetCurrentPriceAsync(CancellationToken ct = default)
    {
        var p = await _db.UtilityPrices
            .AsNoTracking()
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

        if (p == null)
        {
            return new UtilityPriceDto
            {
                EffectiveFrom = DateTime.Today,
                ElectricPerKwh = 0,
                WaterPerM3 = 0
            };
        }

        return new UtilityPriceDto
        {
            EffectiveFrom = p.EffectiveFrom,
            ElectricPerKwh = p.ElectricPerKwh,
            WaterPerM3 = p.WaterPerM3
        };
    }

    public async Task SetPriceAsync(UtilityPriceDto dto, CancellationToken ct = default)
    {
        if (dto.ElectricPerKwh < 0 || dto.WaterPerM3 < 0)
            throw new ArgumentException("Đơn giá không được âm.");

        // lưu lịch sử: tạo record mới
        var entity = new UtilityPrice
        {
            EffectiveFrom = dto.EffectiveFrom,
            ElectricPerKwh = dto.ElectricPerKwh,
            WaterPerM3 = dto.WaterPerM3
        };

        _db.UtilityPrices.Add(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task BulkUpsertReadingsAsync(BulkUtilityReadingDto dto, CancellationToken ct = default)
    {
        var period = dto.Period;

        // validate YYYYMM
        var year = period / 100;
        var month = period % 100;
        if (year < 2000 || month < 1 || month > 12)
            throw new ArgumentException("Period không hợp lệ. Định dạng đúng: YYYYMM (vd: 202602).");

        var roomIds = dto.Items.Select(x => x.RoomId).Distinct().ToList();

        var existing = await _db.UtilityReadings
            .Where(r => r.Period == period && roomIds.Contains(r.RoomId))
            .ToListAsync(ct);

        foreach (var item in dto.Items)
        {
            if (item.ElectricKwh < 0 || item.WaterM3 < 0)
                throw new ArgumentException("Chỉ số điện/nước không được âm.");

            var ex = existing.FirstOrDefault(x => x.RoomId == item.RoomId);
            if (ex == null)
            {
                _db.UtilityReadings.Add(new UtilityReading
                {
                    RoomId = item.RoomId,
                    Period = period,
                    ElectricKwh = item.ElectricKwh,
                    WaterM3 = item.WaterM3
                });
            }
            else
            {
                ex.ElectricKwh = item.ElectricKwh;
                ex.WaterM3 = item.WaterM3;
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<UtilityChargeResultDto> CalculateChargesAsync(int roomId, int period, CancellationToken ct = default)
    {
        var reading = await _db.UtilityReadings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.RoomId == roomId && x.Period == period, ct);

        if (reading == null)
            throw new InvalidOperationException($"Không tìm thấy chỉ số điện/nước của phòng {roomId} cho kỳ {period}.");

        // period YYYYMM -> ngày đầu tháng
        var from = new DateTime(period / 100, period % 100, 1);

        // bảng giá áp dụng cho kỳ
        var price = await _db.UtilityPrices
            .AsNoTracking()
            .Where(x => x.EffectiveFrom <= from)
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

        // fallback nếu không có record <= from
        price ??= await _db.UtilityPrices
            .AsNoTracking()
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

        if (price == null)
            throw new InvalidOperationException("Chưa thiết lập bảng giá điện/nước.");

        var electricAmount = reading.ElectricKwh * price.ElectricPerKwh;
        var waterAmount = reading.WaterM3 * price.WaterPerM3;

        return new UtilityChargeResultDto
        {
            RoomId = roomId,
            Period = period,
            ElectricKwh = reading.ElectricKwh,
            WaterM3 = reading.WaterM3,
            ElectricAmount = electricAmount,
            WaterAmount = waterAmount
            // Total là computed property trong DTO
        };
    }
}