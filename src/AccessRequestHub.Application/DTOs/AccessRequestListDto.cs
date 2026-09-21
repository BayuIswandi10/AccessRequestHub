using AccessRequestHub.Domain.Enums;

namespace AccessRequestHub.Application.DTOs;

public class AccessRequestListDto
{
    public Guid Id { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
    public RequestEnvironment Environment { get; set; }
    public AccessLevel AccessLevel { get; set; }
    public RequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string RequesterName { get; set; } = string.Empty;
}
