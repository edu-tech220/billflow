using BillFlow.Api.Data;
using BillFlow.Api.Domain;
using BillFlow.Api.Dtos;
using BillFlow.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.Api.Controllers;

[ApiController]
[Route("api/plans")]
public class PlansController : ControllerBase
{
    private readonly BillFlowDbContext _db;

    public PlansController(BillFlowDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<PlanDto>>> GetPlans(CancellationToken ct)
    {
        var isAdmin = User.IsInRole(nameof(CustomerRole.Admin));
        var query = _db.Plans.AsQueryable();
        if (!isAdmin)
        {
            query = query.Where(p => p.IsActive);
        }

        var plans = await query
            .Select(p => ToDto(p))
            .ToListAsync(ct);

        // Ordered client-side: SQLite (used in tests) can't translate ORDER BY on a decimal column.
        return Ok(plans.OrderBy(p => p.MonthlyPrice));
    }

    [HttpPost]
    [Authorize(Roles = nameof(CustomerRole.Admin))]
    public async Task<ActionResult<PlanDto>> Create(CreatePlanRequest request, CancellationToken ct)
    {
        var codeTaken = await _db.Plans.AnyAsync(p => p.Code == request.Code, ct);
        if (codeTaken)
        {
            return Conflict(new ProblemDetails { Title = "Conflict", Detail = $"Plan code '{request.Code}' already exists.", Status = StatusCodes.Status409Conflict });
        }

        var plan = new Plan
        {
            Code = request.Code,
            Name = request.Name,
            MonthlyPrice = request.MonthlyPrice,
            TrialDays = request.TrialDays,
            IsActive = true,
        };

        _db.Plans.Add(plan);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetPlans), null, ToDto(plan));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(CustomerRole.Admin))]
    public async Task<ActionResult<PlanDto>> Update(int id, UpdatePlanRequest request, CancellationToken ct)
    {
        var plan = await _db.Plans.FindAsync(new object[] { id }, ct)
            ?? throw new EntityNotFoundException(nameof(Plan), id);

        plan.Name = request.Name;
        plan.MonthlyPrice = request.MonthlyPrice;
        plan.IsActive = request.IsActive;

        await _db.SaveChangesAsync(ct);
        return Ok(ToDto(plan));
    }

    private static PlanDto ToDto(Plan plan) => new(plan.Id, plan.Code, plan.Name, plan.MonthlyPrice, plan.TrialDays, plan.IsActive);
}
