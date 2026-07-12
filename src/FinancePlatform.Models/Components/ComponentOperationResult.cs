using FinancePlatform.Models.Enums;

namespace FinancePlatform.Models.Components;

/// <summary>
/// Result returned by component services for EP orchestration.
/// </summary>
public sealed class ComponentOperationResult
{
    public required TriggerResultCode ResultCode { get; init; }

    public string? Message { get; init; }

    public string? ResultJson { get; init; }

    public IReadOnlyList<NextTriggerSpec> NextTriggers { get; init; } = [];

    public static ComponentOperationResult Success(
        string? resultJson = null,
        IReadOnlyList<NextTriggerSpec>? nextTriggers = null,
        string? message = null) =>
        new()
        {
            ResultCode = TriggerResultCode.Success,
            ResultJson = resultJson,
            NextTriggers = nextTriggers ?? [],
            Message = message
        };

    public static ComponentOperationResult Retry(string message) =>
        new() { ResultCode = TriggerResultCode.Retry, Message = message };

    public static ComponentOperationResult Failure(string message) =>
        new() { ResultCode = TriggerResultCode.Failure, Message = message };
}
