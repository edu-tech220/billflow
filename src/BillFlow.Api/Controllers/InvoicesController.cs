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
[Route("api/invoices")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly BillFlowDbContext _db;
    private readonly IPaymentService _payments;

    public InvoicesController(BillFlowDbContext db, IPaymentService payments)
    {
        _db = db;
        _payments = payments;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<InvoiceDto>>> List(int page = 1, int pageSize = 20, int? customerId = null, CancellationToken ct = default)
    {
        var isAdmin = User.IsInRole(nameof(CustomerRole.Admin));
        var effectiveCustomerId = isAdmin ? customerId : User.GetCustomerId();

        var query = _db.Invoices.Include(i => i.Subscription).AsQueryable();
        if (effectiveCustomerId is not null)
        {
            query = query.Where(i => i.Subscription.CustomerId == effectiveCustomerId);
        }

        var result = await query
            .OrderByDescending(i => i.IssuedAt)
            .Select(i => ToDto(i))
            .ToPagedResultAsync(page, pageSize, ct);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InvoiceDto>> GetById(int id, CancellationToken ct)
    {
        var invoice = await _db.Invoices.Include(i => i.Subscription).FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new EntityNotFoundException(nameof(Invoice), id);

        if (!User.IsInRole(nameof(CustomerRole.Admin)) && invoice.Subscription.CustomerId != User.GetCustomerId())
        {
            return Forbid();
        }

        return Ok(ToDto(invoice));
    }

    [HttpPost("{id:int}/payments")]
    public async Task<ActionResult<PaymentResultDto>> Pay(int id, [FromHeader(Name = "Idempotency-Key")] string idempotencyKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new ProblemDetails { Title = "Bad Request", Detail = "The Idempotency-Key header is required.", Status = StatusCodes.Status400BadRequest });
        }

        var invoice = await _db.Invoices.Include(i => i.Subscription).FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new EntityNotFoundException(nameof(Invoice), id);

        if (!User.IsInRole(nameof(CustomerRole.Admin)) && invoice.Subscription.CustomerId != User.GetCustomerId())
        {
            return Forbid();
        }

        var result = await _payments.ChargeInvoiceAsync(id, idempotencyKey, ct);
        return Ok(new PaymentResultDto(result.Succeeded, result.InvoiceStatus.ToString(), result.FailureReason));
    }

    private static InvoiceDto ToDto(Invoice invoice) => new(
        invoice.Id,
        invoice.SubscriptionId,
        invoice.Number,
        invoice.Type.ToString(),
        invoice.Amount,
        invoice.Status.ToString(),
        invoice.IssuedAt,
        invoice.DueAt,
        invoice.PaidAt,
        invoice.AttemptCount,
        invoice.NextRetryAt);
}
