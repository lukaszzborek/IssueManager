using IssueManager.Core.Entities;

namespace IssueManager.Core.Exceptions;

public class FailedToGetErrorException : IssueManagerException
{
    public FailedToGetErrorException(Error error) : base(error)
    {
    }
}