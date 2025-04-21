namespace LawyerCustomerApp.Domain.Auth.Common.Models;

public class AuthenticateParametersDto
{
    public string? Email { get; init; }
    public string? Password { get; init; }

    public AuthenticateParameters ToOrdinary()
    {
        return new AuthenticateParameters
        {
            Email    = this.Email    ?? string.Empty,
            Password = this.Password ?? string.Empty
        };
    }
}

public class AuthenticateParameters
{
    public required string Email { get; init; } = string.Empty;
    public required string Password { get; init; } = string.Empty;

    public AuthenticateParametersDto ToDto()
    {
        return new AuthenticateParametersDto
        {
            Email    = this.Email,
            Password = this.Password
        };
    }
}

public class RefreshParametersDto
{
    public string? Token { get; init; }
    public string? RefreshToken { get; init; }

    public RefreshParameters ToOrdinary()
    {
        return new RefreshParameters
        {
            Token        = this.Token        ?? string.Empty,
            RefreshToken = this.RefreshToken ?? string.Empty
        }; 
    }
}

public class RefreshParameters
{
    public required string Token { get; init; } = string.Empty;
    public required string RefreshToken { get; init; } = string.Empty;

    public RefreshParametersDto ToDto()
    {
        return new RefreshParametersDto
        {
            Token        = this.Token,
            RefreshToken = this.RefreshToken
        };  
    }
}

public class InvalidateParametersDto
{
    public string? Token { get; init; }
    public string? RefreshToken { get; init; }

    public InvalidateParameters ToOrdinary()
    {
        return new InvalidateParameters
        {
            Token        = this.Token        ?? string.Empty,
            RefreshToken = this.RefreshToken ?? string.Empty
        }; 
    }
}

public class InvalidateParameters
{
    public required string Token { get; init; } = string.Empty;
    public required string RefreshToken { get; init; } = string.Empty;

    public InvalidateParametersDto ToDto()
    {
        return new InvalidateParametersDto
        {
            Token        = this.Token,
            RefreshToken = this.RefreshToken
        };  
    }
}

public class ValidateParametersDto
{
    public string? Token { get; init; }

    public ValidateParameters ToOrdinary()
    {
        return new ValidateParameters
        {
            Token = this.Token ?? string.Empty
        };
    }
}

public class ValidateParameters
{
    public required string Token { get; init; } = string.Empty;

    public ValidateParametersDto ToDto()
    {
        return new ValidateParametersDto
        {
            Token = this.Token
        };
    }
}


public class AuthenticateInformationDto
{
    public string? Token { get; init; }
    public string? RefreshToken { get; init; }

    public AuthenticateInformation ToOrdinary()
    {
        return new AuthenticateInformation
        {
            Token        = this.Token        ?? string.Empty,
            RefreshToken = this.RefreshToken ?? string.Empty
        };
    }
}

public class AuthenticateInformation
{
    public required string Token { get; init; } = string.Empty;
    public required string RefreshToken { get; init; } = string.Empty;

    public AuthenticateInformationDto ToDto()
    {
        return new AuthenticateInformationDto
        {
            Token        = this.Token,
            RefreshToken = this.RefreshToken
        };
    }
}

public class RefreshInformationDto
{
    public string? Token { get; init; }
    public string? RefreshToken { get; init; }

    public RefreshInformation ToOrdinary()
    {
        return new RefreshInformation
        {
            Token        = this.Token        ?? string.Empty,
            RefreshToken = this.RefreshToken ?? string.Empty
        };
    }
}

public class RefreshInformation
{
    public required string Token { get; init; } = string.Empty;
    public required string RefreshToken { get; init; } = string.Empty;

    public RefreshInformationDto ToDto()
    {
        return new RefreshInformationDto
        {
            Token        = this.Token,
            RefreshToken = this.RefreshToken
        };
    }
}