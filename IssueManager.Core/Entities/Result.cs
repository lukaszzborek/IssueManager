namespace IssueManager.Core.Entities;

public class Result<T>
{
    public T Data { get; init; }
    public bool IsSuccessful { get; init; }
    public Error Error { get; init; }
    public int RateLimit { get; init; }
    public int RateLimitRemaining { get; init; }
}