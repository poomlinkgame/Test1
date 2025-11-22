using System.Security.Claims;
using WebAuthen.Models;

namespace WebAuthen.App_code;

public static class ClaimsPrincipalExtensions
{
    public static AuthorizationDto? GetAuthorization(this ClaimsPrincipal user)
    {
        var id = user.FindFirst("id")?.Value;
        var constr = user.FindFirst("constr")?.Value;
        var sysstr = user.FindFirst("sysstr")?.Value;

        if (string.IsNullOrEmpty(id)
         || string.IsNullOrEmpty(constr)
         || string.IsNullOrEmpty(sysstr))
            return null;

        return new AuthorizationDto(
            int.Parse(id!),
            constr!,
            sysstr!
        );
    }
}

