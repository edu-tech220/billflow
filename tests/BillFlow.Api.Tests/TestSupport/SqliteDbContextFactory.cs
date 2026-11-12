using BillFlow.Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.Api.Tests.TestSupport;

/// <summary>
/// Creates an isolated, schema-ready SQLite in-memory <see cref="BillFlowDbContext"/> for a single test.
/// The underlying connection must stay open for the lifetime of the context, so this type
/// owns it and disposes it alongside the context.
/// </summary>
public sealed class SqliteDbContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteDbContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public BillFlowDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BillFlowDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new BillFlowDbContext(options);
    }

    public void Dispose() => _connection.Dispose();
}
