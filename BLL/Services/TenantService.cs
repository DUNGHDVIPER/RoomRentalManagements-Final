using BLL.DTOs.Tenant;
using BLL.DTOs.Common;
using BLL.Common;
using BLL.Services.Interfaces;
using DAL.Data;
using DAL.Repositories;
using DAL.Entities.Tenanting;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services;

public class TenantService : ITenantService
{
    private readonly ITenantRepository _tenantRepo;
    private readonly AppDbContext _context;

    public TenantService(
        ITenantRepository tenantRepo,
        AppDbContext context)
    {
        _tenantRepo = tenantRepo;
        _context = context;
    }

    // ===================== GET LIST (FIXED) =====================
    public async Task<PagedResultDto<TenantDto>> GetTenantsAsync(
     PagedRequestDto req, CancellationToken ct = default)
    {
        var query = _context.Tenants
            .Include(t => t.StayHistories)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Keyword))
        {
            query = query.Where(x =>
                x.FullName.Contains(req.Keyword) ||
                x.Phone!.Contains(req.Keyword) ||
                x.CCCD!.Contains(req.Keyword));
        }

        var total = await query.CountAsync(ct);

        var data = await query
            .OrderByDescending(x => x.Id)
            .Skip((req.PageNumber - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(x => new TenantDto
            {
                Id = x.Id,
                FullName = x.FullName,
                Phone = x.Phone,
                Email = x.Email,
                CCCD = x.CCCD,
                Gender = x.Gender,
                DateOfBirth = x.DateOfBirth,
                Address = x.Address,
                Status = x.Status,

                CheckInDate = x.StayHistories
                    .OrderByDescending(s => s.CheckInAt)
                    .Select(s => s.CheckInAt)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        return new PagedResultDto<TenantDto>
        {
            TotalCount = total,
            Items = data
        };
    }

    // ===================== GET BY ID =====================
    public async Task<TenantDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (tenant == null)
            throw new Exception("Tenant not found");

        return new TenantDto
        {
            Id = tenant.Id,
            FullName = tenant.FullName,
            Phone = tenant.Phone,
            Email = tenant.Email
        };
    }

    // ===================== CREATE =====================
    public async Task<TenantDto> CreateAsync(
        CreateTenantDto dto,
        CancellationToken ct = default)
    {
        var tenant = new Tenant
        {
            FullName = dto.FullName,
            DateOfBirth = dto.DateOfBirth,
            Phone = dto.Phone,
            Email = dto.Email
        };

        await _tenantRepo.AddAsync(tenant);
        await _context.SaveChangesAsync(ct);

        return new TenantDto
        {
            Id = tenant.Id,
            FullName = tenant.FullName,
            Phone = tenant.Phone,
            Email = tenant.Email
        };
    }

    // ===================== UPDATE =====================
    public async Task<TenantDto> UpdateAsync(
        int id,
        UpdateTenantDto dto,
        CancellationToken ct = default)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (tenant == null)
            throw new Exception("Tenant not found");

        tenant.FullName = dto.FullName;
        tenant.Phone = dto.Phone;
        tenant.Email = dto.Email;
        tenant.DateOfBirth = dto.DateOfBirth;

        await _context.SaveChangesAsync(ct);

        return new TenantDto
        {
            Id = tenant.Id,
            FullName = tenant.FullName,
            Phone = tenant.Phone,
            Email = tenant.Email
        };
    }

    // ===================== DELETE =====================
    // ===================== DELETE =====================
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (tenant == null)
            throw new Exception("Tenant not found");

        _context.Tenants.Remove(tenant);
        await _context.SaveChangesAsync(ct);
    }
    // ===================== ID DOCS =====================
    public async Task<List<TenantIdDocDto>> GetIdDocsAsync(
        int tenantId,
        CancellationToken ct = default)
    {
        return await _context.TenantIdDocs
            .Where(x => x.TenantId == tenantId)
            .Select(x => new TenantIdDocDto
            {
                Id = x.Id,
                DocNumber = x.DocNumber,
                ImageUrl = x.ImageUrl,
                IssuedAt = x.IssuedAt,
                ExpiredAt = x.ExpiredAt
            })
            .ToListAsync(ct);
    }

    public async Task<TenantIdDocDto> AddIdDocAsync(
        int tenantId,
        TenantIdDocDto dto,
        CancellationToken ct = default)
    {
        var tenant = await _context.Tenants.FindAsync(new object[] { tenantId }, ct);

        if (tenant == null)
            throw new Exception("Tenant not found");

        var doc = new TenantIdDoc
        {
            TenantId = tenantId,
            DocType = "CCCD",
            DocNumber = dto.DocNumber,
            ImageUrl = dto.ImageUrl,
            IssuedAt = dto.IssuedAt,
            ExpiredAt = dto.ExpiredAt
        };

        _context.TenantIdDocs.Add(doc);
        await _context.SaveChangesAsync(ct);

        dto.Id = doc.Id;
        return dto;
    }

    public async Task RemoveIdDocAsync(int docId, CancellationToken ct = default)
    {
        var doc = await _context.TenantIdDocs.FindAsync(new object[] { docId }, ct);

        if (doc == null)
            throw new Exception("Document not found");

        _context.TenantIdDocs.Remove(doc);
        await _context.SaveChangesAsync(ct);
    }

    // ===================== BLACKLIST =====================
    //public async Task BlacklistAsync(
    //    int tenantId,
    //    string reason,
    //    CancellationToken ct = default)
    //{
    //    var tenant = await _context.Tenants.FindAsync(new object[] { tenantId }, ct);

    //    if (tenant == null)
    //        throw new Exception("Tenant not found");

    //    tenant. = true;
    //    tenant.BlacklistReason = reason;

    //    await _context.SaveChangesAsync(ct);
    //}

    //public async Task UnBlacklistAsync(
    //    int tenantId,
    //    CancellationToken ct = default)
    //{
    //    var tenant = await _context.Tenants.FindAsync(new object[] { tenantId }, ct);

    //    if (tenant == null)
    //        throw new Exception("Tenant not found");

    //    tenant.IsBlacklisted = false;
    //    tenant.BlacklistReason = null;

    //    await _context.SaveChangesAsync(ct);
    

    // ===================== STAY HISTORY =====================
    public Task<List<StayHistoryDto>> GetStayHistoryAsync(
        int tenantId,
        CancellationToken ct = default)
    {
        // TODO: implement later
        return Task.FromResult(new List<StayHistoryDto>());
    }

    public Task BlacklistAsync(int tenantId, string reason, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task UnBlacklistAsync(int tenantId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
