using CargoFlow.Data;
using CargoFlow.Domain;
using CargoFlow.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CargoFlow.IntegrationTests;

public class LocalSqlWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>, IAsyncLifetime where TProgram : class
{
    private static string _testDbPassword = Environment.GetEnvironmentVariable("TEST_DB_PASSWORD") ?? throw new Exception("Environment variable 'TEST_DB_PASSWORD' is missing");
    private static string _testDbName = $"TestDb_{Guid.NewGuid():N}";
    private static string TestConnectionString => $"Data Source=localhost,1433;Initial Catalog={_testDbName};User Id=sa;Password={_testDbPassword};TrustServerCertificate=True";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("JwtSettings:Secret", "AAiy9dgdZbARx+27g2mDOnRiAmr44nasuzadUqd45lg=");
        builder.UseSetting("JwtSettings:Audience", "test");
        builder.UseSetting("JwtSettings:Issuer", "test");
        builder.UseSetting("JwtSettings:ExpiryMinutes", "60");

        builder.ConfigureServices(services =>
            {
                var desciptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<CargoFlowDbContext>));
                if (desciptor is not null)
                {
                    services.Remove(desciptor);
                }

                services.AddDbContext<CargoFlowDbContext>(options => options.UseSqlServer(TestConnectionString));
            }
        );
        builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                    {
                        options.DefaultScheme = TestAuthenticationHandler.SchemeName;
                        options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                        options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                    })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.SchemeName, options =>
                    {
                    });
            }
        );
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await db.Database.MigrateAsync();
        await TestDbSeeder.SeedAsync(db, userManager);
    }

    public new async Task DisposeAsync()
    {
        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();
            await db.Database.EnsureDeletedAsync();
        }

        await base.DisposeAsync();
    }

    public async Task<(HttpClient client, ApplicationUser user, int profileId)> CreateClientForAsync(string email, string role)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var user = await userManager.FindByEmailAsync(email) ?? throw new Exception($"{email} not seeded");

        var profile = await db.Set<UserProfile>()
            .FirstOrDefaultAsync(p => p.UserId == user.Id)
                ?? throw new Exception("User not found");

        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, user.Id);
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, role);
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.ProfileIdHeader, profile.Id.ToString());

        return (client, user, profile.Id);
    }

    public async Task<T> QueryDbAsync<T>(Func<CargoFlowDbContext, Task<T>> query)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();
        return await query(db);
    }

}
