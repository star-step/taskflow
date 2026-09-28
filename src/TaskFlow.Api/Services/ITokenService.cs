using TaskFlow.Api.Domain;

namespace TaskFlow.Api.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateAccessToken(AppUser user);
}
