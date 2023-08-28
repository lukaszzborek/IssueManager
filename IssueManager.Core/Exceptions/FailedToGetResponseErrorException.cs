using IssueManager.Core.Entities;

namespace IssueManager.Core.Exceptions;

public class FailedToGetResponseErrorException : IssueManagerException
{
    public FailedToGetResponseErrorException(Error error) : base(error)
    {
    }
}