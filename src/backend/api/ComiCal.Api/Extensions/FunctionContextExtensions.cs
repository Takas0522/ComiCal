using ComiCal.Domain.Entities;
using ComiCal.Domain.Enums;
using Microsoft.Azure.Functions.Worker;
using System.Diagnostics.CodeAnalysis;

namespace ComiCal.Api.Extensions;

public static class FunctionContextExtensions
{
    public static User? GetResolvedUser(this FunctionContext ctx)
        => ctx.Items.TryGetValue("ResolvedUser", out var u) ? u as User : null;

    public static bool TryGetResolvedUser(this FunctionContext ctx, [NotNullWhen(true)] out User? user)
    {
        user = ctx.GetResolvedUser();
        return user is not null;
    }

    public static bool IsAdmin(this FunctionContext ctx)
    {
        var u = ctx.GetResolvedUser();
        return u?.Role == UserRole.Admin;
    }
}
