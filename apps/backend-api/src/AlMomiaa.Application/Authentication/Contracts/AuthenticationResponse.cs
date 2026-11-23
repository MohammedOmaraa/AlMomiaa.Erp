namespace AlMomiaa.Application.Authentication.Contracts;

public sealed record AuthenticationResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    AuthenticatedUserDto User);
