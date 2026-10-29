using Microsoft.AspNetCore.Http.HttpResults;

namespace CargoFlow.Helpers;

public static class HttpResultProblems
{
    public static ValidationProblem ValidationProblem(string field, string[] messages)
    {
        return TypedResults.ValidationProblem(
            errors: new Dictionary<string, string[]>
            {
                [field] = messages
            });
    }

    public static ProblemHttpResult NotFoundProblem(string field, int id)
    {
        return TypedResults.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Not Found",
            detail: $"{field} => {id} not found"
            );
    }
}
