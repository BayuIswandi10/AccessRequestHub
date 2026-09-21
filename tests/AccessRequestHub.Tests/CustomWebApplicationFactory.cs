using AccessRequestHub.Infrastructure.Data;
using AccessRequestHub.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AccessRequestHub.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AccessRequestDbContext>));

            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<AccessRequestDbContext>(options =>
                options.UseSqlServer($"Server=(localdb)\\mssqllocaldb;Database=AccessRequestHub_Test_{_dbName};Trusted_Connection=True;MultipleActiveResultSets=true"));

            services.AddScoped<DbContext>(provider =>
                provider.GetRequiredService<AccessRequestDbContext>());

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AccessRequestDbContext>();
            db.Database.EnsureCreated();
            DbInitializer.Initialize(db);
        });
    }

    public override async ValueTask DisposeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AccessRequestDbContext>();
        await db.Database.EnsureDeletedAsync();
        await base.DisposeAsync();
    }
}
