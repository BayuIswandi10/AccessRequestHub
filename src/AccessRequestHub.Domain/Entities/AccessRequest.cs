using AccessRequestHub.Domain.Enums;

namespace AccessRequestHub.Domain.Entities;

public class AccessRequest
{
    public Guid Id { get; set; }
    public Guid ClientRequestId { get; set; }
    public Guid RequesterId { get; set; }
    public User? Requester { get; set; }
    public Guid ApplicationId { get; set; }
    public Application? Application { get; set; }
    public RequestEnvironment Environment { get; set; }
    public AccessLevel AccessLevel { get; set; }
    public RequestStatus Status { get; set; }
    public string BusinessJustification { get; set; } = string.Empty;
    public string PolicyVersion { get; set; } = "v1";
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public ICollection<AuditEvent> AuditEvents { get; set; } = new List<AuditEvent>();

    public bool IsHighRisk()
    {
        return Environment == RequestEnvironment.Production || AccessLevel == AccessLevel.Admin;
    }
}
