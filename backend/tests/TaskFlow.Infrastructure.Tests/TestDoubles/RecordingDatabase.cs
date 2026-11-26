using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.SqlClient;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Tests.TestDoubles;

internal sealed class RecordingDatabase : IDbConnectionFactory
{
    public List<RecordedCommand> Commands { get; } = [];
    public Func<DbDataReader> Reader { get; set; } = () => new DataTable().CreateDataReader();
    public int RowsAffected { get; set; } = 1;
    public Exception? Error { get; set; }
    public int DisposedConnections { get; private set; }
    public int DisposedCommands { get; private set; }

    public Task<DbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<DbConnection>(new Connection(this));

    public sealed record RecordedCommand(string Text, IReadOnlyList<SqlParameter> Parameters);

    private sealed class Connection(RecordingDatabase owner) : DbConnection
    {
        [AllowNull] public override string ConnectionString { get; set; } = string.Empty;
        public override string Database => "TaskFlowDb";
        public override string DataSource => "test";
        public override string ServerVersion => "1";
        public override ConnectionState State => ConnectionState.Open;
        public override void ChangeDatabase(string databaseName) { }
        public override void Close() { }
        public override void Open() { }
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
        protected override DbCommand CreateDbCommand() => new Command(owner, this);
        protected override void Dispose(bool disposing) { if (disposing) owner.DisposedConnections++; base.Dispose(disposing); }
    }

    private sealed class Command(RecordingDatabase owner, DbConnection connection) : DbCommand
    {
        private readonly SqlCommand parameters = new();
        [AllowNull] public override string CommandText { get; set; } = string.Empty;
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; } = CommandType.Text;
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; } = connection;
        protected override DbParameterCollection DbParameterCollection => parameters.Parameters;
        protected override DbTransaction? DbTransaction { get; set; }
        public override void Cancel() { }
        public override void Prepare() { }
        protected override DbParameter CreateDbParameter() => parameters.CreateParameter();
        public override int ExecuteNonQuery() { Record(); return owner.RowsAffected; }
        public override object? ExecuteScalar() { Record(); return null; }
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) { Record(); return owner.Reader(); }
        protected override void Dispose(bool disposing) { if (disposing) { owner.DisposedCommands++; parameters.Dispose(); } base.Dispose(disposing); }

        private void Record()
        {
            owner.Commands.Add(new RecordedCommand(CommandText, parameters.Parameters.Cast<SqlParameter>().ToArray()));
            if (owner.Error is not null) throw owner.Error;
        }
    }
}
