using AccessRequestHub.Domain.Enums;

namespace AccessRequestHub.Application.DTOs;

public class AccessRequestDetailDto
{
    public Guid Id { get; set; }
    public Guid ClientRequestId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string RequesterEmail { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public Guid ApplicationId { get; set; }
    public RequestEnvironment Environment { get; set; }
    public AccessLevel AccessLevel { get; set; }
    public RequestStatus Status { get; set; }
    public string BusinessJustification { get; set; } = string.Empty;
    public string PolicyVersion { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string RowVersion { get; set; } = string.Empty;
    public List<AuditEventDto> AuditEvents { get; set; } = new();
}
