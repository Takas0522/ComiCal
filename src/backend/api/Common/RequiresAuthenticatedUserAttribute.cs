using System;

namespace ComiCal.Api.Common;

/// <summary>
/// Marker attribute applied to a Function class or method that requires an
/// authenticated SWA caller. <c>SwaAuthMiddleware</c> rejects anonymous calls
/// with HTTP 401 + RFC 7807 problem+json when this attribute is present.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RequiresAuthenticatedUserAttribute : Attribute
{
}
