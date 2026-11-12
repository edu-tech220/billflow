using BillFlow.Api.Auth;
using BillFlow.Api.Data;
using BillFlow.Api.Domain;
using BillFlow.Api.Dtos;
using BillFlow.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.Api.Controllers;

[ApiController]
[Route("api/subscriptions")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly BillFlowDbContext _db;
    private readonly ISubscriptionService _subscriptions;

    public SubscriptionsController(BillFlowDbContext db, ISubscriptionService subscriptions)
    {
        _db = db;
        _subscriptions = subscriptions;
    }

    [HttpPost]
    public async Task<ActionResult<SubscriptionDto>> Create(CreateSubscriptionRequest request, CancellationToken ct)
    {
        var customerId = User.GetCustomerId();
        var subscription = await _subscriptions.CreateAsync(customerId, request.PlanId, ct);
        var dto = await ToDtoAsync(subscription.Id, ct);
        return CreatedAtAction(nameof(GetMine), null, dto);
    }

    [HttpGet("me")]
    public async Task<ActionResult<SubscriptionDto>> GetMine(CancellationToken ct)
    {
        var customerId = User.GetCustomerId();
        var subscription = await _db.Subscriptions
            .Where(s => s.CustomerId == customerId)
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync(ct);

        if (subscription is null)
        {
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = "No subscription found for this account.", Status = StatusCodes.Status404NotFound });
        }

        return Ok(await ToDtoAsync(subscription.Id, ct));
    }

    [HttpPost("me/change-plan")]
    public async Task<ActionResult<SubscriptionDto>> ChangePlan(ChangePlanRequest request, CancellationToken ct)
    {
        var subscriptionId = await GetOpenSubscriptionIdAsync(ct);
        var subscription = await _subscriptions.ChangePlanAsync(subscriptionId, request.NewPlanId, ct);
        return Ok(await ToDtoAsync(subscription.Id, ct));
    }

    [HttpPost("me/cancel")]
    public async Task<ActionResult<SubscriptionDto>> Cancel(CancelSubscriptionRequest request, CancellationToken ct)
    {
        var subscriptionId = await GetOpenSubscriptionIdAsync(ct);
        var subscription = await _subscriptions.CancelAsync(subscriptionId, request.AtPeriodEnd, ct);
        return Ok(await ToDtoAsync(subscription.Id, ct));
    }

    private async Task<int> GetOpenSubscriptionIdAsync(CancellationToken ct)
    {
        var customerId = User.GetCustomerId();
        var subscriptionId = await _db.Subscriptions
            .Where(s => s.CustomerId == customerId
                && (s.Status == SubscriptionStatus.Trialing || s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.PastDue))
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync(ct);

        return subscriptionId ?? throw new EntityNotFoundException(nameof(Subscription), customerId);
    }

    private async Task<SubscriptionDto> ToDtoAsync(int subscriptionId, CancellationToken ct)
    {
        var subscription = await _db.Subscriptions.Include(s => s.Plan).FirstAsync(s => s.Id == subscriptionId, ct);
        return new SubscriptionDto(
            subscription.Id,
            subscription.Plan.Code,
            subscription.Plan.Name,
            subscription.Status.ToString(),
            subscription.CurrentPeriodStart,
            subscription.CurrentPeriodEnd,
            subscription.TrialEndsAt,
            subscription.CancelAtPeriodEnd);
    }
}
