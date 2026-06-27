using MediatR;
using Microsoft.EntityFrameworkCore;
using Watchtower.Application.Abstractions;

namespace Watchtower.Application.Features.Auth;

public record ForgotPasswordCommand(string Email) : IRequest;

public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailService _email;

    public ForgotPasswordHandler(IApplicationDbContext db, IEmailService email)
        => (_db, _email) = (db, email);

    public async Task Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), ct);

        // Always return success to prevent email enumeration
        if (user is null) return;

        var token = Guid.NewGuid().ToString("N");
        user.SetPasswordResetToken(token, DateTime.UtcNow.AddHours(1));
        await _db.SaveChangesAsync(ct);

        await _email.SendPasswordResetAsync(user.Email, token, ct);
    }
}
