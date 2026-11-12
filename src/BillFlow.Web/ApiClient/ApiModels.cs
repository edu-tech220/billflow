namespace BillFlow.Web.ApiClient;

public record AuthResponse(string Token, DateTime ExpiresAt, string Role);

public record PlanDto(int Id, string Code, string Name, decimal MonthlyPrice, int TrialDays, bool IsActive);

public record SubscriptionDto(
    int Id,
    string PlanCode,
    string PlanName,
    string Status,
    DateTime CurrentPeriodStart,
    DateTime CurrentPeriodEnd,
    DateTime? TrialEndsAt,
    bool CancelAtPeriodEnd);

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

public record CustomerDto(int Id, string Name, string Email, string Role, decimal CreditBalance, DateTime CreatedAt);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public record DunningSweepResult(int RenewalsProcessed, int PaymentRetriesProcessed);

public record ApiProblem(string? Title, string? Detail, int? Status);
