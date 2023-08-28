using IssueManager.Core.Entities;

namespace IssueManager.Core.Exceptions;

public class InvalidLineFormatException : IssueManagerException
{
    public InvalidLineFormatException(Error error) : base(error)
    {
    }
}