namespace BillFlow.Api.Domain;

/// <summary>
/// Marks an entity as carrying a provider-agnostic optimistic concurrency token.
/// Unlike SQL Server's native rowversion, a plain Guid concurrency token behaves
/// identically on SQL Server and SQLite, which keeps the in-memory test suite
/// honest about concurrency conflicts without needing a real database.
/// </summary>
public interface IHasConcurrencyStamp
{
    Guid ConcurrencyStamp { get; set; }
}
