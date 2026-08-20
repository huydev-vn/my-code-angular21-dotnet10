namespace Application.Common.Errors;

public sealed class PersistenceConflictException : Exception
{
    public PersistenceConflictException()
        : base("The change conflicted with existing data.")
    {
    }

    public PersistenceConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
