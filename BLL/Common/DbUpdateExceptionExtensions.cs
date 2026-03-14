using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BLL.Common;

public static class DbUpdateExceptionExtensions
{
    public static bool IsUniqueViolation(this DbUpdateException ex)
    {
        if (ex.InnerException is SqlException sqlEx)
            return sqlEx.Number is 2601 or 2627;

        return false;
    }

    public static bool IsOneActivePerRoomViolation(this DbUpdateException ex)
    {
        if (ex.InnerException is SqlException sqlEx)
            return sqlEx.Message.Contains(
                "UX_Contracts_OneActivePerRoom",
                StringComparison.OrdinalIgnoreCase);

        return false;
    }
}
