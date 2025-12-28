using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PropertyManager.Application.Common.Interfaces;

namespace PropertyManager.Infrastructure.Identity;

/// <summary>
/// Implementation of ICurrentUser that extracts user info from JWT claims.
/// Used by AppDbContext for tenant filtering.
/// </summary>
public class CurrentUserService : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("userId")?.Value
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }

    public Guid AccountId
    {
        get
        {
            var accountIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("accountId")?.Value;
            return Guid.TryParse(accountIdClaim, out var accountId) ? accountId : Guid.Empty;
        }
    }

    public string Role
    {
        get
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("role")?.Value
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value
                ?? string.Empty;
        }
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
