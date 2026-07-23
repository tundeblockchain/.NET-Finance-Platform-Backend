using System.Text.Json;
using FinancePlatform.Models.Asset;
using FinancePlatform.Models.Components;
using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Investment;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Cash;
using FinancePlatform.Services.Customer;
using FinancePlatform.Services.Investment;
using FinancePlatform.Services.Orders;

namespace FinancePlatform.Services.Investment;

/// <summary>
/// Investment component: credits investment cash, creates the local Order row, then raises Asset triggers.
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

        if (!TrySyncCashDeposit(investmentAccount.Id, request.Currency, request.EffectiveCashAmount, context))
        {
            customerDirectory.TryDebitInvestmentAccount(
                investmentAccount.Id,
                request.EffectiveCashAmount,
                context.TriggerId,
                $"{context.IdempotencyKey}:investment-credit-rollback");
            return ComponentOperationResult.Failure(
                "Unable to sync executable cash ledger for investment account.");
        }

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

        if (instruction.Status is InvestmentInstructionStatus.Pending)
        {
            instructionStore.TryUpdateStatus(instruction.Id, InvestmentInstructionStatus.Processing);
        }

        var orderKey = $"{instruction.IdempotencyKey}:order";
        var order = orderService.TryCreate(
            orderKey,
            instruction.InvestmentAccountId,
            context.TriggerId,
            context.AllocationRequestId ?? context.RootWorkflowId,
            instruction.AssetSymbol,
            instruction.Side,
            instruction.Quantity,
            limitPrice: null);

        if (!order.Succeeded || order.Order is null)
        {
            instructionStore.TryUpdateStatus(instruction.Id, InvestmentInstructionStatus.Failed);
            return ComponentOperationResult.Failure(order.Error ?? "Unable to create asset order.");
        }

        instructionStore.TrySetOrderId(instruction.Id, order.Order.Id);

        var assetPayload = JsonSerializer.Serialize(new AssetOrderRequest
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
            resultJson: $$"""{"status":"investment-invested","orderId":"{{order.Order.Id}}"}""",
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

    private bool TrySyncCashDeposit(Guid investmentAccountId, string currency, decimal amount, TriggerContext context)
    {
        var lockResult = cashService.TryAcquireLock(
            investmentAccountId,
            currency,
            context.TriggerId,
            context.AllocationRequestId ?? context.RootWorkflowId,
            LockLease);

        if (!lockResult.IsHeld)
        {
            return false;
        }

        try
        {
            var deposit = cashService.TryDeposit(
                $"{context.IdempotencyKey}:investment-cash-sync",
                investmentAccountId,
                currency,
                amount,
                context.TriggerId);
            return deposit.Succeeded;
        }
        finally
        {
            cashService.TryReleaseLock(investmentAccountId, currency, context.TriggerId);
        }
    }
}
