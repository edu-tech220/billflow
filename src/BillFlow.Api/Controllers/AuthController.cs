using BillFlow.Api.Auth;
using BillFlow.Api.Data;
using BillFlow.Api.Domain;
using BillFlow.Api.Dtos;
using BillFlow.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly BillFlowDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IClock _clock;

    public AuthController(BillFlowDbContext db, IPasswordHasher passwordHasher, IJwtTokenGenerator tokenGenerator, IClock clock)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _clock = clock;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var emailTaken = await _db.Customers.AnyAsync(c => c.Email == request.Email, ct);
        if (emailTaken)
        {
            return Conflict(new ProblemDetails { Title = "Conflict", Detail = "That email is already registered.", Status = StatusCodes.Status409Conflict });
        }

        var (hash, salt) = _passwordHasher.HashPassword(request.Password);
        var customer = new Customer
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = CustomerRole.Customer,
            CreatedAt = _clock.UtcNow,
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(ct);

        var (token, expiresAt) = _tokenGenerator.GenerateToken(customer);
        return Ok(new AuthResponse(token, expiresAt, customer.Role.ToString()));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Email == request.Email, ct);
        if (customer is null || !_passwordHasher.VerifyPassword(request.Password, customer.PasswordHash, customer.PasswordSalt))
        {
            return Unauthorized(new ProblemDetails { Title = "Unauthorized", Detail = "Invalid email or password.", Status = StatusCodes.Status401Unauthorized });
        }

        var (token, expiresAt) = _tokenGenerator.GenerateToken(customer);
        return Ok(new AuthResponse(token, expiresAt, customer.Role.ToString()));
    }
}
