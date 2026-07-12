using FinancePlatform.Models.Enums;

namespace FinancePlatform.Services.Triggers;

/// <summary>
/// Result of executing a single trigger handler.
/// </summary>
public sealed class TriggerHandlerResult
{
    public required TriggerResultCode ResultCode { get; init; }

    public string? Message { get; init; }

    public string? ResultJson { get; init; }

    public IReadOnlyList<NextTriggerRequest> NextTriggers { get; init; } = [];

    public static TriggerHandlerResult Success(
        string? resultJson = null,
        IReadOnlyList<NextTriggerRequest>? nextTriggers = null,
        string? message = null)
    {
        return new TriggerHandlerResult
        {
            ResultCode = TriggerResultCode.Success,
            ResultJson = resultJson,
            NextTriggers = nextTriggers ?? [],
            Message = message
        };
    }

    public static TriggerHandlerResult Retry(string message) =>
        new() { ResultCode = TriggerResultCode.Retry, Message = message };

    public static TriggerHandlerResult Failure(string message) =>
        new() { ResultCode = TriggerResultCode.Failure, Message = message };

    public static TriggerHandlerResult Compensation(string message, IReadOnlyList<NextTriggerRequest>? nextTriggers = null) =>
        new()
        {
            ResultCode = TriggerResultCode.Compensation,
            Message = message,
            NextTriggers = nextTriggers ?? []
        };
}
