namespace IssueManager.Core.Services;

public class RateLimitingHttpClientHandler : DelegatingHandler
{
    private int _remainingRequests = 10;


    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_remainingRequests < 5)
            await Task.Delay(500);

        var response = await base.SendAsync(request, cancellationToken);

        foreach (var header in response.Headers)
            if (header.Key.ToLower() == "x-ratelimit-remaining")
                _remainingRequests = int.Parse(header.Value.First());

        return response;
    }
}