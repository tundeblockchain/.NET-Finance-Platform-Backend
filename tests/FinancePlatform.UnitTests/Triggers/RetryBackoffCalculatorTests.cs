using FinancePlatform.Services.Triggers;
using FluentAssertions;

namespace FinancePlatform.UnitTests.Triggers;

public class RetryBackoffCalculatorTests
{
    [Fact]
    public void Calculate_grows_exponentially_and_caps_at_max()
    {
        var baseDelay = TimeSpan.FromMilliseconds(100);
        var maxDelay = TimeSpan.FromMilliseconds(1000);

        var attempt1 = RetryBackoffCalculator.Calculate(1, baseDelay, maxDelay, jitterFactor: 0);
        var attempt2 = RetryBackoffCalculator.Calculate(2, baseDelay, maxDelay, jitterFactor: 0);
        var attempt5 = RetryBackoffCalculator.Calculate(5, baseDelay, maxDelay, jitterFactor: 0);

        attempt1.Should().Be(TimeSpan.FromMilliseconds(100));
        attempt2.Should().Be(TimeSpan.FromMilliseconds(200));
        attempt5.Should().Be(TimeSpan.FromMilliseconds(1000));
    }

    [Fact]
    public void Calculate_applies_deterministic_jitter_with_seeded_random()
    {
        var random = new Random(42);
        var delay = RetryBackoffCalculator.Calculate(
            attemptCount: 3,
            baseDelay: TimeSpan.FromMilliseconds(100),
            maxDelay: TimeSpan.FromMilliseconds(10_000),
            jitterFactor: 0.2,
            random);

        // 100 * 2^(3-1) = 400, jitter ±20%
        delay.TotalMilliseconds.Should().BeInRange(320, 480);
    }
}
