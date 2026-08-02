namespace CrmMida.Api.Configuration;

public static class EnvironmentFileLoader
{
    public static string? LoadFromCurrentOrParentDirectories(string fileName = ".env")
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (directory is not null)
        {
            var path = Path.Combine(directory.FullName, fileName);
            if (File.Exists(path))
            {
                Load(path);
                ConfigureAspNetCoreUrl();
                return path;
            }

            directory = directory.Parent;
        }

        ConfigureAspNetCoreUrl();
        return null;
    }

    private static void Load(string path)
    {
        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            if (line.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
                line = line[7..].Trim();

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
                continue;

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();

            if (value.Length >= 2 &&
                ((value.StartsWith('"') && value.EndsWith('"')) ||
                 (value.StartsWith('\'') && value.EndsWith('\''))))
            {
                value = value[1..^1];
            }

            // Las variables del sistema, Docker o el servidor tienen prioridad sobre .env.
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                Environment.SetEnvironmentVariable(key, value);
        }
    }

    private static void ConfigureAspNetCoreUrl()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
            return;

        var apiPort = Environment.GetEnvironmentVariable("API_PORT");
        if (int.TryParse(apiPort, out var port) && port is > 0 and <= 65535)
            Environment.SetEnvironmentVariable("ASPNETCORE_URLS", $"http://localhost:{port}");
    }
}
