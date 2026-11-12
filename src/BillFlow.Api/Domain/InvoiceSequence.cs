namespace BillFlow.Api.Domain;

/// <summary>
/// Per-year counter used to mint sequential, human-readable invoice numbers
/// (e.g. "INV-2026-000001") without relying on a database-specific sequence object,
/// so the same code path works against SQL Server and SQLite.
/// </summary>
public class InvoiceSequence : IHasConcurrencyStamp
{
    public int Year { get; set; }
    public int LastNumber { get; set; }
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();
}
