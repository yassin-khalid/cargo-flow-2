using System.Text.Json;
using CargoFlow.Dtos;

namespace CargoFlow.Endpoints.Filters;

public class ShipmentValidationFilter : IEndpointFilter
{
    private const string CustomerIdKey = "customer_id";
    private const string ShipmentStatusKey = "shipment_status";
    private const string PickupAddressKey = "pickup_address";
    private const string DestinationAddressKey = "destination_address";
    private const string PageKey = "page";
    private const string PageSizeKey = "page_size";
    private const string SortOrderKey = "sort_order";
    private const string SortByKey = "sort_by";

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

        CommonValidationFilter.ValidateEnumFilter<ShipmentSortBy>(query, SortByKey, errors);
        CommonValidationFilter.ValidateEnumFilter<ShipmentSortDirection>(query, SortOrderKey, errors);

        CommonValidationFilter.ValidateIntFilter(query, CustomerIdKey, errors);
        CommonValidationFilter.ValidateIntFilter(query, ShipmentStatusKey, errors);
        CommonValidationFilter.ValidateIntFilter(query, PickupAddressKey, errors);
        CommonValidationFilter.ValidateIntFilter(query, DestinationAddressKey, errors);

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
}
