using Microsoft.Extensions.Options;

namespace AlMomiaa.WebApi.Security;

public sealed class CorsSecurityOptions
{
    public const string SectionName = "Cors";
    public string[] AllowedOrigins { get; init; } = [];
}

public interface ITrustedOriginValidator
{
    bool IsTrustedOrigin(HttpRequest request);
}

public sealed class TrustedOriginValidator(IOptions<CorsSecurityOptions> options) : ITrustedOriginValidator
{
    private readonly HashSet<string> _allowedOrigins = new(
        options.Value.AllowedOrigins
            .Where(o => !string.IsNullOrWhiteSpace(o) && o != "*")
            .Select(NormalizeOrigin),
        StringComparer.OrdinalIgnoreCase);

    public bool IsTrustedOrigin(HttpRequest request)
    {
        var originHeader = request.Headers.Origin.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(originHeader))
        {
            return IsMatch(originHeader, request);
        }

        var refererHeader = request.Headers.Referer.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(refererHeader))
        {
            if (Uri.TryCreate(refererHeader, UriKind.Absolute, out var refererUri))
            {
                var refererOrigin = $"{refererUri.Scheme}://{refererUri.Authority}";
                return IsMatch(refererOrigin, request);
            }

            return false;
        }

        // Non-browser / direct same-host clients without Origin/Referer headers are permitted
        // when no cross-site fetch metadata header indicates a cross-site request.
        var fetchSite = request.Headers["Sec-Fetch-Site"].FirstOrDefault();
        return string.IsNullOrWhiteSpace(fetchSite)
            || fetchSite.Equals("same-origin", StringComparison.OrdinalIgnoreCase)
            || fetchSite.Equals("none", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsMatch(string candidateOrigin, HttpRequest request)
    {
        var normalized = NormalizeOrigin(candidateOrigin);
        if (_allowedOrigins.Contains(normalized))
        {
            return true;
        }

        if (request.Host.HasValue)
        {
            var selfOrigin = NormalizeOrigin($"{request.Scheme}://{request.Host.Value}");
            if (string.Equals(normalized, selfOrigin, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeOrigin(string origin) => origin.Trim().TrimEnd('/');
}
