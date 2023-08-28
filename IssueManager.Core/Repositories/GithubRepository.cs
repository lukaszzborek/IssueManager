using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using IssueManager.Core.DTOs;
using IssueManager.Core.DTOs.GitHub;
using IssueManager.Core.Entities;
using IssueManager.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace IssueManager.Core.Repositories;

public class GithubRepository : IRepository
{

    private const string RepositoryPattern = @".+[/].+";

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _client;
    private readonly ILogger<GithubRepository>? _logger;

    public GithubRepository(HttpClient client, ILogger<GithubRepository> logger)
    {
        _client = client;
        _logger = logger;
    }

    public GithubRepository(string apiKey, string appName, ILogger<GithubRepository>? logger = null)
    {
        _client = new HttpClient();
        _client.BaseAddress = new Uri("https://api.github.com/");
        _client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        _client.DefaultRequestHeaders.Add("User-Agent", appName);
        _logger = logger;
    }

    public async Task<Issue> AddIssueAsync(string repo, CreateIssueDto issue,
        CancellationToken cancellationToken = default)
    {
        ValidateRepo(repo);
        var issueGitHubDto = new CreateIssueGitHubDto(issue.Title, issue.Description);
        var response = await _client.PostAsJsonAsync($"repos/{repo}/issues", issueGitHubDto, cancellationToken);
        await CheckSuccessStatusCode(cancellationToken, response);

        var issueDtoResponse =
            await response.Content.ReadFromJsonAsync<IssueGitHubDto>(cancellationToken: cancellationToken);
        if (issueDtoResponse is null)
            throw new FailedToGetResponseErrorException(new Error("Failed to add issue"));

        return new Issue(issueDtoResponse.Number, issueDtoResponse.Title, issueDtoResponse.Body);
    }

    public async Task<Issue> UpdateIssueAsync(string repo, UpdateIssueDto issue,
        CancellationToken cancellationToken = default)
    {
        ValidateRepo(repo);
        var updateIssue = new UpdateIssueGitHubDto(issue.Id, issue.Title, issue.Description);
        var response = await _client.PatchAsJsonAsync($"repos/{repo}/issues/{updateIssue.Id}", updateIssue,
            cancellationToken);
        await CheckSuccessStatusCode(cancellationToken, response);

        var issueDtoResponse =
            await response.Content.ReadFromJsonAsync<IssueGitHubDto>(cancellationToken: cancellationToken);

        if (issueDtoResponse is null)
            throw new FailedToGetResponseErrorException(new Error("Failed to update issue"));

        return new Issue(issueDtoResponse.Number, issueDtoResponse.Title, issueDtoResponse.Body);
    }


    public async Task<Issue> CloseIssueAsync(string repo, int id, CancellationToken cancellationToken = default)
    {
        ValidateRepo(repo);
        var closeIssue = new CloseIssueGitHubDto(id, "close");
        var response = await _client.PatchAsJsonAsync($"repos/{repo}/issues/{id}", closeIssue, cancellationToken);
        await CheckSuccessStatusCode(cancellationToken, response);

        var issueDtoResponse =
            await response.Content.ReadFromJsonAsync<IssueGitHubDto>(cancellationToken: cancellationToken);

        if (issueDtoResponse is null)
            throw new FailedToGetResponseErrorException(new Error("Failed to close issue"));

        return new Issue(issueDtoResponse.Number, issueDtoResponse.Title, issueDtoResponse.Body);
    }

    public async Task<IEnumerable<Issue>> GetAllIssuesAsync(string repo, int page = 1, int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        ValidateRepo(repo);
        var response =
            await _client.GetAsync($"repos/{repo}/issues?page={page}&per_page={pageSize}", cancellationToken);
        await CheckSuccessStatusCode(cancellationToken, response);

        var issuesDtoResponse =
            await response.Content.ReadFromJsonAsync<IEnumerable<IssueGitHubDto>>(cancellationToken: cancellationToken);

        if (issuesDtoResponse is null)
            return new List<Issue>();

        return issuesDtoResponse.Select(issueDto => new Issue(issueDto.Number, issueDto.Title, issueDto.Body))
                                .ToList();
    }

    private T? Deserialize<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonSerializerOptions);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to deserialize json");
            return default;
        }
    }

    private async Task CheckSuccessStatusCode(CancellationToken cancellationToken, HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorStr = await response.Content.ReadAsStringAsync(cancellationToken: cancellationToken);
            var error = Deserialize<Error>(errorStr);
            if (error is not null)
                throw new FailedToGetErrorException(new Error(error.Message));

            throw new FailedToSendRequestException(new Error(errorStr));
        }
    }

    private void ValidateRepo(string repo)
    {
        if (!Regex.Match(repo, RepositoryPattern)
                  .Success)
            throw new InvalidRepositoryException(new Error($"Repo '{repo}' is invalid. Expected format: 'owner/repo'"));
    }
}