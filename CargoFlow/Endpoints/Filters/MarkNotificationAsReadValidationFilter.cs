using System.Net;
using CargoFlow.Domain.Enums;

namespace CargoFlow.Endpoints.Filters;

public class MarkNotificationAsReadValidationFilter : IEndpointFilter
{
    private const string recipientIdKey = "recipient_id";
    private const string recipientTypeKey = "recipient_type";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var query = context.HttpContext.Request.Query;
        var errors = new Dictionary<string, string[]>();

        CommonValidationFilter.ValidateIntFilter(query, recipientIdKey, errors, ValidationType.Required);
        CommonValidationFilter.ValidateEnumFilter<RecipientType>(query, recipientTypeKey, errors, ValidationType.Required);

        if (errors is { Count: > 0 })
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Query Parameters Parsing Error",
                detail: "Error occur when trying to parse query parameters. See 'errors' for details",
                extensions: new Dictionary<string, object?>()
                {
                    ["errors"] = errors
                }
            );
        }
        return await next(context);
    }
}
