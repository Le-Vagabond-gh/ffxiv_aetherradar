using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.ImGuiNotification;

namespace aetherradar
{
    internal class UpdateChecker
    {
        private const string RepoOwner = "Le-Vagabond-gh";
        private const string RepoName = "ffxiv_aetherradar";
        private const string PluginName = "Aether Radar";

        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(10),
        };

        static UpdateChecker()
        {
            HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ffxiv_aetherradar");
        }

        /// <summary>
        /// Gets the git short hash embedded at build time via InformationalVersion.
        /// Format is "0.0.0.1+{hash}" or "0.0.0.1+local" for local builds.
        /// </summary>
        private static string GetCurrentGitHash()
        {
            var infoVersion = typeof(UpdateChecker).Assembly
                .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false);
            if (infoVersion.Length > 0)
            {
                var version = ((System.Reflection.AssemblyInformationalVersionAttribute)infoVersion[0]).InformationalVersion;
                var plusIndex = version.IndexOf('+');
                if (plusIndex >= 0)
                    return version[(plusIndex + 1)..];
            }
            return "local";
        }

        public static void CheckForUpdate()
        {
            var currentHash = GetCurrentGitHash();
            if (currentHash == "local")
            {
                Service.PluginLog.Debug("Skipping update check for local build");
                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    await CheckForUpdateAsync(currentHash);
                }
                catch (Exception ex)
                {
                    Service.PluginLog.Debug(ex, "Update check failed");
                }
            });
        }

        private static async Task CheckForUpdateAsync(string currentHash)
        {
            var url = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";
            var response = await HttpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Service.PluginLog.Debug($"Update check HTTP {response.StatusCode}");
                return;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("tag_name", out var tagElement))
                return;

            var tag = tagElement.GetString();
            if (string.IsNullOrEmpty(tag))
                return;

            // Tag format: v20260225104603-30fd37f
            // Check if the current hash appears in the latest tag
            if (tag.Contains(currentHash, StringComparison.OrdinalIgnoreCase))
            {
                Service.PluginLog.Debug($"Up to date ({currentHash})");
                return;
            }

            var releaseUrl = $"https://github.com/{RepoOwner}/{RepoName}/releases/latest";

            Service.PluginLog.Info($"Update available: {tag} (current: {currentHash})");

            // Show chat message
            var message = new SeStringBuilder()
                .AddUiForeground(58) // yellow-ish
                .AddText($"[{PluginName}] ")
                .AddUiForegroundOff()
                .AddText("Update available! ")
                .AddUiForeground(32) // green link color
                .AddText(releaseUrl)
                .AddUiForegroundOff()
                .Build();
            Service.ChatGui.Print(message);

            // Show ImGui notification
            Service.NotificationManager.AddNotification(new Notification
            {
                Title = PluginName,
                Content = $"{PluginName}: a new version is available.",
                Type = NotificationType.Info,
                InitialDuration = TimeSpan.FromSeconds(8),
                UserDismissable = true,
            });
        }
    }
}
