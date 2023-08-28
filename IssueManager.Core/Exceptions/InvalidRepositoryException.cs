using IssueManager.Core.Entities;

namespace IssueManager.Core.Exceptions;

public class InvalidRepositoryException : IssueManagerException
{
    public InvalidRepositoryException(Error error) : base(error)
    {
    }
}