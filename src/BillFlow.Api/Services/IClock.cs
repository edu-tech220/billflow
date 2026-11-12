namespace BillFlow.Api.Services;

/// <summary>Abstracts the current time so billing logic (periods, dunning schedules) is deterministic in tests.</summary>
public interface IClock
{
    DateTime UtcNow { get; }
}

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
