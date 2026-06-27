using Watchtower.Domain.Entities;

namespace Watchtower.Application.Abstractions;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    (string Token, DateTime ExpiresAt) GenerateRefreshToken();
}
