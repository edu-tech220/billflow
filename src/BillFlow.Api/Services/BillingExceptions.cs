namespace BillFlow.Api.Services;

public class EntityNotFoundException : Exception
{
    public EntityNotFoundException(string entityName, object key)
        : base($"{entityName} '{key}' was not found.")
    {
    }
}

/// <summary>Thrown when an operation would move a subscription or invoice into a state its state machine forbids.</summary>
public class InvalidStateTransitionException : Exception
{
    public InvalidStateTransitionException(string message) : base(message)
    {
    }
}
