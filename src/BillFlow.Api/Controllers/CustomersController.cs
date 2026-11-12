using BillFlow.Api.Data;
using BillFlow.Api.Domain;
using BillFlow.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize(Roles = nameof(CustomerRole.Admin))]
public class CustomersController : ControllerBase
{
    private readonly BillFlowDbContext _db;

    public CustomersController(BillFlowDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CustomerDto>>> List(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _db.Customers
            .OrderBy(c => c.Id)
            .Select(c => new CustomerDto(c.Id, c.Name, c.Email, c.Role.ToString(), c.CreditBalance, c.CreatedAt))
            .ToPagedResultAsync(page, pageSize, ct);

        return Ok(result);
    }
}
