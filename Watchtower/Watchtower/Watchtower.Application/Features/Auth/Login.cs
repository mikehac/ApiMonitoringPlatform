using MediatR;
using Microsoft.EntityFrameworkCore;
using Watchtower.Application.Abstractions;
using Watchtower.Domain.Entities;

namespace Watchtower.Application.Features.Auth;

public record LoginCommand(string Email, string Password) : IRequest<AuthTokensResult>;

public record AuthTokensResult(string AccessToken, string RefreshToken, DateTime ExpiresAt);

public class LoginHandler : IRequestHandler<LoginCommand, AuthTokensResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;

    public LoginHandler(IApplicationDbContext db, IPasswordHasher hasher, IJwtTokenService jwt)
        => (_db, _hasher, _jwt) = (db, hasher, jwt);

    public async Task<AuthTokensResult> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), ct)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!_hasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        return await IssueTokens(user, ct);
    }

    internal async Task<AuthTokensResult> IssueTokens(User user, CancellationToken ct)
    {
        var (tokenValue, expiresAt) = _jwt.GenerateRefreshToken();
        var refreshToken = RefreshToken.Create(user.Id, tokenValue, expiresAt);
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(ct);

        return new AuthTokensResult(_jwt.GenerateAccessToken(user), tokenValue, expiresAt);
    }
}
