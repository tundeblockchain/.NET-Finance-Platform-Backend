using System.Text.Json;
using FinancePlatform.Models.Allocation;
using FinancePlatform.Models.Components;
using FinancePlatform.Models.Customer;
using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Investment;
using FinancePlatform.Models.Trade;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Cash;
using FinancePlatform.Services.Customer;
using FinancePlatform.Services.Investment;
using FinancePlatform.Services.Ledger;
using FinancePlatform.Services.Orders;
using FinancePlatform.Services.Positions;

namespace FinancePlatform.Services.Investment;

/// <summary>
/// Main investment component service — receives distributed cash and creates asset orders.
/// </summary>
public sealed class InvestmentService(
    ICustomerDirectory customerDirectory,
    IInvestmentInstructionStore instructionStore,
    IOrderService orderService,
    ICashService cashService) : IInvestmentService
{
    private static readonly TimeSpan LockLease = TimeSpan.FromSeconds(30);

    public ComponentOperationResult ReceiveMoney(
        TriggerContext context,
        InvestMoneyRequest request,
        string rawPayloadJson)
    {
        if (request.EffectiveCashAmount <= 0)
        {
            return ComponentOperationResult.Failure("Investment receive amount must be positive.");
        }

        var investmentAccount = customerDirectory.FindInvestmentAccount(request.InvestmentAccountId);
        if (investmentAccount is null || investmentAccount.CustomerId != request.CustomerId)
        {
            return ComponentOperationResult.Failure("Investment account was not found.");
        }

        var credited = customerDirectory.TryCreditInvestmentAccount(
            investmentAccount.Id,
            request.EffectiveCashAmount,
            context.TriggerId,
            $"{context.IdempotencyKey}:investment-credit");

        if (!credited)
        {
            return ComponentOperationResult.Failure("Unable to credit investment account.");
        }

        SyncCashDeposit(investmentAccount.Id, request.Currency, request.EffectiveCashAmount, context);
        instructionStore.TryUpdateStatus(request.InstructionId, InvestmentInstructionStatus.Processing);

        return ComponentOperationResult.Success(
            resultJson: """{"status":"investment-received"}""",
            nextTriggers:
            [
                new NextTriggerSpec
                {
                    TriggerCode = TriggerCodes.InvestmentInvestMoney,
                    QueueName = QueueNames.Investment,
                    TargetComponent = "Investment",
                    PayloadJson = rawPayloadJson,
                    IdempotencyKey = $"{context.IdempotencyKey}:8002"
                }
            ]);
    }

    public ComponentOperationResult InvestMoney(
        TriggerContext context,
        InvestMoneyRequest request,
        string rawPayloadJson)
    {
        _ = rawPayloadJson;

        var instruction = instructionStore.GetById(request.InstructionId);
        if (instruction is null)
        {
            return ComponentOperationResult.Failure("Investment instruction was not found.");
        }

        var orderKey = $"{instruction.IdempotencyKey}:asset-order";
        var order = orderService.TryCreate(
            orderKey,
            instruction.InvestmentAccountId,
            context.TriggerId,
            context.AllocationRequestId ?? context.RootWorkflowId,
            instruction.AssetSymbol,
            instruction.Side,
            instruction.Quantity,
            limitPrice: null);

        if (!order.Succeeded)
        {
            instructionStore.TryUpdateStatus(instruction.Id, InvestmentInstructionStatus.Failed);
            return ComponentOperationResult.Failure(order.Error ?? "Unable to create asset order.");
        }

        instructionStore.TrySetOrderId(instruction.Id, order.Order!.Id);

        var assetPayload = JsonSerializer.Serialize(new Models.Asset.AssetOrderRequest
        {
            InstructionId = instruction.Id,
            OrderId = order.Order.Id,
            InvestmentAccountId = instruction.InvestmentAccountId,
            AssetSymbol = instruction.AssetSymbol,
            Quantity = instruction.Quantity,
            Currency = instruction.Currency,
            CashAmount = instruction.CashAmount
        });

        return ComponentOperationResult.Success(
            resultJson: """{"status":"investment-invested"}""",
            nextTriggers:
            [
                new NextTriggerSpec
                {
                    TriggerCode = instruction.Side == OrderSide.Buy
                        ? TriggerCodes.AssetBuyAsset
                        : TriggerCodes.AssetSellAsset,
                    QueueName = QueueNames.AssetTrading,
                    TargetComponent = "AssetTrading",
                    PayloadJson = assetPayload,
                    IdempotencyKey = $"{context.IdempotencyKey}:9001"
                }
            ]);
    }

    private void SyncCashDeposit(Guid investmentAccountId, string currency, decimal amount, TriggerContext context)
    {
        var lockResult = cashService.TryAcquireLock(
            investmentAccountId,
            currency,
            context.TriggerId,
            context.AllocationRequestId ?? context.RootWorkflowId,
            LockLease);

        if (!lockResult.IsHeld)
        {
            return;
        }

        try
        {
            cashService.TryDeposit(
                $"{context.IdempotencyKey}:investment-cash-sync",
                investmentAccountId,
                currency,
                amount,
                context.TriggerId);
        }
        finally
        {
            cashService.TryReleaseLock(investmentAccountId, currency, context.TriggerId);
        }
    }
}
