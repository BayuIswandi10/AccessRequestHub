namespace AccessRequestHub.Application.DTOs;

public class ApprovalActionDto
{
    public string RowVersion { get; set; } = string.Empty;
    public string? Reason { get; set; }
}
