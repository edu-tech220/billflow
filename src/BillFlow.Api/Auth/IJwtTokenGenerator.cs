using BillFlow.Api.Domain;

namespace BillFlow.Api.Auth;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAt) GenerateToken(Customer customer);
}
