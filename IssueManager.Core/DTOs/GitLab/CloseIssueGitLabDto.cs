using System.Text.Json.Serialization;

namespace IssueManager.Core.DTOs.GitLab;

public class CloseIssueGitLabDto
{
    [JsonPropertyName("state_event")]
    public string StateEvent { get; set; }

    public CloseIssueGitLabDto(string stateEvent)
    {
        StateEvent = stateEvent;
    }
}