using System.Text.Json;
using CargoFlow.Domain.Enums;

namespace CargoFlow.Endpoints.Filters;

public class NotificationValidationFilter : IEndpointFilter
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

        ValidateOptionalIntFilter(query, recipientIdKey, errors);
        ValidateOptionalIntFilter(query, shipmentIdKey, errors);

        ValidateOptionalEnumFilter<NotificationType>(query, notificationTypeKey, errors);
        ValidateOptionalEnumFilter<RecipientType>(query, recipientTypeKey, errors);

        ValidateOptionalBoolFilter(query, isReadKey, errors);

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

    private static void ValidateOptionalIntFilter(IQueryCollection query, string key, Dictionary<string, string[]> errors)
    {
        if (query.TryGetValue(key, out var keyString))
        {
            if (!int.TryParse(keyString, out var keyValue) || keyValue < 1)
            {
                errors[key] = [$"{key} must be a valid positve integer"];
            }
        }
    }

    private static void ValidateOptionalEnumFilter<T>(IQueryCollection query, string key, Dictionary<string, string[]> errors) where T : struct, Enum
    {
        if (!query.TryGetValue(key, out var keyString) || string.IsNullOrWhiteSpace(keyString)) return;
        var values = Enum.GetNames<T>().Select(JsonNamingPolicy.SnakeCaseLower.ConvertName).ToList();
        if (!values.Contains(keyString!))
        {
            errors[key] = [$"{key} must be one of '{string.Join(", ", values)}' values"];
        }
    }

    private static void ValidateOptionalBoolFilter(IQueryCollection query, string key, Dictionary<string, string[]> errors)
    {
        if (query.TryGetValue(key, out var keyString))
        {
            if (!bool.TryParse(keyString, out _))
            {
                errors[key] = [$"{key} must be a boolean value"];
            }
        }
    }

}
