namespace AccessRequestHub.Application.DTOs;

public class AuditEventDto
{
    public string ActorName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime Timestamp { get; set; }
}
