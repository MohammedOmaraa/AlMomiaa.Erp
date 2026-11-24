using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AlMomiaa.WebApi.Hubs;

[Authorize]
public sealed class OperationsHub : Hub
{
    public static string UserGroup(Guid userId) => $"user:{userId:D}";
    public static string RoleGroup(string roleCode) => $"role:{roleCode.Trim().ToUpperInvariant()}";
    public static string WarehouseGroup(int warehouseId) => $"warehouse:{warehouseId}";

    public override async Task OnConnectedAsync()
    {
        var user = Context.User;
        if (user is not null)
        {
            var rawUserId = user.FindFirstValue("UserId") ?? user.FindFirstValue("sub");
            if (Guid.TryParse(rawUserId, out var userId) && userId != Guid.Empty)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
            }

            var roleCodes = user.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .Concat((user.FindFirstValue("RoleCodes") ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var roleCode in roleCodes)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, RoleGroup(roleCode));
            }

            var warehouseIds = new HashSet<int>();
            if (int.TryParse(user.FindFirstValue("DefaultWarehouseId"), out var defaultWh) && defaultWh > 0)
            {
                warehouseIds.Add(defaultWh);
            }

            var assignedRaw = user.FindFirstValue("AssignedWarehouseIds") ?? string.Empty;
            foreach (var part in assignedRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (int.TryParse(part, out var wid) && wid > 0)
                {
                    warehouseIds.Add(wid);
                }
            }

            foreach (var wid in warehouseIds)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, WarehouseGroup(wid));
            }
        }

        await base.OnConnectedAsync();
    }
}
