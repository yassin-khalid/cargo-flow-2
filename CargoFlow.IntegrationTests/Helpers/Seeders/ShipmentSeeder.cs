// using CargoFlow.Data;
// using CargoFlow.Domain;
// using CargoFlow.Domain.Enums;
// using CargoFlow.Dtos;
// using Microsoft.AspNetCore.Identity;
// using Microsoft.EntityFrameworkCore;

// namespace CargoFlow.IntegrationTests.Helpers.Seeders;

// public record OfferSpec(string CompanyName, decimal Price, OfferStateName StateName, bool HasWon = false);

// public record ShipmentSpec(ShipmentStatusName ShipmentStatus, string WeightType, decimal Weight);
// public static class ShipmentSeeder
// {

//     public static async Task<CreateShipmentRequestDto> GetValidCreateShipmentDtoAsync(
//         CargoFlowDbContext db,
//         UserManager<ApplicationUser> userManager)
//     {
//         using var transaction = await db.Database.BeginTransactionAsync();
//         try
//         {
//             var uniqueSuffix = Guid.NewGuid().ToString()[..8];
//             var user = new ApplicationUser
//             {
//                 Email = $"test-{uniqueSuffix}@test.com",
//                 UserName = $"test-{uniqueSuffix}"
//             };

//             var result = await userManager.CreateAsync(user, "test@T123");
//             if (!result.Succeeded)
//             {
//                 throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
//             }

//             var (pickup, destination) = UniqueAddressSeeder.GenerateUniqueAddresses();

//             db.Addresses.AddRange(pickup, destination);


//             var customer = new Customer()
//             {
//                 Name = $"Cust-{uniqueSuffix}",
//                 NationalId = $"Nat-{uniqueSuffix[..6]}",
//                 UserId = user.Id
//             };
//             db.Customers.Add(customer);

//             var weightType = new WeightType()
//             {
//                 Name = "Standard"
//             };

//             db.WeightTypes.Add(weightType);

//             await db.SaveChangesAsync();
//             await transaction.CommitAsync();

//             return new CreateShipmentRequestDto(customer.Id, pickup.Id, destination.Id, 20, weightType.Id);
//         }
//         catch (Exception)
//         {
//             await transaction.RollbackAsync();
//             throw;
//         }
//     }
//     public static async Task<(Shipment, List<Offer>)> SeedShipmentWithOffersAsync(
//         CargoFlowDbContext db,
//         UserManager<ApplicationUser> userManager,
//         ShipmentStatusName shipmentStatus,
//         string weightType,
//         decimal weight,
//         OfferSpec[] offerSpecs)
//     {
//         using var transaction = await db.Database.BeginTransactionAsync();
//         try
//         {

//             var uniqueSuffix = Guid.NewGuid().ToString()[..8];
//             var user = new ApplicationUser
//             {
//                 Email = $"test-{uniqueSuffix}@test.com",
//                 UserName = $"test-{uniqueSuffix}"
//             };

//             var result = await userManager.CreateAsync(user, "Test@test123");
//             if (!result.Succeeded)
//             {
//                 throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
//             }

//             var targetStatus = await db.ShipmentStatuses.FirstAsync(ss => ss.Name == shipmentStatus);
//             var targetType = await db.WeightTypes.FirstAsync(wt => wt.Name == weightType);

//             var (pickup, destination) = UniqueAddressSeeder.GenerateUniqueAddresses();

//             db.Addresses.AddRange(pickup, destination);


//             var customer = new Customer()
//             {
//                 Name = $"Cust-{uniqueSuffix}",
//                 NationalId = $"Nat-{uniqueSuffix[..6]}",
//                 UserId = user.Id
//             };
//             db.Customers.Add(customer);

//             await db.SaveChangesAsync();

//             var shipment = new Shipment()
//             {
//                 DestinationId = destination.Id,
//                 PickupId = pickup.Id,
//                 Weight = weight,
//                 CustomerId = customer.Id,
//                 WeightTypeId = targetType.Id,
//                 ShipmentStatusId = targetStatus.Id,
//             };

//             db.Shipments.Add(shipment);
//             await db.SaveChangesAsync();

//             List<Offer> offers = [];
//             for (var i = 0; i < offerSpecs.Length; i++)
//             {
//                 var spec = offerSpecs[i];
//                 var subSuffix = $"{uniqueSuffix}-{i}";

