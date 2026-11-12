namespace BillFlow.Api.Dtos;

public record InvoiceDto(
    int Id,
    int SubscriptionId,
    string Number,
    string Type,
    decimal Amount,
    string Status,
    DateTime IssuedAt,
    DateTime DueAt,
    DateTime? PaidAt,
    int AttemptCount,
    DateTime? NextRetryAt);

public record PaymentResultDto(bool Succeeded, string InvoiceStatus, string? FailureReason);
