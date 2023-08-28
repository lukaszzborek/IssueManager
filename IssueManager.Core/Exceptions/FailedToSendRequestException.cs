using IssueManager.Core.Entities;

namespace IssueManager.Core.Exceptions;

public class FailedToSendRequestException : IssueManagerException
{
    public FailedToSendRequestException(Error error) : base(error, "Failed to send request. Contact the administrator")
    {
    }
}