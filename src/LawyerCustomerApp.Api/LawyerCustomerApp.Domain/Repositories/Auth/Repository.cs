using Dapper;
using LawyerCustomerApp.Domain.Auth.Common.Models;
using LawyerCustomerApp.Domain.Auth.Interfaces.Services;
using LawyerCustomerApp.Domain.Auth.Repositories.Models;
using LawyerCustomerApp.Domain.Auth.Responses.Repositories.Error;
using LawyerCustomerApp.Domain.Common.Responses.Error;
using LawyerCustomerApp.External.Database.Common.Models;
using LawyerCustomerApp.External.Extensions;
using LawyerCustomerApp.External.Interfaces;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace LawyerCustomerApp.Domain.Auth.Repositories;

internal class Repository : IRepository
{
    private readonly IConfiguration _configuration;

    private readonly IJwtService      _jwtService;
    private readonly IHashService     _hashService;
    private readonly IDatabaseService _databaseService;
    public Repository(IConfiguration configuration, IJwtService jwtService, IHashService hashService, IDatabaseService databaseService)
    {
        _configuration = configuration;

        _jwtService      = jwtService;
        _hashService     = hashService;
        _databaseService = databaseService;
    }

    public async Task<Result<AuthenticateInformation>> AuthenticateAsync(AuthenticateParameters parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var sqliteConnectionString = _configuration.GetConnectionString("Sqlite");

        if (string.IsNullOrWhiteSpace(sqliteConnectionString))
        {
            resultConstructor.SetConstructor(
                new NotFoundDatabaseConnectionStringError()
                {
                    Status = 500
                });

            return resultConstructor.Build<AuthenticateInformation>();
        }

        _databaseService.AppendConnectionStringWithIdentifier("local-sqlite", sqliteConnectionString, ProviderType.Sqlite);

        var connection = await _databaseService.GetConnection("local-sqlite", ProviderType.Sqlite);

        contextualizer.AssignContextualizedConnection(connection);

        var userInformation = await ValuesExtensions.GetValue(async () =>
        {
            var encrpytedEmail    = _hashService.Encrypt(parameters.Email);
            var encrpytedPassword = _hashService.Encrypt(parameters.Password);

            var queryParameters = new
            {
                Email    = encrpytedEmail,
                Password = encrpytedPassword
            };

            var stringBuilder = new StringBuilder();

            stringBuilder.Append(@"SELECT [U].[id]    AS [UserId],
                                          [U].[email] AS [Email] FROM [users] U
                                    WHERE [U].[email]    = @Email AND
                                          [U].[password] = @Password");

            var userInformation = await connection.Connection.QueryFirstOrDefaultAsync<AuthenticateDatabaseInformation>(
                new CommandDefinition(
                        commandText:       stringBuilder.ToString(),
                        parameters:        queryParameters,
                        transaction:       connection.Transaction,
                        cancellationToken: contextualizer.CancellationToken,
                        commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

            if (userInformation != null)
                return userInformation with
                {
                    Email = _hashService.Decrypt(userInformation.Email)
                };

            return null;
        });

        if (userInformation == null)
        {
            resultConstructor.SetConstructor(
                new TokenAuthenticationError()
                {
                    Status = 400
                });

            return resultConstructor.Build<AuthenticateInformation>();
        }

        var actualDate = DateTime.UtcNow;

        // [JWT Creation]
        var jwtTokenResult = _jwtService.GenerateJwtToken(
            new() 
            { 
                NameIdentifier = userInformation.UserId.ToString(),
                Email          = userInformation.Email,

                Role   = External.Jwt.Common.Models.JwtConfiguration.Roles.User,

                TimeSpecification = new() 
                { 
                    Base     = actualDate,
                    Quantity = 12,
                    Type     = External.Jwt.Common.Models.JwtConfiguration.TimeSpecificationProperties.Types.Hour
                }
            });

        if (jwtTokenResult.IsFinished)
            return resultConstructor.Build<AuthenticateInformation>().Incorporate(jwtTokenResult);

        // [Refresh Token Creation]
        var refreshTokenResult = _jwtService.GenerateRefreshToken();

        if (refreshTokenResult.IsFinished)
            return resultConstructor.Build<AuthenticateInformation>().Incorporate(refreshTokenResult);

        var tokenInformation = new
        {
            JwtToken = new
            {
                Raw   = jwtTokenResult.Value,
                Limit = actualDate.AddHours(12)
            },

            RefreshToken = new
            {
                Raw   = refreshTokenResult.Value,
                Limit = actualDate.AddDays(3)
            },
        };

        var includedItems = await ValuesExtensions.GetValue(async () =>
        {
            var encrpytedJwtToken     = _hashService.Encrypt(tokenInformation.JwtToken.Raw);
            var encrpytedRefreshToken = _hashService.Encrypt(tokenInformation.RefreshToken.Raw);

            var queryParameters = new
            {
                UserId = userInformation.UserId,

                JwtToken          = encrpytedJwtToken,
                JwtTokenLimitDate = tokenInformation.JwtToken.Limit,

                RefreshToken          = encrpytedRefreshToken,
                RefreshTokenLimitDate = tokenInformation.RefreshToken.Limit,

                CreatedDate = actualDate
            };

            var stringBuilder = new StringBuilder();

            stringBuilder.Append(@"INSERT INTO [tokens] ([user_id], [jwt_token], [jwt_token_limit_date], [refresh_token], [refresh_token_limit_date], [created_date])
                                                 VALUES (@UserId,  @JwtToken, @JwtTokenLimitDate, @RefreshToken, @RefreshTokenLimitDate, @CreatedDate)");

            var includedItems = await connection.Connection.ExecuteAsync(
                new CommandDefinition(
                        commandText:       stringBuilder.ToString(),
                        parameters:        queryParameters,
                        transaction:       connection.Transaction,
                        cancellationToken: contextualizer.CancellationToken,
                        commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

            return includedItems;
        });

        if (includedItems == 0)
        {
            resultConstructor.SetConstructor(
                new TokenAuthenticationInsertionError()
                {
                    Status = 500
                });

            return resultConstructor.Build<AuthenticateInformation>();
        }

        return resultConstructor.Build<AuthenticateInformation>(
            new()
            {
                Token        = tokenInformation.JwtToken.Raw,
                RefreshToken = tokenInformation.RefreshToken.Raw
            });
    }

    public async Task<Result<RefreshInformation>> RefreshAsync(RefreshParameters parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var sqliteConnectionString = _configuration.GetConnectionString("Sqlite");

        if (string.IsNullOrWhiteSpace(sqliteConnectionString))
        {
            resultConstructor.SetConstructor(
                new NotFoundDatabaseConnectionStringError()
                {
                    Status = 500
                });

            return resultConstructor.Build<RefreshInformation>();
        }

        _databaseService.AppendConnectionStringWithIdentifier("local-sqlite", sqliteConnectionString, ProviderType.Sqlite);

        var connection = await _databaseService.GetConnection("local-sqlite", ProviderType.Sqlite);

        contextualizer.AssignContextualizedConnection(connection);

        var tokensInformation = await ValuesExtensions.GetValue(async () =>
        {
            var encrpytedJwtToken     = _hashService.Encrypt(parameters.Token);
            var encrpytedRefreshToken = _hashService.Encrypt(parameters.RefreshToken);

            var queryParameters = new
            {
                JwtToken     = encrpytedJwtToken,
                RefreshToken = encrpytedRefreshToken
            };

            var stringBuilder = new StringBuilder();

            stringBuilder.Append(@"SELECT [T].[jwt_token]                AS [JwtToken], 
                                          [T].[jwt_token_limit_date]     AS [JwtTokenLimitDate], 
                                          [T].[refresh_token]            AS [RefreshToken], 
                                          [T].[refresh_token_limit_date] AS [RefreshTokenLimitDate],
                                          [U].[id]                       AS [UserId],      
                                          [U].[email]                    AS [Email] FROM [tokens] T
                                   LEFT JOIN [users] U ON [T].[user_id] = [U].[id]
                                   WHERE  [T].[jwt_token]     = @JwtToken AND 
                                          [T].[refresh_token] = @RefreshToken");

            var tokensInformation = await connection.Connection.QueryFirstOrDefaultAsync<RefreshDatabaseInformation>(
                new CommandDefinition(
                        commandText:       stringBuilder.ToString(),
                        parameters:        queryParameters,
                        transaction:       connection.Transaction,
                        cancellationToken: contextualizer.CancellationToken,
                        commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

            if (tokensInformation != null)
                return tokensInformation with
                {
                    Email        = _hashService.Decrypt(tokensInformation.Email),
                    JwtToken     = _hashService.Decrypt(tokensInformation.JwtToken),
                    RefreshToken = _hashService.Decrypt(tokensInformation.RefreshToken),
                };

            return null;
        });

        if (tokensInformation == null)
        {
            resultConstructor.SetConstructor(
                new TokenRefreshError()
                {
                    Status = 400
                });

            return resultConstructor.Build<RefreshInformation>();
        }

        var actualDate = DateTime.UtcNow;

        if (tokensInformation.JwtTokenLimitDate > actualDate)
        {
            resultConstructor.SetConstructor(
                new TokenJwtNotExpiredError()
                {
                    Status = 400
                });

            return resultConstructor.Build<RefreshInformation>();
        }

        if (tokensInformation.RefreshTokenLimitDate < actualDate)
        {
            resultConstructor.SetConstructor(
                new TokenRefreshExpiredError()
                {
                    Status = 400
                });

            return resultConstructor.Build<RefreshInformation>();
        }

        // [JWT Creation]
        var jwtTokenResult = _jwtService.GenerateJwtToken(
            new() 
            { 
                NameIdentifier = tokensInformation.UserId.ToString(),
                Email          = tokensInformation.Email,

                Role = External.Jwt.Common.Models.JwtConfiguration.Roles.User,

                TimeSpecification = new() 
                { 
                    Base     = actualDate,
                    Quantity = 12,
                    Type     = External.Jwt.Common.Models.JwtConfiguration.TimeSpecificationProperties.Types.Hour
                }
            });

        if (jwtTokenResult.IsFinished)
            return resultConstructor.Build<RefreshInformation>().Incorporate(jwtTokenResult);

        // [Refresh Token Creation]
        var refreshTokenResult = _jwtService.GenerateRefreshToken();

        if (refreshTokenResult.IsFinished)
            return resultConstructor.Build<RefreshInformation>().Incorporate(refreshTokenResult);

        var tokenInformation = new
        {
            JwtToken = new
            {
                Raw   = jwtTokenResult.Value,
                Limit = actualDate.AddHours(12)
            },

            RefreshToken = new
            {
                Raw   = refreshTokenResult.Value,
                Limit = actualDate.AddDays(3)
            },
        };

        var includedItems = await ValuesExtensions.GetValue(async () =>
        {
            var encrpytedJwtToken     = _hashService.Encrypt(tokenInformation.JwtToken.Raw);
            var encrpytedRefreshToken = _hashService.Encrypt(tokenInformation.RefreshToken.Raw);

            var queryParameters = new
            {
                UserId = tokensInformation.UserId,

                JwtToken          = encrpytedJwtToken,
                JwtTokenLimitDate = tokenInformation.JwtToken.Limit,

                RefreshToken          = encrpytedRefreshToken,
                RefreshTokenLimitDate = tokenInformation.RefreshToken.Limit,

                CreatedDate = actualDate
            };

            var stringBuilder = new StringBuilder();

            stringBuilder.Append(@"INSERT INTO [tokens] ([user_id], [jwt_token], [jwt_token_limit_date], [refresh_token], [refresh_token_limit_date], [created_date])
                                                 VALUES (@UserId,  @JwtToken, @JwtTokenLimitDate, @RefreshToken, @RefreshTokenLimitDate, @CreatedDate)");

            var includedItems = await connection.Connection.ExecuteAsync(
                new CommandDefinition(
                        commandText:       stringBuilder.ToString(),
                        parameters:        queryParameters,
                        transaction:       connection.Transaction,
                        cancellationToken: contextualizer.CancellationToken,
                        commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

            return includedItems;
        });

        if (includedItems == 0)
        {
            resultConstructor.SetConstructor(
                new TokenRefreshInsertionError()
                {
                    Status = 500
                });

            return resultConstructor.Build<RefreshInformation>();
        }

        return resultConstructor.Build<RefreshInformation>(
            new()
            {
                Token        = tokenInformation.JwtToken.Raw,
                RefreshToken = tokenInformation.RefreshToken.Raw
            });
    }

    public async Task<Result> InvalidateAsync(InvalidateParameters parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var sqliteConnectionString = _configuration.GetConnectionString("Sqlite");

        if (string.IsNullOrWhiteSpace(sqliteConnectionString))
        {
            resultConstructor.SetConstructor(
                new NotFoundDatabaseConnectionStringError()
                {
                    Status = 500
                });

            return resultConstructor.Build();
        }

        _databaseService.AppendConnectionStringWithIdentifier("local-sqlite", sqliteConnectionString, ProviderType.Sqlite);

        var connection = await _databaseService.GetConnection("local-sqlite", ProviderType.Sqlite);

        contextualizer.AssignContextualizedConnection(connection);

        var tokensInformation = await ValuesExtensions.GetValue(async () =>
        {
            var encrpytedJwtToken     = _hashService.Encrypt(parameters.Token);
            var encrpytedRefreshToken = _hashService.Encrypt(parameters.RefreshToken);

            var queryParameters = new
            {
                JwtToken     = encrpytedJwtToken,
                RefreshToken = encrpytedRefreshToken
            };

            var stringBuilder = new StringBuilder();

            stringBuilder.Append(@"SELECT [T].[id]            AS [Id], 
                                          [T].[invalidated]   AS [Invalidated] FROM [tokens] T
                                   WHERE  [T].[jwt_token]     = @JwtToken AND 
                                          [T].[refresh_token] = @RefreshToken");

            var tokensInformation = await connection.Connection.QueryFirstOrDefaultAsync<InvalidateDatabaseInformation>(
                new CommandDefinition(
                        commandText:       stringBuilder.ToString(),
                        parameters:        queryParameters,
                        transaction:       connection.Transaction,
                        cancellationToken: contextualizer.CancellationToken,
                        commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

            return tokensInformation;
        });

        if (tokensInformation == null)
        {
            resultConstructor.SetConstructor(
                new TokenInvalidatedError()
                {
                    Status = 400
                });

            return resultConstructor.Build();
        }

        if (tokensInformation.Invalidated)
        {
            resultConstructor.SetConstructor(
                new TokenInvalidatedError()
                {
                    Status = 400
                });

            return resultConstructor.Build();
        }

        var updatedItems = await ValuesExtensions.GetValue(async () =>
        {
            var queryParameters = new
            {
                TokenId     = tokensInformation.Id,
                Invalidated = true
            };

            var stringBuilder = new StringBuilder();

            stringBuilder.Append(@"UPDATE [tokens] T SET [T].[invalidated] = @Invalidated WHERE [T].[id] = @TokenId");

            var includedItems = await connection.Connection.ExecuteAsync(
                new CommandDefinition(
                        commandText:       stringBuilder.ToString(),
                        parameters:        queryParameters,
                        transaction:       connection.Transaction,
                        cancellationToken: contextualizer.CancellationToken,
                        commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

            return includedItems;
        });

        if (updatedItems == 0)
        {
            resultConstructor.SetConstructor(
                new TokenInvalidateUpdateError()
                {
                    Status = 500
                });

            return resultConstructor.Build();
        }

        return resultConstructor.Build();
    }

    public async Task<Result> ValidateAsync(ValidateParameters parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var sqliteConnectionString = _configuration.GetConnectionString("Sqlite");

        if (string.IsNullOrWhiteSpace(sqliteConnectionString))
        {
            resultConstructor.SetConstructor(
                new NotFoundDatabaseConnectionStringError()
                {
                    Status = 500
                });

            return resultConstructor.Build();
        }

        _databaseService.AppendConnectionStringWithIdentifier("local-sqlite", sqliteConnectionString, ProviderType.Sqlite);

        var connection = await _databaseService.GetConnection("local-sqlite", ProviderType.Sqlite);

        contextualizer.AssignContextualizedConnection(connection);

        var tokensInformation = await ValuesExtensions.GetValue(async () =>
        {
            var encrpytedJwtToken = _hashService.Encrypt(parameters.Token);

            var queryParameters = new
            {
                JwtToken = encrpytedJwtToken
            };

            var stringBuilder = new StringBuilder();

            stringBuilder.Append(@"SELECT [T].[jwt_token_limit_date] AS [JwtTokenLimitDate],  
                                          [T].[invalidated]          AS [Invaldiated] FROM [tokens] T
                                   WHERE  [T].[jwt_token] = @JwtToken");

            var tokensInformation = await connection.Connection.QueryFirstOrDefaultAsync<ValidateDatabaseInformation>(
                new CommandDefinition(
                        commandText:       stringBuilder.ToString(),
                        parameters:        queryParameters,
                        transaction:       connection.Transaction,
                        cancellationToken: contextualizer.CancellationToken,
                        commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

            return tokensInformation;
        });

        if (tokensInformation == null)
        {
            resultConstructor.SetConstructor(
                new TokenValidationError()
                {
                    Status = 400
                });

            return resultConstructor.Build();
        }

        if (tokensInformation.Invalidated)
        {
            resultConstructor.SetConstructor(
                new TokenInvalidatedError()
                {
                    Status = 400
                });

            return resultConstructor.Build();
        }

        var actualDate = DateTime.UtcNow;

        if (tokensInformation.JwtTokenLimitDate < actualDate)
        {
            resultConstructor.SetConstructor(
                new TokenJwtExpiredError()
                {
                    Status = 400
                });

            return resultConstructor.Build();
        }
        return resultConstructor.Build();
    }
}