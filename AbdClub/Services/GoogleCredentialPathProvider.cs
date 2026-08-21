using AbdClub.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace AbdClub.Services
{

    public class GoogleCredentialPathProvider : IGoogleCredentialPathProvider
    {
        private readonly string? _configPath;
        private readonly ILogger<GoogleCredentialPathProvider>? _logger;

        public GoogleCredentialPathProvider(IConfiguration config, ILogger<GoogleCredentialPathProvider>? logger = null)
        {
            _configPath = config["Google:ServiceAccountKeyPath"];
            _logger = logger;
        }

        public string GetCredentialPath()
        {
            // Try environment variable GOOGLE_APPLICATION_CREDENTIALS first (Application Default Credentials)
            string? envPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
            if (!string.IsNullOrWhiteSpace(envPath))
            {
                if (System.IO.File.Exists(envPath))
                {
                    _logger?.LogInformation("Using Google credentials from GOOGLE_APPLICATION_CREDENTIALS environment variable: {Path}", envPath);
                    return envPath;
                }
                else
                {
                    _logger?.LogWarning("GOOGLE_APPLICATION_CREDENTIALS points to non-existent file: {Path}", envPath);
                }
            }

            // Fallback to configuration-based path
            string credentialPath;
            if (!string.IsNullOrWhiteSpace(_configPath))
            {
                credentialPath = Path.IsPathRooted(_configPath)
                    ? _configPath!
                    : Path.Combine(Directory.GetCurrentDirectory(), _configPath!);
            }
            else
            {
                credentialPath = Path.Combine(AppContext.BaseDirectory, "Data", "Publishing", "abdclub-81c5d585c9db.json");
            }

            var devPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Publishing", Path.GetFileName(credentialPath));
            bool existsAtCredential = System.IO.File.Exists(credentialPath);
            bool existsAtDev = System.IO.File.Exists(devPath);

            if (!existsAtCredential && existsAtDev)
            {
                credentialPath = devPath;
                existsAtCredential = true;
            }

            if (!existsAtCredential)
            {
                _logger?.LogWarning("Google service account key not found. Searched locations: {Primary}, {Fallback}. Ensure GOOGLE_APPLICATION_CREDENTIALS is set for Application Default Credentials.", credentialPath, devPath);
            }

            return credentialPath;
        }
    }
}
