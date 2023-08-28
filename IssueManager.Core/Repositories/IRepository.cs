using IssueManager.Core.DTOs;
using IssueManager.Core.Entities;

namespace IssueManager.Core.Repositories;

public interface IRepository
{
    Task<Issue> AddIssueAsync(string repo, CreateIssueDto issue, CancellationToken cancellationToken = default);
    Task<Issue> UpdateIssueAsync(string repo, UpdateIssueDto issue, CancellationToken cancellationToken = default);
    Task<Issue> CloseIssueAsync(string repo, int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Issue>> GetAllIssuesAsync(string repo, int page = 1, int pageSize = 50,
        CancellationToken cancellationToken = default);
}