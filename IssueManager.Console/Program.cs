using IssueManager.Console;
using IssueManager.Core.Entities;
using IssueManager.Core.Repositories;
using IssueManager.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;

using var host = Host.CreateDefaultBuilder(args)
                     .ConfigureServices((context, services) =>
                     {
                         var config = context.Configuration;
                         var github = config.GetSection("GitHub")
                                            .Get<GitHubOptions>();
                         services.Configure<GitHubOptions>(config.GetSection("GitHub"));
                         services.AddSingleton<RateLimitingHttpClientHandler>();

                         services.AddHttpClient<GithubRepository>(options =>
                                 {
                                     options.BaseAddress = new Uri("https://api.github.com/");
                                     options.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
                                     options.DefaultRequestHeaders.Add("Authorization", $"Bearer {github.ApiKey}");
                                     options.DefaultRequestHeaders.Add("User-Agent", github.AppName);
                                 })
                                 .AddHttpMessageHandler<RateLimitingHttpClientHandler>();

                         var gitLab = config.GetSection("GitLab")
                                            .Get<GitLabOptions>();
                         services.Configure<GitLabOptions>(config.GetSection("GitLab"));

                         services.AddHttpClient<GitLabRepository>(options =>
                         {
                             options.BaseAddress = new Uri("https://gitlab.com/api/v4/");
                             options.DefaultRequestHeaders.Add("Accept", "application/json");
                             options.DefaultRequestHeaders.Add("Authorization", $"Bearer {gitLab.ApiKey}");
                         });
                     })
                     .Build();

var state = new ProgramState(host.Services);

if (!state.SetRepositoryService())
    return;

state.SetRepository();

do
{
    AnsiConsole.Clear();
    AnsiConsole.WriteLine($"Selected service: {state.Service}");
    AnsiConsole.WriteLine($"Selected repository: {state.Repository}");
    AnsiConsole.WriteLine();

    var action = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Select action:")
                                                                 .PageSize(10)
                                                                 .AddChoices("Change service", "Change repository",
                                                                     "Add issue", "Update issue", "Close issue",
                                                                     "Get all issues", "Export issues", "Import issues",
                                                                     "Exit"));
    switch (action)
    {
        case "Change service":
            state.SetRepositoryService();
            break;
        case "Change repository":
            state.SetRepository();
            break;
        case "Add issue":
            await state.AddIssueAsync();
            break;
        case "Update issue":
            await state.UpdateIssueAsync();
            break;
        case "Close issue":
            await state.CloseIssueAsync();
            break;
        case "Get all issues":
            await state.GetIssuesAsync();
            break;
        case "Export issues":
            await state.ExportIssuesAsync();
            break;
        case "Import issues":
            await state.ImportIssuesAsync();
            break;
        case "Exit": return;
    }
} while (true);