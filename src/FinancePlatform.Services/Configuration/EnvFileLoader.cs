namespace FinancePlatform.Services.Configuration;

/// <summary>
/// Loads a local <c>.env</c> file into process environment variables so the default
/// host configuration ( <c>__</c> / nested keys ) can bind secrets without committing them.
/// Existing environment variables are not overwritten.
/// </summary>
public static class EnvFileLoader
{
    public static void Load(string fileName = ".env", string? startDirectory = null)
    {
        var path = FindEnvFile(fileName, startDirectory ?? Directory.GetCurrentDirectory());
        if (path is null)
        {
            return;
        }

        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            var separator = trimmed.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = trimmed[..separator].Trim();
            if (key.Length == 0)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                continue;
            }

            var value = trimmed[(separator + 1)..].Trim();
            if (value.Length >= 2
                && ((value.StartsWith('"') && value.EndsWith('"'))
                    || (value.StartsWith('\'') && value.EndsWith('\''))))
            {
                value = value[1..^1];
            }

            Environment.SetEnvironmentVariable(key, value);
        }
    }

    private static string? FindEnvFile(string fileName, string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
