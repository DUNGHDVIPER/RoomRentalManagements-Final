using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Entities.Tenanting;

namespace BLL.Services.Interfaces
{
    public interface IStayHistoryService
    {
        Task CheckInAsync(int roomId, int tenantId, string? note = null, CancellationToken ct = default);
        Task CheckOutAsync(int roomId, CancellationToken ct = default);
        Task<List<StayHistory>> GetRoomHistoryAsync(int roomId, CancellationToken ct = default);
    }
}
