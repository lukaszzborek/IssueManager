using System.Text;
using IssueManager.Core.DTOs;
using IssueManager.Core.Entities;
using IssueManager.Core.Exceptions;
using IssueManager.Core.Repositories;
using FileNotFoundException=IssueManager.Core.Exceptions.FileNotFoundException;

namespace IssueManager.Core.Services;

public class RepositoryService
{
    private readonly IRepository _repository;

    public RepositoryService(IRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Issue>> AddIssueAsync(string repo, CreateIssueDto issue,
        CancellationToken cancellationToken = default)
    {
        return await GetResult(_repository.AddIssueAsync(repo, issue, cancellationToken));
    }

    public async Task<Result<Issue>> UpdateIssueAsync(string repo, UpdateIssueDto issue,
        CancellationToken cancellationToken = default)
    {
        return await GetResult(_repository.UpdateIssueAsync(repo, issue, cancellationToken));
    }

    public async Task<Result<Issue>> CloseIssueAsync(string repo, int id, CancellationToken cancellationToken = default)
    {
        return await GetResult(_repository.CloseIssueAsync(repo, id, cancellationToken));
    }

    public async Task<Result<IEnumerable<Issue>>> GetAllIssuesAsync(string repo, int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var page = 1;
        var list = new List<Issue>();
        int currentSize;

        do
        {
            try
            {
                var issues = (await _repository.GetAllIssuesAsync(repo, page, pageSize, cancellationToken)).ToList();
                list.AddRange(issues);
                page++;
                currentSize = issues.Count;
            }
            catch (IssueManagerException e)
            {
                return new Result<IEnumerable<Issue>>
                {
                    IsSuccessful = false,
                    Error = e.Error
                };
            }
            catch (Exception e)
            {
                return new Result<IEnumerable<Issue>>
                {
                    IsSuccessful = false,
                    Error = new Error(e.Message)
                };
            }
        } while (currentSize == pageSize);

        return new Result<IEnumerable<Issue>>
        {
            Data = list,
            IsSuccessful = true
        };
    }

    public async Task<Result<IEnumerable<Issue>>> ExportIssuesAsync(string repo, string path,
        CancellationToken cancellationToken = default)
    {
        var issues = await GetAllIssuesAsync(repo, cancellationToken: cancellationToken);
        if (!issues.IsSuccessful)
            return issues;

        var sb = new StringBuilder();
        sb.Append(nameof(Issue.Id));
        sb.Append(';');
        sb.Append(nameof(Issue.Name));
        sb.Append(';');
        sb.AppendLine(nameof(Issue.Description));

        foreach (var issue in issues.Data)
        {
            sb.Append(issue.Id);
            sb.Append(';');
            sb.Append(issue.Name);
            sb.Append(';');
            sb.AppendLine(issue.Description);
        }
        await File.WriteAllTextAsync(path, sb.ToString(), cancellationToken);
        return issues;
    }

    public async Task<IEnumerable<Result<Issue>>> ImportIssueToServiceAsync(string repo, string path,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException(new Error($"File '{path}' not found"));

        var lines = await File.ReadAllLinesAsync(path, cancellationToken);
        var issues = new List<Issue>();
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            var values = line.Split(';');
            if (values.Length != 3)
                throw new InvalidLineFormatException(
                    new Error($"Invalid line format at line {i}. Expected 'Id;Name;Description'"));

            var id = int.Parse(values[0]);
            var name = values[1];
            var description = values[2];
            issues.Add(new Issue(id, name, description));
        }

        var results = new List<Result<Issue>>();

        foreach (var issue in issues)
        {
            var result = await GetResult(_repository.AddIssueAsync(repo,
                new CreateIssueDto(issue.Name, issue.Description), cancellationToken));
            if (!result.IsSuccessful)
                result = new Result<Issue>
                {
                    Data = issue,
                    IsSuccessful = result.IsSuccessful,
                    Error = result.Error
                };

            results.Add(result);
        }
        return results;
    }

    private async Task<Result<T>> GetResult<T>(Task<T> action)
    {
        try
        {
            var actionResult = await action;
            return new Result<T>
            {
                Data = actionResult,
                IsSuccessful = true
            };
        }
        catch (IssueManagerException e)
        {
            return new Result<T>
            {
                IsSuccessful = false,
                Error = e.Error
            };
        }
        catch (Exception e)
        {
            return new Result<T>
            {
                IsSuccessful = false,
                Error = new Error(e.Message)
            };
        }
    }
}