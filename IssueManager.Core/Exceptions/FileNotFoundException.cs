using IssueManager.Core.Entities;

namespace IssueManager.Core.Exceptions;

public class FileNotFoundException : IssueManagerException
{
    public FileNotFoundException(Error error) : base(error)
    {
    }
}