using KPKflowApi.Models.Authentication;
using System.Security.Claims;

namespace KPKflowApi.Repository
{
    public interface IJWTManagerRepository
    {
        Tokens Authenticate(UserDTO users);
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }
}
