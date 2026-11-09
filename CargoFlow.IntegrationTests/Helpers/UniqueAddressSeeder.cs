using CargoFlow.Domain;

namespace CargoFlow.IntegrationTests.Helpers;

public static class UniqueAddressSeeder
{

    public static (Address pickup, Address destination) GenerateUniqueAddresses()
    {
        var hash = Guid.NewGuid().GetHashCode();
        // Base coordinates (e.g., starting around Saudi Arabia / Red Sea region)
        // We add a tiny offset derived from the hash (e.g., +/- up to 5 degrees)
        var pickupLat = 21.4858M + ((hash % 1000) * 0.001M);
        var pickupLng = 39.1925M + (((hash / 1000) % 1000) * 0.001M);

        var destLat = 24.0895M + (((hash / 1000000) % 1000) * 0.001M);
        var destLng = 38.0618M + (((hash / 100000000) % 1000) * 0.001M);

        var pickup = new Address()
        {
            Latitude = pickupLat,
            Longitude = pickupLng
        };

        var destination = new Address()
        {
            Latitude = destLat,
            Longitude = destLng
        };

        return (pickup, destination);
    }
}
