using System.Reflection;
using Microsoft.Data.SqlClient;

namespace TaskFlow.Infrastructure.Tests.TestDoubles;

internal static class SqlErrors
{
    public static SqlException Create(int number, string message)
    {
        var errorConstructor = typeof(SqlError).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance, null,
            [typeof(int), typeof(byte), typeof(byte), typeof(string), typeof(string), typeof(string), typeof(int), typeof(Exception)], null)
            ?? throw new InvalidOperationException("SqlClient SqlError constructor was not found.");
        var error = (SqlError)errorConstructor.Invoke([number, (byte)1, (byte)14, "test", message, "", 1, null]);
        var errors = (SqlErrorCollection)(Activator.CreateInstance(typeof(SqlErrorCollection), nonPublic: true)
            ?? throw new InvalidOperationException("Could not create SQL error collection."));
        var add = typeof(SqlErrorCollection).GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("SqlClient SqlErrorCollection.Add was not found.");
        add.Invoke(errors, [error]);
        var create = typeof(SqlException).GetMethod("CreateException", BindingFlags.NonPublic | BindingFlags.Static, null,
            [typeof(SqlErrorCollection), typeof(string)], null)
            ?? throw new InvalidOperationException("SqlClient SqlException factory was not found.");
        return (SqlException)(create.Invoke(null, [errors, "16.0.0"])
            ?? throw new InvalidOperationException("Could not create SQL exception."));
    }
}
