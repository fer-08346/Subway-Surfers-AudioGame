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
        public const string CurrentVersion = "1.2.0";
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
        public static async Task<string?> DownloadAndPrepareUpdateAsync(string downloadUrl, Action<int>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(downloadUrl)) return null;

            string tempDir = Path.Combine(Path.GetTempPath(), "SSAG_Update_" + Guid.NewGuid().ToString("N"));
            string zipPath = Path.Combine(tempDir, "update.zip");
            try
            {
                Directory.CreateDirectory(tempDir);

                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("SubwaySurfersAudioGame", GameInfo.CurrentVersion));
                client.Timeout = TimeSpan.FromMinutes(5);

                if (progress != null)
                {
                    using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();
                    long? total = response.Content.Headers.ContentLength;
                    using var src = await response.Content.ReadAsStreamAsync();
                    using var dst = File.Create(zipPath);
                    byte[] buffer = new byte[81920];
                    long read = 0;
                    int chunk;
                    while ((chunk = await src.ReadAsync(buffer)) > 0)
                    {
                        await dst.WriteAsync(buffer.AsMemory(0, chunk));
                        read += chunk;
                        if (total.HasValue && total.Value > 0)
                        {
                            progress((int)(read * 100 / total.Value));
                        }
                    }
                }
                else
                {
                    byte[] data = await client.GetByteArrayAsync(downloadUrl);
                    await File.WriteAllBytesAsync(zipPath, data);
                }

                ZipFile.ExtractToDirectory(zipPath, tempDir, overwriteFiles: true);
            }
            catch
            {
                try { Directory.Delete(tempDir, true); } catch { }
                return null;
            }
            finally
            {
                // Always dispose of the downloaded archive so no .zip garbage is left on disk
                // (and, crucially, so it never gets copied into the game installation folder).
                if (File.Exists(zipPath))
                {
                    try { File.Delete(zipPath); }
                    catch { }
                }
            }

            string? contentRoot = FindUpdateContentRoot(tempDir);
            if (contentRoot == null)
            {
                try { Directory.Delete(tempDir, true); } catch { }
                return null;
            }

            return contentRoot;
        }

        /// <summary>
        /// Locates the real root folder of the extracted update, handling zips whose files live inside
        /// a nested subfolder (e.g. "Subway Surfers Audiogame/..."). Returns null if no executable is present.
        /// </summary>
        private static string? FindUpdateContentRoot(string extractDir)
        {
            const string exeName = "SubwaySurfersAudioGame.exe";

            if (File.Exists(Path.Combine(extractDir, exeName)))
            {
                return extractDir;
            }

            foreach (var dir in Directory.GetDirectories(extractDir))
            {
                if (File.Exists(Path.Combine(dir, exeName)))
                {
                    return dir;
                }
            }

            string? anyExe = Directory.GetFiles(extractDir, "*.exe", SearchOption.AllDirectories).FirstOrDefault();
            return anyExe != null ? Path.GetDirectoryName(anyExe) : null;
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
