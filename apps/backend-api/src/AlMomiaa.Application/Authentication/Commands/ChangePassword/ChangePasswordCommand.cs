using AlMomiaa.Application.Authentication.Abstractions;
using FluentValidation;
using MediatR;

namespace AlMomiaa.Application.Authentication.Commands.ChangePassword;

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword);

public sealed record ChangePasswordCommand(
    Guid UserId,
    ChangePasswordRequest Request) : IRequest<Unit>;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");
        RuleFor(x => x.Request.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("New password must be at least 8 characters long.")
            .MaximumLength(128).WithMessage("New password must not exceed 128 characters.")
            .Must((cmd, newPwd) => !string.Equals(newPwd, cmd.Request.CurrentPassword, StringComparison.Ordinal))
            .WithMessage("New password must be different from the current password.");
        RuleFor(x => x.Request.ConfirmNewPassword)
            .Equal(x => x.Request.NewPassword)
            .WithMessage("Password confirmation does not match the new password.");
    }
}

public sealed class ChangePasswordCommandHandler(IAuthenticationService authService)
    : IRequestHandler<ChangePasswordCommand, Unit>
{
    public async Task<Unit> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        await authService.ChangePasswordAsync(command.UserId, command.Request, cancellationToken);
        return Unit.Value;
    }
}
