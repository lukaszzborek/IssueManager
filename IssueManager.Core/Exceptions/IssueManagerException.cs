using IssueManager.Core.Entities;

namespace IssueManager.Core.Exceptions;

public class IssueManagerException : Exception
{
    public Error Error { get; }

    protected IssueManagerException(Error error, string? message = default) : base(string.IsNullOrEmpty(message)
        ? error.Message
        : message)
    {
        Error = error;
    }
}