using MediatR;
using Microsoft.EntityFrameworkCore;
using Watchtower.Application.Abstractions;

namespace Watchtower.Application.Features.Auth;

public record ForgotPasswordCommand(string Email) : IRequest<string?>;

public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand, string?>
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailService _email;

    public ForgotPasswordHandler(IApplicationDbContext db, IEmailService email)
        => (_db, _email) = (db, email);

    public async Task<string?> Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), ct);

        if (user is null) return null;

        var token = Guid.NewGuid().ToString("N");
        user.SetPasswordResetToken(token, DateTime.UtcNow.AddHours(1));
        await _db.SaveChangesAsync(ct);

        await _email.SendPasswordResetAsync(user.Email, token, ct);
        return token;
    }
}
