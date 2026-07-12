using System.Data;
using Dapper;
using FinancePlatform.Data.Sql;
using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public sealed class AccountRepository(IDbConnectionFactory connectionFactory) : IAccountRepository
{
    public async Task<Account?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<Account>(
            new CommandDefinition(
                SqlObjectNames.GetProc("Account"),
                new { Id = id },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<Account> UpsertAsync(Account account, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleAsync<Account>(
            new CommandDefinition(
                SqlObjectNames.UpsertProc("Account"),
                new
                {
                    account.Id,
                    account.CustomerId,
                    account.AccountNumber,
                    account.Currency,
                    account.IsActive,
                    account.CreatedUtc,
                    account.ChangedBy
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }
}
