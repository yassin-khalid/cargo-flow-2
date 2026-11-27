using System.Globalization;
using System.Text.Json;
using CargoFlow.Domain.Enums;

namespace CargoFlow.Endpoints.Filters;

public class CreateShipmentTrackingEventFilter : IEndpointFilter
{
    const string latitudeKey = "latitude";
    const string longitudeKey = "longitude";
    const string eventKey = "event_type";
    const string occuredAtKey = "occured_at";

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next
    )
    {
        var query = context.HttpContext.Request.Query;
        Dictionary<string, string[]> errors = [];

        if (query.TryGetValue(latitudeKey, out var latitudeStr))
        {
            if (!decimal.TryParse(latitudeStr, out var latitude) || latitude > 90 || latitude < -90)
            {
                errors[latitudeKey] = ["Latitude must be a decimal value between 90 and -90"];
            }
        }
        else
        {
            errors[latitudeKey] = ["Latitude parameter is required and must be provided"];
        }

        if (query.TryGetValue(longitudeKey, out var longitudeStr))
        {
            if (
                !decimal.TryParse(longitudeStr, out var longitude)
                || longitude > 180
                || longitude < -180
            )
            {
                errors[longitudeKey] = ["Longitude must be a decimal value between -180 and 180"];
            }
        }
        else
        {
            errors[longitudeKey] = ["Longitude paramiter is required and must be provided"];
        }

        if (query.TryGetValue(eventKey, out var eventStr) && !string.IsNullOrWhiteSpace(eventStr))
        {
            var events = Enum.GetNames<ShipmentEventType>()
                .Select(JsonNamingPolicy.SnakeCaseLower.ConvertName)
                .ToList();
            if (!events.Contains(eventStr!))
            {
                errors[eventKey] =
                [
                    $"Event Type must be one of ${string.Join(',', events)} values",
                ];
            }
        }
        else
        {
            errors[eventKey] = [$"Evnet Type is required and must be provided"];
        }

        if (query.TryGetValue(occuredAtKey, out var occuredAtStr))
        {
            if (
                !DateTime.TryParseExact(
                    occuredAtStr,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var occuredAt
                )
            )
            {
                errors[occuredAtKey] = [$"Occured At date time must be in 'yyyy-MM-dd' format"];
            }
        }
        else
        {
            errors[occuredAtKey] = ["Occured At Parameter is required and must be provided"];
        }

        if (errors.Count > 0)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Query Parameters Parsing Error",
                detail: "Error occur when trying to parse query parameters. See 'errors' for details",
                extensions: new Dictionary<string, object?>() { ["errors"] = errors }
            );
        }

        return await next(context);
    }
}
