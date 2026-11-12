namespace BillFlow.Api.Services;

public interface IInvoiceNumberGenerator
{
    Task<string> NextAsync(int year, CancellationToken ct = default);
}
