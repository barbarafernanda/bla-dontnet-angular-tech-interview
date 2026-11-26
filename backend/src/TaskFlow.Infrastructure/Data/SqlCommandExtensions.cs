using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace TaskFlow.Infrastructure.Data;

internal static class SqlCommandExtensions
{
    public static void AddParameter(this DbCommand command, string name, SqlDbType type, object? value, int size = 0)
    {
        var parameter = command.CreateParameter() as SqlParameter
            ?? throw new InvalidOperationException("The connection must provide SQL Server parameters.");
        parameter.ParameterName = name;
        parameter.SqlDbType = type;
        if (size != 0) parameter.Size = size;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
