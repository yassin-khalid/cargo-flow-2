using CargoFlow.Domain.Enums;

namespace CargoFlow.Endpoints.Filters;

public class GetNotificationValidationFilter : IEndpointFilter
{
    private const string recipientTypeKey = "recipient_type";
    private const string recipientIdKey = "recipient_id";
    private const string shipmentIdKey = "shipment_id";
    private const string notificationTypeKey = "notification_type";
    // private const string titleKey = "title";
    // private const string messageKey = "message";
    private const string isReadKey = "is_read";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var query = context.HttpContext.Request.Query;
        var errors = new Dictionary<string, string[]>();

        CommonValidationFilter.ValidateIntFilter(query, recipientIdKey, errors);
        CommonValidationFilter.ValidateIntFilter(query, shipmentIdKey, errors);

        CommonValidationFilter.ValidateEnumFilter<NotificationType>(query, notificationTypeKey, errors);
        CommonValidationFilter.ValidateEnumFilter<RecipientType>(query, recipientTypeKey, errors);

        CommonValidationFilter.ValidateBoolFilter(query, isReadKey, errors);

        if (errors.Count > 0)
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
