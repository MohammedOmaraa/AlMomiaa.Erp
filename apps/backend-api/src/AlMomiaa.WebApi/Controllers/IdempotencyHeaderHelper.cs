using Microsoft.AspNetCore.Mvc;

namespace AlMomiaa.WebApi.Controllers;

internal static class IdempotencyHeaderHelper
{
    public const string HeaderName = "X-Idempotency-Key";

    public static bool TryParse(HttpRequest request, out Guid key, out IActionResult? errorResult)
    {
        if (!request.Headers.TryGetValue(HeaderName, out var values) ||
            !Guid.TryParse(values.FirstOrDefault(), out key) ||
            key == Guid.Empty)
        {
            key = Guid.Empty;
            errorResult = new BadRequestObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Missing or invalid idempotency key",
                Detail = $"Header '{HeaderName}' is required and must be a non-empty valid GUID.",
                Type = "https://api.almomiaa.sa/errors/idempotency-key-required",
                Instance = request.Path
            });
            return false;
        }

        errorResult = null;
        return true;
    }
}
