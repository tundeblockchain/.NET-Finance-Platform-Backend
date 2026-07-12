using System.Data;
using Dapper;
using FinancePlatform.Data.Sql;
using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public sealed class DistributionAgreementRepository(IDbConnectionFactory connectionFactory)
    : IDistributionAgreementRepository
{
    public async Task<DistributionAgreement?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<DistributionAgreement>(
            new CommandDefinition(
                SqlObjectNames.GetProc("DistributionAgreement"),
                new { Id = id },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<DistributionAgreement?> GetByOwnerAccountAsync(
        Guid ownerAccountId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<DistributionAgreement>(
            new CommandDefinition(
                "get_DistributionAgreement_ByOwnerAccount_f",
                new { OwnerAccountId = ownerAccountId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<DistributionAgreement> UpsertAsync(
        DistributionAgreement agreement,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agreement);

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleAsync<DistributionAgreement>(
            new CommandDefinition(
                SqlObjectNames.UpsertProc("DistributionAgreement"),
                new
                {
                    agreement.Id,
                    agreement.CustomerId,
                    OwnerComponent = (int)agreement.OwnerComponent,
                    agreement.OwnerAccountId,
                    agreement.Name,
                    agreement.IsActive,
                    agreement.CreatedUtc,
                    agreement.ChangedBy
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }
}
