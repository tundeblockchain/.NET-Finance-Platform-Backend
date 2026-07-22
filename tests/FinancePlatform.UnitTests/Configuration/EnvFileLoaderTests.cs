using FinancePlatform.Services.Configuration;
using FluentAssertions;

namespace FinancePlatform.UnitTests.Configuration;

public class EnvFileLoaderTests
{
    [Fact]
    public void Load_sets_missing_environment_variables_from_env_file()
    {
        var key = $"FP_TEST_{Guid.NewGuid():N}";
        var dir = Directory.CreateTempSubdirectory("fp-env");
        try
        {
            File.WriteAllText(Path.Combine(dir.FullName, ".env"), $"{key}=from-dotenv\n");
            Environment.SetEnvironmentVariable(key, null);

            EnvFileLoader.Load(startDirectory: dir.FullName);

            Environment.GetEnvironmentVariable(key).Should().Be("from-dotenv");
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, null);
            dir.Delete(recursive: true);
        }
    }

    [Fact]
    public void Load_does_not_overwrite_existing_environment_variables()
    {
        var key = $"FP_TEST_{Guid.NewGuid():N}";
        var dir = Directory.CreateTempSubdirectory("fp-env");
        try
        {
            File.WriteAllText(Path.Combine(dir.FullName, ".env"), $"{key}=from-dotenv\n");
            Environment.SetEnvironmentVariable(key, "already-set");

            EnvFileLoader.Load(startDirectory: dir.FullName);

            Environment.GetEnvironmentVariable(key).Should().Be("already-set");
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, null);
            dir.Delete(recursive: true);
        }
    }
}
