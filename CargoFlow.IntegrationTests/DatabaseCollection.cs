namespace CargoFlow.IntegrationTests;

[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<LocalSqlWebApplicationFactory<Program>>
{
    
}