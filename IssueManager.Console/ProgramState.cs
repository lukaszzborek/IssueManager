using IssueManager.Core.DTOs;
using IssueManager.Core.Entities;
using IssueManager.Core.Exceptions;
using IssueManager.Core.Repositories;
using IssueManager.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Spectre.Console;

namespace IssueManager.Console;

public class ProgramState
{

    private readonly IServiceProvider _serviceProvider;
    private RepositoryService _repositoryService;
    public string Service { get; private set; }
    public string Repository { get; private set; }

    public ProgramState(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public bool SetRepositoryService()
    {
        Service = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Select repository service:")
                                                                  .PageSize(10)
                                                                  .AddChoices("GitHub", "GitLab"));

        switch (Service.ToLower())
        {
            case "github":
                var configurationGitHub = _serviceProvider.GetRequiredService<IOptions<GitHubOptions>>();
                if (string.IsNullOrWhiteSpace(configurationGitHub.Value.ApiKey) ||
                    string.IsNullOrWhiteSpace(configurationGitHub.Value.AppName))
                {
                    AnsiConsole.WriteLine("GitHub ApiKey or AppName is empty. Please check configuration file");
                    return false;
                }

                _repositoryService = new RepositoryService(_serviceProvider.GetRequiredService<GithubRepository>());

                return true;

            case "gitlab":
                var configurationGitLab = _serviceProvider.GetRequiredService<IOptions<GitLabOptions>>();
                if (string.IsNullOrWhiteSpace(configurationGitLab.Value.ApiKey))
                {
                    AnsiConsole.WriteLine("GitLab ApiKey is empty. Please check configuration file");
                    return false;
                }

                _repositoryService = new RepositoryService(_serviceProvider.GetRequiredService<GitLabRepository>());

                return true;
        }

        return false;
    }

    public void SetRepository()
    {
        Repository = AnsiConsole.Ask<string>($"Enter repository for {Service}:")
                                .Trim();
    }

    public async Task AddIssueAsync()
    {
        var title = AnsiConsole.Ask<string>("Enter issue title:");
        var description = AnsiConsole.Ask<string>("Enter issue description:");

        var result = await _repositoryService.AddIssueAsync(Repository, new CreateIssueDto(title, description));
        if (!result.IsSuccessful)
            AnsiConsole.WriteLine($"Failed to create issue {result.Error.Message}");
        else
            AnsiConsole.WriteLine("Issue created");

        AnsiConsole.Confirm("Press any key to continue...");
    }

    public async Task GetIssuesAsync()
    {
        var issues = await _repositoryService.GetAllIssuesAsync(Repository);
        if (!issues.IsSuccessful)
        {
            AnsiConsole.WriteLine($"Failed to get issues {issues.Error.Message}");
        }
        else
        {
            var grid = new Grid();

            grid.AddColumn();
            grid.AddColumn();
            grid.AddColumn();

            grid.AddRow(nameof(Issue.Id), nameof(Issue.Name), nameof(Issue.Description));

            foreach (var issue in issues.Data)
                grid.AddRow(issue.Id.ToString(), issue.Name, issue.Description ?? "");

            AnsiConsole.Write(grid);
        }

        AnsiConsole.Confirm("Press any key to continue...");
    }

    public async Task UpdateIssueAsync()
    {
        var id = AnsiConsole.Ask<int>("Enter issue id:");
        var title = AnsiConsole.Ask<string>("Enter issue title:");
        var description = AnsiConsole.Ask<string>("Enter issue description:");

        var result = await _repositoryService.UpdateIssueAsync(Repository, new UpdateIssueDto(id, title, description));
        if (!result.IsSuccessful)
            AnsiConsole.WriteLine($"Failed to update issue {result.Error.Message}");
        else
            AnsiConsole.WriteLine("Issue updated");

        AnsiConsole.Confirm("Press any key to continue...");
    }

    public async Task CloseIssueAsync()
    {
        var id = AnsiConsole.Ask<int>("Enter issue id:");

        var result = await _repositoryService.CloseIssueAsync(Repository, id);
        if (!result.IsSuccessful)
            AnsiConsole.WriteLine($"Failed to close issue {result.Error.Message}");
        else
            AnsiConsole.WriteLine("Issue closed");

        AnsiConsole.Confirm("Press any key to continue...");
    }

    public async Task ExportIssuesAsync()
    {
        var path = AnsiConsole.Ask<string>("Enter path to file:");
        AnsiConsole.WriteLine("Exporting...");
        try
        {
            var result = await _repositoryService.ExportIssuesAsync(Repository, path);
            if (!result.IsSuccessful)
                AnsiConsole.WriteLine($"Failed to close issue {result.Error.Message}");
            else
                AnsiConsole.WriteLine("Issues exported");
        }
        catch (Exception e)
        {
            AnsiConsole.WriteException(e);
        }

        AnsiConsole.Confirm("Press any key to continue...");
    }

    public async Task ImportIssuesAsync()
    {
        var path = AnsiConsole.Ask<string>("Enter path to file:");
        AnsiConsole.WriteLine("Importing...");
        try
        {
            var issues = await _repositoryService.ImportIssueToServiceAsync(Repository, path);
            var grid = new Grid();

            grid.AddColumn();
            grid.AddColumn();
            grid.AddColumn();
            grid.AddColumn();
            grid.AddColumn();

            grid.AddRow("Imported", nameof(Issue.Id), nameof(Issue.Name), nameof(Issue.Description), "Error");

            foreach (var issue in issues)
                grid.AddRow(issue.IsSuccessful.ToString(), issue.Data.Id.ToString(), issue.Data.Name,
                    issue.Data.Description, issue.Error?.Message ?? string.Empty);

            AnsiConsole.Write(grid);
        }
        catch (IssueManagerException e)
        {
            AnsiConsole.WriteLine($"Failed to import issues {e.Error.Message}");
        }
        catch (Exception e)
        {
            AnsiConsole.WriteException(e);
        }

        AnsiConsole.Confirm("Press any key to continue...");
    }
}