using System.Security.Claims;
using System.Text.Json;

namespace CargoFlow.IntegrationTests;

public record TestClaim(string Type, string Value);
public class TestUser
{
    public List<TestClaim> TestClaims { get; } = [];

    public static TestUser ForCustomer(string userId, string customerId, params string[] roles)
    {
        return Build(userId, "Customer", "customer_id", customerId, roles.Length > 0 ? roles : ["Customer"]);
    }

    public static TestUser ForCompany(string userId, string companyId, params string[] roles)
    {
        return Build(userId, "Company", "company_id", companyId, roles.Length > 0 ? roles : ["Company"]);
    }

    private static TestUser Build(string userId, string userType, string idClaimType, string profileId, string[] roles)
    {
        var testUser = new TestUser();
        testUser.TestClaims.Add(new(ClaimTypes.NameIdentifier, userId));
        testUser.TestClaims.Add(new(ClaimTypes.Email, $"{userId}@test.com"));
        testUser.TestClaims.Add(new("user_type", userType));
        testUser.TestClaims.Add(new(idClaimType, profileId));
        testUser.TestClaims.AddRange(roles.Select(r => new TestClaim(ClaimTypes.Role, r)));
        return testUser;
    }

    public TestUser Without(string type)
    {
        TestClaims.RemoveAll(c => c.Type == type);
        return this;
    }

    public TestUser With(string type, string value)
    {
        TestClaims.Add(new(type, value));
        return this;
    }

    public string ToToken() => Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(TestClaims));
}
