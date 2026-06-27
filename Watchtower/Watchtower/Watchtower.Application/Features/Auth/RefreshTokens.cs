using MediatR;
using Microsoft.EntityFrameworkCore;
using Watchtower.Application.Abstractions;

namespace Watchtower.Application.Features.Auth;

public record RefreshTokensCommand(string Token) : IRequest<AuthTokensResult>;

public class RefreshTokensHandler : IRequestHandler<RefreshTokensCommand, AuthTokensResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IJwtTokenService _jwt;

    public RefreshTokensHandler(IApplicationDbContext db, IJwtTokenService jwt)
        => (_db, _jwt) = (db, jwt);

    public async Task<AuthTokensResult> Handle(RefreshTokensCommand request, CancellationToken ct)
    {
        var existing = await _db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == request.Token, ct)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (!existing.IsValid)
            throw new UnauthorizedAccessException("Refresh token has expired or been revoked.");

        existing.Revoke();

        var (tokenValue, expiresAt) = _jwt.GenerateRefreshToken();
        var newToken = Domain.Entities.RefreshToken.Create(existing.UserId, tokenValue, expiresAt);
        _db.RefreshTokens.Add(newToken);
        await _db.SaveChangesAsync(ct);

        return new AuthTokensResult(_jwt.GenerateAccessToken(existing.User), tokenValue, expiresAt);
    }
}
