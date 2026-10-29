using System.Runtime;
using CargoFlow.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CargoFlow.IntegrationTests;

public class LocalSqlWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>, IAsyncLifetime where TProgram : class
{
    private static readonly string _databaseName = $"TestDb_{Guid.NewGuid():N}";
    private static string TestConnectionString => $"Data Source=localhost,1433;Initial Catalog={_databaseName};User Id=sa;Password=YasKhal1125;TrustServerCertificate=True";
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {

            var descriptor = services
                .SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<CargoFlowDbContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<CargoFlowDbContext>(options => 
                options.UseSqlServer(TestConnectionString));
        });


    }

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<CargoFlowDbContext>()
            .UseSqlServer(TestConnectionString)
            .Options;

        await using var context = new CargoFlowDbContext(options);
        await context.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        var options = new DbContextOptionsBuilder<CargoFlowDbContext>()
            .UseSqlServer(TestConnectionString)
            .Options;

        await using var context = new CargoFlowDbContext(options);
        await context.Database.EnsureDeletedAsync();
    }
}