namespace BillFlow.Api.Domain;

public enum InvoiceStatus
{
    Open = 0,
    Paid = 1,
    Void = 2,
    Uncollectible = 3,
}
