namespace BillFlow.Web.ApiClient;

public record RegisterRequest(string Name, string Email, string Password);

public record LoginRequest(string Email, string Password);

public record CreateSubscriptionRequest(int PlanId);

public record ChangePlanRequest(int NewPlanId);

public record CancelSubscriptionRequest(bool AtPeriodEnd);

public record CreatePlanRequest(string Code, string Name, decimal MonthlyPrice, int TrialDays);

public record UpdatePlanRequest(string Name, decimal MonthlyPrice, bool IsActive);
