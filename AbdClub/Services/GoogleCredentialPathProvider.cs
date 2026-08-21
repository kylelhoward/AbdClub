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
                _logger?.LogWarning("Google service account key not found. Searched locations: {Primary}, {Fallback}", credentialPath, devPath);
            }

            return credentialPath;
        }
    }
}
