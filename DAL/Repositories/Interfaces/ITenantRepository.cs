using DAL.Entities.Tenanting;

namespace DAL.Repositories
{
    public interface ITenantRepository
    {
        Task<List<Tenant>> GetAllAsync();
        Task<Tenant?> GetByIdAsync(long id);
        Task AddAsync(Tenant tenant);
        Task UpdateAsync(Tenant tenant);
        Task DeleteAsync(long id);
    }
}