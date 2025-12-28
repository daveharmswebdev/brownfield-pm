namespace PropertyManager.Domain.Exceptions;

/// <summary>
/// Exception thrown when a requested resource is not found.
/// Used for tenant-isolated access - also thrown when resource belongs to different account (AC-2.4.5).
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException()
        : base()
    {
    }

    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public NotFoundException(string name, object key)
        : base($"{name} with ID '{key}' was not found.")
    {
    }
}
