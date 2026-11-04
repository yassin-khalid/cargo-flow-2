namespace CargoFlow.Endpoints.Filters;

public class ShipmentValidationFilter : IEndpointFilter
{
    private const string CustomerIdKey = "customer_id";
    private const string ShipmentStatusKey = "shipment_status";
    private const string PickupAddressKey = "pickup_address";
    private const string DestinationAddressKey = "destination_address";
    private const string PageKey = "page";
    private const string PageSizeKey = "page_size";

    private static readonly int[] AllowedPageSizes = [5, 10, 15, 25, 50, 100];
    
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var query = context.HttpContext.Request.Query;
        var errors = new Dictionary<string, string[]>();


        if (query.TryGetValue(PageKey, out var pageStr) && !string.IsNullOrWhiteSpace(pageStr))
        {
            if (!int.TryParse(pageStr, out var page) || page < 1)
            {
                errors[PageKey] = [$"{PageKey} must be a valid positive integer"];
            }
        }

        if (query.TryGetValue(PageSizeKey, out var pageSizeStr) && !string.IsNullOrWhiteSpace(pageSizeStr))
        {
            if (!int.TryParse(pageSizeStr, out var pageSize) || !AllowedPageSizes.Contains(pageSize))
            {
                errors[PageSizeKey] = [$"{PageSizeKey} must be one of: {string.Join(", ", AllowedPageSizes)}"];
            }
        }
        ValidateOptionalIntFilter(query, CustomerIdKey, errors);
        ValidateOptionalIntFilter(query, ShipmentStatusKey, errors);
        ValidateOptionalIntFilter(query, PickupAddressKey, errors);
        ValidateOptionalIntFilter(query, DestinationAddressKey, errors);

        if (errors is { Count: > 0 })
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Query Parameters Parsing Error",
                detail: "Error occur when trying to parse query parameters. See 'errors' for details",
                extensions: new Dictionary<string, object?>()
                {
                    ["errors"] = errors
                });
        }

        return await next(context);
    }

    private static void ValidateOptionalIntFilter(IQueryCollection query, string key, Dictionary<string, string[]> errors)
    {
        if (!query.TryGetValue(key, out var keyStr)) return;
        if (!int.TryParse(keyStr, out _))
        {
            errors[key] = [$"{key} must be a valid integer"];
        }
    }
}