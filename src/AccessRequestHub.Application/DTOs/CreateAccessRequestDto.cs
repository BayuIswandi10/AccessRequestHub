using AccessRequestHub.Domain.Enums;

namespace AccessRequestHub.Application.DTOs;

public class CreateAccessRequestDto
{
    public Guid ClientRequestId { get; set; }
    public Guid ApplicationId { get; set; }
    public RequestEnvironment Environment { get; set; }
    public AccessLevel AccessLevel { get; set; }
    public string BusinessJustification { get; set; } = string.Empty;
}
