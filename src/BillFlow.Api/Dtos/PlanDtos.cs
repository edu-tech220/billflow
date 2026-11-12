using System.ComponentModel.DataAnnotations;

namespace BillFlow.Api.Dtos;

public record PlanDto(int Id, string Code, string Name, decimal MonthlyPrice, int TrialDays, bool IsActive);

public record CreatePlanRequest(
    [Required, MaxLength(50)] string Code,
    [Required, MaxLength(200)] string Name,
    [Range(0.01, 100000)] decimal MonthlyPrice,
    [Range(0, 365)] int TrialDays);

public record UpdatePlanRequest(
    [Required, MaxLength(200)] string Name,
    [Range(0.01, 100000)] decimal MonthlyPrice,
    bool IsActive);
