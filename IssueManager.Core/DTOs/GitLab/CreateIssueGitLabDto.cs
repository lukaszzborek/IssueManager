using System.Text.Json.Serialization;

namespace IssueManager.Core.DTOs.GitLab;

public class CreateIssueGitLabDto
{
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    public CreateIssueGitLabDto(string title, string description)
    {
        Title = title;
        Description = description;
    }
}