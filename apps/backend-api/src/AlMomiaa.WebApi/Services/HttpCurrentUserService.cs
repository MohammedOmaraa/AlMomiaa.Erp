using System.Security.Claims;
using AlMomiaa.Application.Common.Abstractions;

namespace AlMomiaa.WebApi.Services;

public sealed class HttpCurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid UserId
    {
        get
        {
            var raw = Principal?.FindFirstValue("UserId") ?? Principal?.FindFirstValue("sub");
            return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
        }
    }

    public IReadOnlyList<string> RoleCodes
    {
        get
        {
            if (Principal is null)
            {
                return [];
            }

            var roleClaims = Principal.FindAll(ClaimTypes.Role).Select(c => c.Value)
                .Concat((Principal.FindFirstValue("RoleCodes") ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return roleClaims;
        }
    }

    public IReadOnlyList<string> Permissions =>
        Principal?.FindAll("permission").Select(c => c.Value).Distinct(StringComparer.Ordinal).ToArray() ?? [];

    public string? RepType
    {
        get
        {
            var raw = Principal?.FindFirstValue("RepType");
            return string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
        }
    }

    public int? DefaultWarehouseId =>
        int.TryParse(Principal?.FindFirstValue("DefaultWarehouseId"), out var id) ? id : null;

    public IReadOnlyList<int> AssignedWarehouseIds
    {
        get
        {
            var raw = Principal?.FindFirstValue("AssignedWarehouseIds");
            if (string.IsNullOrWhiteSpace(raw))
            {
                return [];
            }

            var list = new List<int>();
            foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (int.TryParse(part, out var id))
                {
                    list.Add(id);
                }
            }

            return list;
        }
    }

    public string IpAddress =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    public bool IsInRole(string roleCode) =>
        RoleCodes.Any(r => string.Equals(r, roleCode, StringComparison.OrdinalIgnoreCase));

    public bool HasPermission(string permission) =>
        Permissions.Any(p => string.Equals(p, permission, StringComparison.Ordinal));
}
