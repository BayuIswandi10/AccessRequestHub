namespace AccessRequestHub.Domain.Entities;

public class AuditEvent
{
    public Guid Id { get; set; }
    public Guid AccessRequestId { get; set; }
    public AccessRequest? AccessRequest { get; set; }
    public Guid ActorId { get; set; }
    public User? Actor { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime Timestamp { get; set; }
}
