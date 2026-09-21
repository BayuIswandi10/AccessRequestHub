namespace AccessRequestHub.Domain.Entities;

public class Application
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid SystemOwnerId { get; set; }
    public User? SystemOwner { get; set; }
}
