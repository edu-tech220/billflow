using System.ComponentModel.DataAnnotations;

namespace BillFlow.Api.Dtos;

public record SubscriptionDto(
    int Id,
    string PlanCode,
    string PlanName,
    string Status,
    DateTime CurrentPeriodStart,
    DateTime CurrentPeriodEnd,
    DateTime? TrialEndsAt,
    bool CancelAtPeriodEnd);

public record CreateSubscriptionRequest([Required] int PlanId);

public record ChangePlanRequest([Required] int NewPlanId);

public record CancelSubscriptionRequest(bool AtPeriodEnd);
