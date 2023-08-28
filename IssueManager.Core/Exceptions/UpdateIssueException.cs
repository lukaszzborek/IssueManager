using IssueManager.Core.Entities;

namespace IssueManager.Core.Exceptions;

public class UpdateIssueException : IssueManagerException
{
    public UpdateIssueException(Error error) : base(error)
    {
    }
}