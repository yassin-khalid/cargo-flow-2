using CargoFlow.Data;
using CargoFlow.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace CargoFlow.Endpoints;

public static class WeightTypeEndpoints
{
   public static RouteGroupBuilder MapWeightTypeEndpoints(this RouteGroupBuilder group)
   {
      group.MapGet("/", GetWeightTypes);
      group.MapGet("/{id:int}", GetWeightType);
      return group;
   }

   private static async Task<Ok<List<WeightTypeDto>>> GetWeightTypes(CargoFlowDbContext db)
   {
      return TypedResults.Ok(await db.WeightTypes
         .AsNoTracking()
         .Select(w => new WeightTypeDto(w.Id, w.Name))
         .ToListAsync());
   }

   private static async Task<Results<Ok<WeightTypeDto>, NotFound>> GetWeightType(CargoFlowDbContext db, int id)
   {
      return await db.WeightTypes
            .AsNoTracking()
            .Select(w => new WeightTypeDto(w.Id, w.Name))
            .FirstOrDefaultAsync(w => w.Id == id)switch
      {
         { } weightType => TypedResults.Ok(weightType),
         _ => TypedResults.NotFound()
      };
   }
}