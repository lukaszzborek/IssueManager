using System.Net.Http.Json;
using System.Text.Json;
using IssueManager.Core.DTOs;
using IssueManager.Core.DTOs.GitLab;
using IssueManager.Core.Entities;
using IssueManager.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace IssueManager.Core.Repositories;

public class GitLabRepository : IRepository
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _client;
    private readonly ILogger<GitLabRepository>? _logger;

    public GitLabRepository(HttpClient client, ILogger<GitLabRepository> logger)
    {
        _client = client;
        _logger = logger;
    }

    public GitLabRepository(string apiKey, string appName, ILogger<GitLabRepository>? logger = null)
    {
        _client = new HttpClient();
        _client.BaseAddress = new Uri("https://gitlab.com/api/v4/");
        _client.DefaultRequestHeaders.Add("Accept", "application/json");
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        _client.DefaultRequestHeaders.Add("User-Agent", appName);
        _logger = logger;
    }
    public async Task<Issue> AddIssueAsync(string repo, CreateIssueDto issue,
        CancellationToken cancellationToken = default)
    {
        ValidateRepo(repo);
        var issueGitLabDto = new CreateIssueGitLabDto(issue.Title, issue.Description);
        var response = await _client.PostAsJsonAsync($"projects/{repo}/issues", issueGitLabDto, cancellationToken);
        await CheckSuccessStatusCode(cancellationToken, response);

        var issueDtoResponse =
            await response.Content.ReadFromJsonAsync<IssueGitLabDto>(cancellationToken: cancellationToken);
        if (issueDtoResponse is null)
            throw new FailedToGetResponseErrorException(new Error("Failed to add issue"));

        return new Issue(issueDtoResponse.IId, issueDtoResponse.Title, issueDtoResponse.Description);
    }
    public async Task<Issue> UpdateIssueAsync(string repo, UpdateIssueDto issue,
        CancellationToken cancellationToken = default)
    {
        ValidateRepo(repo);
        var updateIssue = new UpdateIssueGitLabDto(issue.Id, issue.Title, issue.Description);
        var response = await _client.PutAsJsonAsync($"projects/{repo}/issues/{updateIssue.Id}", updateIssue,
            cancellationToken);
        await CheckSuccessStatusCode(cancellationToken, response);

        var issueDtoResponse =
            await response.Content.ReadFromJsonAsync<IssueGitLabDto>(cancellationToken: cancellationToken);

        if (issueDtoResponse is null)
            throw new FailedToGetResponseErrorException(new Error("Failed to update issue"));

        return new Issue(issueDtoResponse.IId, issueDtoResponse.Title, issueDtoResponse.Description);
    }
    public async Task<Issue> CloseIssueAsync(string repo, int id, CancellationToken cancellationToken = default)
    {
        ValidateRepo(repo);

        var closeIssue = new CloseIssueGitLabDto("close");
        var response = await _client.PutAsJsonAsync($"projects/{repo}/issues/{id}", closeIssue, cancellationToken);
        await CheckSuccessStatusCode(cancellationToken, response);

        var issueDtoResponse =
            await response.Content.ReadFromJsonAsync<IssueGitLabDto>(cancellationToken: cancellationToken);

        if (issueDtoResponse is null)
            throw new FailedToGetResponseErrorException(new Error("Failed to close issue"));

        return new Issue(issueDtoResponse.IId, issueDtoResponse.Title, issueDtoResponse.Description);
    }
    public async Task<IEnumerable<Issue>> GetAllIssuesAsync(string repo, int page = 1, int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        ValidateRepo(repo);
        var response = await _client.GetAsync($"projects/{repo}/issues?scope=all&page={page}&per_page={pageSize}",
            cancellationToken);
        await CheckSuccessStatusCode(cancellationToken, response);

        var issuesDtoResponse =
            await response.Content.ReadFromJsonAsync<IEnumerable<IssueGitLabDto>>(cancellationToken: cancellationToken);

        if (issuesDtoResponse is null)
            return new List<Issue>();

        return issuesDtoResponse.Select(issueDto => new Issue(issueDto.IId, issueDto.Title, issueDto.Description))
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
        if (string.IsNullOrEmpty(repo))
            throw new InvalidRepositoryException(new Error($"Repo '{repo}' is invalid"));
    }
}