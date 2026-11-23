using FluentValidation;

namespace AlMomiaa.Application.Authentication.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.Identifier)
            .NotEmpty()
            .MaximumLength(150);
        RuleFor(x => x.Request.Password)
            .NotEmpty()
            .MaximumLength(256);
    }
}
