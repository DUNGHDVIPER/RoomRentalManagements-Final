using DAL.Data;
using DAL.Entities.Tenanting;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly AppDbContext _context;
    public TenantRepository(AppDbContext context) => _context = context;

    public Task<List<Tenant>> GetAllAsync() => _context.Tenants.ToListAsync();
    public Task<Tenant?> GetByIdAsync(long id) => _context.Tenants.FirstOrDefaultAsync(t => t.Id == id);
    public Task AddAsync(Tenant tenant) => _context.Tenants.AddAsync(tenant).AsTask();
    public Task UpdateAsync(Tenant tenant) { _context.Tenants.Update(tenant); return Task.CompletedTask; }
    public async Task DeleteAsync(long id)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == id);
        if (tenant != null) _context.Tenants.Remove(tenant);
    }
}