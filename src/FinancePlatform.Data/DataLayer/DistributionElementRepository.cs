using System.Data;
using Dapper;
using FinancePlatform.Data.Sql;
using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public sealed class DistributionElementRepository(IDbConnectionFactory connectionFactory)
    : IDistributionElementRepository
{
    public async Task<DistributionElement?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<DistributionElement>(
            new CommandDefinition(
                SqlObjectNames.GetProc("DistributionElement"),
                new { Id = id },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<DistributionElement>> GetByAgreementIdAsync(
        Guid agreementId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var rows = await connection.QueryAsync<DistributionElement>(
            new CommandDefinition(
                "get_DistributionElement_ByAgreementId_f",
                new { AgreementId = agreementId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        return rows.ToArray();
    }

    public async Task<DistributionElement> UpsertAsync(
        DistributionElement element,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(element);

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleAsync<DistributionElement>(
            new CommandDefinition(
                SqlObjectNames.UpsertProc("DistributionElement"),
                new
                {
                    element.Id,
                    element.AgreementId,
                    TargetType = (int)element.TargetType,
                    element.TargetAccountId,
                    element.Percentage,
                    element.Priority,
                    element.ChangedBy
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }
}
