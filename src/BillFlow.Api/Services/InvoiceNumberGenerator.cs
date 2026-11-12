using BillFlow.Api.Data;
using BillFlow.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.Api.Services;

/// <summary>
/// Mints sequential invoice numbers like "INV-2026-000001" using a per-year counter row.
/// Concurrent callers race to increment the same row, so conflicts are expected: on an
/// optimistic-concurrency (or unique-key, for the very first invoice of a year) failure we
/// simply reload and retry. This works identically against SQL Server and SQLite.
/// </summary>
public class InvoiceNumberGenerator : IInvoiceNumberGenerator
{
    private const int MaxAttempts = 5;

    private readonly BillFlowDbContext _db;

    public InvoiceNumberGenerator(BillFlowDbContext db)
    {
        _db = db;
    }

    public async Task<string> NextAsync(int year, CancellationToken ct = default)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var sequence = await _db.InvoiceSequences.FindAsync(new object[] { year }, ct);
            var isNew = sequence is null;
            sequence ??= new InvoiceSequence { Year = year, LastNumber = 0 };

            sequence.LastNumber++;
            var candidateNumber = sequence.LastNumber;

            if (isNew)
            {
                _db.InvoiceSequences.Add(sequence);
            }

            try
            {
                await _db.SaveChangesAsync(ct);
                return FormatNumber(year, candidateNumber);
            }
            catch (DbUpdateException)
            {
                _db.Entry(sequence).State = EntityState.Detached;
            }
        }

        throw new InvalidOperationException($"Could not allocate an invoice number for year {year} after {MaxAttempts} attempts.");
    }

    private static string FormatNumber(int year, int number) => $"INV-{year}-{number:D6}";
}