//                 var innerUser = new ApplicationUser
//                 {
//                     Email = $"test-{uniqueSuffix}{i}@test.com",
//                     UserName = $"test-{uniqueSuffix}{i}"
//                 };

//                 var innerResult = await userManager.CreateAsync(innerUser, "Test@test123");

//                 if (!innerResult.Succeeded)
//                 {
//                     throw new Exception("Unable to create user");
//                 }

//                 var company = new Company()
//                 {
//                     Name = $"{spec.CompanyName} {subSuffix}".Trim()[..Math.Min(40, $"{spec.CompanyName} {subSuffix}".Length)],
//                     TaxID = $"TAX-{subSuffix}".Trim()[..Math.Min(15, $"TAX-{subSuffix}".Length)],
//                     Email = $"test-{subSuffix}@company.com",
//                     UserId = innerUser.Id
//                 };

//                 db.Companies.Add(company);
//                 await db.SaveChangesAsync();

//                 var offerState = await db.OfferStates.FirstAsync(os => os.Name == spec.StateName);
//                 var offer = new Offer()
//                 {
//                     CompanyId = company.Id,
//                     HasWon = spec.HasWon,
//                     OfferStateId = offerState.Id,
//                     Price = spec.Price,
//                     ShipmentId = shipment.Id,
//                     SubmittedDate = DateTime.UtcNow.AddMinutes(-i),
//                     ExpirationDate = DateTime.UtcNow.AddMinutes(-i).AddDays(7)
//                 };
//                 db.Offers.Add(offer);
//                 await db.SaveChangesAsync();
//                 offers.Add(offer);
//             }

//             await transaction.CommitAsync();
//             return (shipment, offers);
//         }
//         catch (Exception)
//         {
//             await transaction.RollbackAsync();
//             throw;
//         }
//     }

//     public static async Task<Dictionary<ShipmentStatusName, Shipment>> SeedShipmentListWithUniqueStatusAsync(
//         CargoFlowDbContext db,
//         UserManager<ApplicationUser> userManager,
//         ShipmentSpec[] shipmentSpecs,
//         CancellationToken ct = default
//     )
//     {
//         if (shipmentSpecs is { Length: 0 })
//         {
//             return [];
//         }

//         var duplicateStatuses = shipmentSpecs
//             .GroupBy(s => s.ShipmentStatus)
//             .Where(g => g.Count() > 1)
//             .Select(g => g.Key)
//             .ToList();

//         if (duplicateStatuses is { Count: > 0 })
//         {
//             throw new ArgumentException(
//                 $"ShipmentStatus must be unique. Duplicates found: {string.Join(", ", duplicateStatuses)}",
//                 nameof(shipmentSpecs));
//         }

//         using var transaction = await db.Database.BeginTransactionAsync(ct);
//         try
//         {

//             var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];

//             var statusMap = await db.ShipmentStatuses
//                 .ToDictionaryAsync(ss => ss.Name, ct);

//             var weightTypeMap = await db.WeightTypes
//                 .ToDictionaryAsync(wt => wt.Name, ct);

//             List<Shipment> created = [];
//             for (var i = 0; i < shipmentSpecs.Length; i++)
//             {
//                 var spec = shipmentSpecs[i];

//                 if (!statusMap.TryGetValue(spec.ShipmentStatus, out var targetStatus))
//                 {
//                     throw new ArgumentException($"Unknown status: {spec.ShipmentStatus}");
//                 }

//                 if (!weightTypeMap.TryGetValue(spec.WeightType, out var targetType))
//                 {
//                     throw new ArgumentException($"Unknown weight type: {spec.WeightType}");
//                 }

//                 var user = new ApplicationUser
//                 {
//                     Email = $"test-{uniqueSuffix}{i}@test.com",
//                     UserName = $"test-{uniqueSuffix}{i}"
//                 };

//                 var result = await userManager.CreateAsync(user, "Test@test123");
//                 if (!result.Succeeded)
//                 {
//                     throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
//                 }

//                 var (pickup, destination) = UniqueAddressSeeder.GenerateUniqueAddresses();

//                 db.Addresses.AddRange(pickup, destination);

//                 var subSuffix = $"{uniqueSuffix}-{i}";

