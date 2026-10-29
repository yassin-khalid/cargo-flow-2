using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;

namespace CargoFlow.Helpers;

public abstract class ExceptionsHelpers
{

    public static bool IsForeignKeyViolation(DbUpdateException ex, string constraintName)
    {
        if (ex.InnerException is not SqlException sqlEx)
        {
            return false;
        }

        // 547 is the SQL Server error number for foreign key conflicts
        return sqlEx.Number is 547 && sqlEx.Message.Contains(constraintName, StringComparison.OrdinalIgnoreCase);
    }


    public static bool IsUniqueConstraintViolation(DbUpdateException ex, string constraintName)
    {
        return ex.InnerException is SqlException { Number: (2601 or 2627), Message : var message } &&
               message.Contains(constraintName, StringComparison.OrdinalIgnoreCase);
    }
    
}
