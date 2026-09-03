using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace AbdClub.Services;

public sealed class BuildInfoService
{
    private readonly PublishedBuildInfo _published;

    public BuildInfoService(IWebHostEnvironment environment, ILogger<BuildInfoService> logger)
    {
        EnvironmentName = environment.EnvironmentName;
        _published = LoadPublishedBuildInfo(environment.ContentRootPath, logger);
    }

    public string Version => _published.Version;
    public string CommitId => _published.CommitId;
    public string ShortCommitId => CommitId.Length > 8 ? CommitId[..8] : CommitId;
    public DateTimeOffset? BuiltAtUtc => _published.BuiltAtUtc;
    public string Configuration => _published.Configuration;
    public string TargetFramework => _published.TargetFramework;
    public string BuildMachine => _published.BuildMachine;
    public string EnvironmentName { get; }
    public string RuntimeVersion => RuntimeInformation.FrameworkDescription;
    public string OperatingSystem => RuntimeInformation.OSDescription;
    public string ProcessArchitecture => RuntimeInformation.ProcessArchitecture.ToString();
    public string MachineName => System.Environment.MachineName;

    private static PublishedBuildInfo LoadPublishedBuildInfo(
        string contentRootPath,
        ILogger logger)
    {
        var path = Path.Combine(contentRootPath, "build-info.json");

        try
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var result = JsonSerializer.Deserialize<PublishedBuildInfo>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result != null)
                    return result;
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Unable to read build information from {BuildInfoPath}.", path);
        }

        var assemblyVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "unknown";
        return new PublishedBuildInfo { Version = assemblyVersion };
    }

    private sealed class PublishedBuildInfo
    {
        public string Version { get; init; } = "unknown";
        public string CommitId { get; init; } = "unknown";
        public DateTimeOffset? BuiltAtUtc { get; init; }
        public string Configuration { get; init; } = "unknown";
        public string TargetFramework { get; init; } = "unknown";
        public string BuildMachine { get; init; } = "unknown";
    }
}
