using AccessRequestHub.Domain.Entities;
using AppEntity = AccessRequestHub.Domain.Entities.Application;

namespace AccessRequestHub.Infrastructure.Data;

public static class DbInitializer
{
    public static readonly Guid AliceId = Guid.Parse("a1111111-1111-1111-1111-111111111111");
    public static readonly Guid BobId = Guid.Parse("b2222222-2222-2222-2222-222222222222");
    public static readonly Guid CarolId = Guid.Parse("c3333333-3333-3333-3333-333333333333");
    public static readonly Guid DanaId = Guid.Parse("d4444444-4444-4444-4444-444444444444");
    public static readonly Guid ErinId = Guid.Parse("e5555555-5555-5555-5555-555555555555");
    public static readonly Guid CrmAppId = Guid.Parse("aa111111-1111-1111-1111-111111111111");
    public static readonly Guid FinanceAppId = Guid.Parse("bb222222-2222-2222-2222-222222222222");

    public static void Initialize(AccessRequestDbContext context)
    {
        context.Database.EnsureCreated();

        if (!context.Users.Any())
        {
            var alice = new User { Id = AliceId, Name = "Alice", Email = "alice@example.local" };
            var bob = new User { Id = BobId, Name = "Bob", Email = "bob@example.local" };
            var carol = new User { Id = CarolId, Name = "Carol", Email = "carol@example.local" };
            var dana = new User { Id = DanaId, Name = "Dana", Email = "dana@example.local" };
            var erin = new User { Id = ErinId, Name = "Erin", Email = "erin@example.local" };

            alice.ManagerId = BobId;

            context.Users.AddRange(alice, bob, carol, dana, erin);
            context.SaveChanges();
        }

        if (!context.Applications.Any())
        {
            var crm = new AppEntity { Id = CrmAppId, Name = "CRM", SystemOwnerId = CarolId };
            var finance = new AppEntity { Id = FinanceAppId, Name = "Finance Portal", SystemOwnerId = DanaId };

            context.Applications.AddRange(crm, finance);
            context.SaveChanges();
        }
    }
}
