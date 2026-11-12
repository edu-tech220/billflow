using BillFlow.Api.Services;

namespace BillFlow.Api.Tests.TestSupport;

public class TestClock : IClock
{
    public DateTime UtcNow { get; set; } = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}
