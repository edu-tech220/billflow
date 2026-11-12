namespace BillFlow.Api.Dtos;

public record CustomerDto(int Id, string Name, string Email, string Role, decimal CreditBalance, DateTime CreatedAt);
