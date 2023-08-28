using IssueManager.Core.Entities;

namespace IssueManager.Core.Exceptions;

public class CloseIssueException : IssueManagerException
{

    public CloseIssueException(Error error) : base(error)
    {
    }
}