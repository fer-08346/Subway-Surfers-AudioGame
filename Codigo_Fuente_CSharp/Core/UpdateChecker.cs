using System;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SubwaySurfersAudioGame.Core
{
    /// <summary>
    /// Metadatos de versión del juego y del repositorio remoto usado por el actualizador automático.
    /// </summary>
    public static class GameInfo
    {
        public const string CurrentVersion = "1.0.0";
        public const string RepoOwner = "fer-08346";
        public const string RepoName = "Subway-Surfers-AudioGame";
    }

    public class UpdateInfo
    {
        public string Version { get; set; } = "";
        public string HtmlUrl { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string Notes { get; set; } = "";
    }

    /// <summary>
    /// Comprueba y descarga actualizaciones desde los Releases de GitHub del proyecto.
    /// </summary>
    public static class UpdateChecker
    {
        public static async Task<UpdateInfo?> CheckForUpdateAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("SubwaySurfersAudioGame", GameInfo.CurrentVersion));
                client.Timeout = TimeSpan.FromSeconds(12);

                string url = $"https://api.github.com/repos/{GameInfo.RepoOwner}/{GameInfo.RepoName}/releases/latest";
                using var resp = await client.GetAsync(url);
                if (!resp.IsSuccessStatusCode) return null;

                string json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("tag_name", out var tagEl)) return null;
                string tag = tagEl.GetString() ?? "";
                if (string.IsNullOrWhiteSpace(tag)) return null;

                string notes = root.TryGetProperty("body", out var bodyEl) ? (bodyEl.GetString() ?? "") : "";
                string html = root.TryGetProperty("html_url", out var htmlEl) ? (htmlEl.GetString() ?? "") : "";

                string dl = "";
                if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
                {
                    foreach (var asset in assets.EnumerateArray())
                    {
                        string name = asset.TryGetProperty("name", out var n) ? (n.GetString() ?? "") : "";
                        if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            dl = asset.TryGetProperty("browser_download_url", out var d) ? (d.GetString() ?? "") : "";
                            break;
                        }
                    }
                }

                if (!IsNewer(tag, GameInfo.CurrentVersion)) return null;

                return new UpdateInfo
                {
                    Version = tag.TrimStart('v', 'V'),
                    HtmlUrl = html,
                    DownloadUrl = dl,
                    Notes = notes
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Descarga el ZIP del release y lo extrae en una carpeta temporal. Devuelve la ruta o null si falla.
        /// </summary>
        public static async Task<string?> DownloadAndPrepareUpdateAsync(string downloadUrl)
        {
            if (string.IsNullOrWhiteSpace(downloadUrl)) return null;

            string tempDir = Path.Combine(Path.GetTempPath(), "SSAG_Update_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(tempDir);
                string zipPath = Path.Combine(tempDir, "update.zip");

                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("SubwaySurfersAudioGame", GameInfo.CurrentVersion));
                client.Timeout = TimeSpan.FromMinutes(5);

                byte[] data = await client.GetByteArrayAsync(downloadUrl);
                await File.WriteAllBytesAsync(zipPath, data);
                ZipFile.ExtractToDirectory(zipPath, tempDir, overwriteFiles: true);
                return tempDir;
            }
            catch
            {
                try { Directory.Delete(tempDir, true); } catch { }
                return null;
            }
        }

        public static bool IsNewer(string remote, string current)
        {
            int[] rv = ParseVersion(remote);
            int[] cv = ParseVersion(current);
            for (int i = 0; i < 3; i++)
            {
                if (rv[i] != cv[i]) return rv[i] > cv[i];
            }
            return false;
        }

        private static int[] ParseVersion(string s)
        {
            s = s.Trim().TrimStart('v', 'V');
            var parts = s.Split('.');
            int[] result = { 0, 0, 0 };
            for (int i = 0; i < 3 && i < parts.Length; i++)
            {
                var m = Regex.Match(parts[i], @"\d+");
                if (m.Success) int.TryParse(m.Value, out result[i]);
            }
            return result;
        }
    }
}
