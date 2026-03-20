using BLL.DTOs.Tenant;
using BLL.DTOs.Common;
using BLL.Common;
using BLL.Services.Interfaces;
using DAL.Data;
using DAL.Repositories;
using DAL.Entities.Tenanting;
using Microsoft.EntityFrameworkCore;
using static BLL.Common.Exceptions;
using DAL.Entities.Common;

namespace BLL.Services;

public class TenantService : ITenantService
{
    private readonly ITenantRepository _tenantRepo;
    private readonly AppDbContext _context;

    public TenantService(ITenantRepository tenantRepo, AppDbContext context)
    {
        _tenantRepo = tenantRepo;
        _context = context;
    }

    public async Task<PagedResultDto<TenantDto>> GetTenantsAsync(PagedRequestDto req, CancellationToken ct = default)
    {
        var query = _context.Tenants.Include(t => t.StayHistories).AsQueryable();

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
                Status = x.Status.ToString(),

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

    public async Task<TenantDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (tenant == null) throw new Exception("Tenant not found");

        return new TenantDto
        {
            Id = tenant.Id,
            FullName = tenant.FullName,
            Phone = tenant.Phone,
            Email = tenant.Email,
            Status = tenant.Status.ToString()
        };
    }

    public async Task<TenantDto> CreateAsync(CreateTenantDto dto, CancellationToken ct = default)
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
            Email = tenant.Email,
            Status = tenant.Status.ToString()
        };
    }

    public async Task<TenantDto> UpdateAsync(int id, UpdateTenantDto dto, CancellationToken ct = default)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (tenant == null) throw new Exception("Tenant not found");

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
            Email = tenant.Email,
            Status = tenant.Status.ToString()
        };
    }

    public async Task DeleteAsync(int id)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == id);
        if (tenant == null) throw new NotFoundException("Tenant not found");

        var hasContract = await _context.Contracts.AnyAsync(c => c.TenantId == id && c.Status == "Active");
        if (hasContract) throw new Exception("Tenant has an active contract and cannot be deleted.");

        _context.Tenants.Remove(tenant);
        await _context.SaveChangesAsync();
    }

    public async Task BlacklistAsync(int tenantId, string reason, CancellationToken ct = default)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId, ct);
        if (tenant == null) throw new Exception("Tenant not found");

        if (tenant.Status == TenantStatus.Blacklisted)
            throw new Exception("Tenant already blacklisted");

        tenant.Status = TenantStatus.Blacklisted;
        await _context.SaveChangesAsync(ct);
    }

    public async Task UnBlacklistAsync(int tenantId, CancellationToken ct = default)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId, ct);
        if (tenant == null) throw new Exception("Tenant not found");

        tenant.Status = TenantStatus.Active;
        await _context.SaveChangesAsync(ct);
    }

    public async Task<Tenant?> GetByEmailAsync(string email)
    {
        return await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Email == email);
    }
}