//                 var customer = new Customer()
//                 {
//                     Name = $"Cust-{subSuffix}",
//                     NationalId = $"Nat-{i}-{subSuffix[..4]}",
//                     UserId = user.Id
//                 };
//                 db.Customers.Add(customer);

//                 await db.SaveChangesAsync(ct);

//                 var shipment = new Shipment()
//                 {
//                     DestinationId = destination.Id,
//                     PickupId = pickup.Id,
//                     Weight = spec.Weight,
//                     CustomerId = customer.Id,
//                     WeightTypeId = targetType.Id,
//                     ShipmentStatusId = targetStatus.Id,
//                 };

//                 db.Shipments.Add(shipment);
//                 created.Add(shipment);
//             }

//             await db.SaveChangesAsync(ct);
//             await transaction.CommitAsync(ct);

//             return await db.Shipments
//                 .Where(s => created.Select(c => c.Id).Contains(s.Id))
//                 .Include(s => s.ShipmentStatus)
//                 .ToDictionaryAsync(s => s.ShipmentStatus.Name, cancellationToken: ct);
//         }
//         catch (Exception)
//         {
//             await transaction.RollbackAsync(ct);
//             throw;
//         }

//     }

//     public static async Task<(Shipment, List<ShipmentStatusHistory>)> SeedShipmentWithStatusHistoryAsync(
//         CargoFlowDbContext db,
//         UserManager<ApplicationUser> userManager,
//         ShipmentStatusName shipmentStatus,
//         string weightType,
//         decimal weight,
//         ShipmentStatusName[] shipmentStatusNames
//         )
//     {
//         using var transaction = await db.Database.BeginTransactionAsync();
//         try
//         {
//             var targetStatus = await db.ShipmentStatuses.FirstAsync(ss => ss.Name == shipmentStatus);
//             var targetType = await db.WeightTypes.FirstAsync(wt => wt.Name == weightType);

//             var (pickup, destination) = UniqueAddressSeeder.GenerateUniqueAddresses();

//             db.Addresses.AddRange(pickup, destination);

//             var uniqueSuffix = Guid.NewGuid().ToString()[..8];

//             var user = new ApplicationUser
//             {
//                 Email = $"test-{uniqueSuffix}@test.com",
//                 UserName = $"test-{uniqueSuffix}"
//             };

//             var result = await userManager.CreateAsync(user, "Test@test123");
//             if (!result.Succeeded)
//             {
//                 throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
//             }

//             var customer = new Customer()
//             {
//                 Name = $"Cust-{uniqueSuffix}",
//                 NationalId = $"Nat-{uniqueSuffix[..6]}",
//                 UserId = user.Id
//             };
//             db.Customers.Add(customer);

//             await db.SaveChangesAsync();

//             var shipment = new Shipment()
//             {
//                 DestinationId = destination.Id,
//                 PickupId = pickup.Id,
//                 Weight = weight,
//                 CustomerId = customer.Id,
//                 WeightTypeId = targetType.Id,
//                 ShipmentStatusId = targetStatus.Id,
//             };

//             db.Shipments.Add(shipment);
//             await db.SaveChangesAsync();

//             List<int> shipmentStatusHistoryIds = [];
//             foreach (var name in shipmentStatusNames)
//             {
//                 var shipmentStatusHistory = new ShipmentStatusHistory()
//                 {
//                     DateTime = DateTime.UtcNow,
//                     ShipmentId = shipment.Id,
//                     ShipmentStatusId = await db.ShipmentStatuses
//                         .Where(ss => ss.Name == name)
//                         .Select(ss => ss.Id)
//                         .FirstAsync()
//                 };

//                 db.ShipmentStatusHistories.Add(shipmentStatusHistory);
//                 await db.SaveChangesAsync();
//                 shipmentStatusHistoryIds.Add(shipmentStatusHistory.Id);
//             }

//             await transaction.CommitAsync();

//             var shipmentStatusHistories = await db.ShipmentStatusHistories
//                 .Include(ssh => ssh.ShipmentStatus)
//                 .Where(ssh => shipmentStatusHistoryIds.Contains(ssh.Id))
//                 .ToListAsync();

//             return (shipment, shipmentStatusHistories);
//         }
//         catch (Exception)
//         {
//             await transaction.RollbackAsync();
//             throw;
//         }

//     }
// }
