using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Watchtower.Application.Abstractions;
using Watchtower.Domain.Entities;
using Watchtower.Domain.Exceptions;

namespace Watchtower.Application.Features.Auth;

public record RegisterUserCommand(string Email, string Password, string DisplayName)
    : IRequest<RegisterUserResult>;

public record RegisterUserResult(Guid UserId, string Email);

public class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(100);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
    }
}

public class RegisterUserHandler : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IEmailService _email;

    public RegisterUserHandler(IApplicationDbContext db, IPasswordHasher hasher, IEmailService email)
        => (_db, _hasher, _email) = (db, hasher, email);

    public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        var exists = await _db.Users.AnyAsync(u => u.Email == request.Email.ToLowerInvariant(), ct);
        if (exists)
            throw new ConflictException($"Email '{request.Email}' is already registered.");

        var user = User.Create(request.Email, _hasher.Hash(request.Password), request.DisplayName);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        await _email.SendEmailVerificationAsync(user.Email, user.EmailVerificationToken!, ct);

        return new RegisterUserResult(user.Id, user.Email);
    }
}
