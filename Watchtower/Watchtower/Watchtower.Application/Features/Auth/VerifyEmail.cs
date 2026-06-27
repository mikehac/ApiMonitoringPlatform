using MediatR;
using Microsoft.EntityFrameworkCore;
using Watchtower.Application.Abstractions;
using Watchtower.Domain.Exceptions;

namespace Watchtower.Application.Features.Auth;

public record VerifyEmailCommand(string Token) : IRequest;

public class VerifyEmailHandler : IRequestHandler<VerifyEmailCommand>
{
    private readonly IApplicationDbContext _db;

    public VerifyEmailHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(VerifyEmailCommand request, CancellationToken ct)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.EmailVerificationToken == request.Token, ct)
            ?? throw new NotFoundException("Email verification token", request.Token);

        user.VerifyEmail();
        await _db.SaveChangesAsync(ct);
    }
}
