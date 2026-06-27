using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Watchtower.Application.Abstractions;
using Watchtower.Domain.Exceptions;

namespace Watchtower.Application.Features.Auth;

public record ResetPasswordCommand(string Token, string NewPassword) : IRequest;

public class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(100);
    }
}

public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;

    public ResetPasswordHandler(IApplicationDbContext db, IPasswordHasher hasher)
        => (_db, _hasher) = (db, hasher);

    public async Task Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token, ct)
            ?? throw new NotFoundException("Password reset token", request.Token);

        if (user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
            throw new DomainException("Password reset token has expired.");

        user.ResetPassword(_hasher.Hash(request.NewPassword));
        await _db.SaveChangesAsync(ct);
    }
}
