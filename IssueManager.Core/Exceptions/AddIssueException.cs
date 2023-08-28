using IssueManager.Core.Entities;

namespace IssueManager.Core.Exceptions;

public class AddIssueException : IssueManagerException
{

    public AddIssueException(Error error) : base(error)
    {
    }
}