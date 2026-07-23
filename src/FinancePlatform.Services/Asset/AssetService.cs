using FinancePlatform.Models.Asset;
using FinancePlatform.Models.Components;
using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Trade;
using FinancePlatform.Services.Allocation;
using FinancePlatform.Services.Investment;
using FinancePlatform.Services.Trade;

namespace FinancePlatform.Services.Asset;

/// <summary>
/// Main asset-trading component service. Delegates cash/order/position work to <see cref="ITradeService"/>.
/// </summary>
public sealed class AssetService(
    ITradeService tradeService,
    IAllocationService allocationService,
    IInvestmentInstructionStore instructionStore) : IAssetService
{
    public async Task<ComponentOperationResult> BuyAsync(
        TriggerContext context,
        AssetOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
        {
            return ComponentOperationResult.Failure("Asset buy requires a positive quantity.");
        }

        var result = await tradeService.BuyAsync(context, ToTradeRequest(request), cancellationToken);
        if (result.ResultCode == TriggerResultCode.Success)
        {
            CompleteInstruction(request);
            if (context.AllocationRequestId is { } allocationId)
            {
                allocationService.MarkCompleted(allocationId);
            }
        }
        else if (result.ResultCode == TriggerResultCode.Failure && request.InstructionId != Guid.Empty)
        {
            instructionStore.TryUpdateStatus(request.InstructionId, InvestmentInstructionStatus.Failed);
        }

        return result;
    }

    public async Task<ComponentOperationResult> SellAsync(
        TriggerContext context,
        AssetOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
        {
            return ComponentOperationResult.Failure("Asset sell requires a positive quantity.");
        }

        var result = await tradeService.SellAsync(context, ToTradeRequest(request), cancellationToken);
        if (result.ResultCode == TriggerResultCode.Success)
        {
            CompleteInstruction(request);
        }
        else if (result.ResultCode == TriggerResultCode.Failure && request.InstructionId != Guid.Empty)
        {
            instructionStore.TryUpdateStatus(request.InstructionId, InvestmentInstructionStatus.Failed);
        }

        return result;
    }

    public async Task<ComponentOperationResult> ReverseBuyAsync(
        TriggerContext context,
        AssetOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
        {
            return ComponentOperationResult.Success(resultJson: """{"status":"asset-buy-reversed"}""");
        }

        var result = await tradeService.ReverseBuyAsync(context, ToTradeRequest(request), cancellationToken);
        if (result.ResultCode == TriggerResultCode.Success)
        {
            if (request.InstructionId != Guid.Empty)
            {
                instructionStore.TryUpdateStatus(request.InstructionId, InvestmentInstructionStatus.Failed);
            }

            return ComponentOperationResult.Success(resultJson: """{"status":"asset-buy-reversed"}""");
        }

        return result;
    }

    private void CompleteInstruction(AssetOrderRequest request)
    {
        if (request.InstructionId == Guid.Empty)
        {
            return;
        }

        if (request.OrderId != Guid.Empty)
        {
            instructionStore.TrySetOrderId(request.InstructionId, request.OrderId);
        }

        instructionStore.TryUpdateStatus(request.InstructionId, InvestmentInstructionStatus.Completed);
    }

    private static TradeAssetRequest ToTradeRequest(AssetOrderRequest request)
    {
        var cashHint = request.EffectiveCashAmount;
        return new TradeAssetRequest
        {
            AssetSymbol = request.AssetSymbol,
            Quantity = request.Quantity,
            Currency = request.Currency,
            CashAmount = cashHint > 0 ? cashHint : null,
            OrderId = request.OrderId == Guid.Empty ? null : request.OrderId
        };
    }
}
