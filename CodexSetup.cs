using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

internal sealed class SetupProgress
{
    internal string Message;
    internal int Percent;
    internal SetupProgress(string message, int percent) { Message = message; Percent = percent; }
}

internal static class CodexSetup
{
    private const string ReleaseApi = "https://api.github.com/repos/openai/codex/releases/latest";
    private const string AssetName = "codex-x86_64-pc-windows-msvc.exe";

    internal static bool IsReady()
    {
        try { return File.Exists(CodexClient.FindCodex()); }
        catch { return false; }
    }

    internal static bool OfficialDownload(string value)
    {
        Uri uri;
        return Uri.TryCreate(value, UriKind.Absolute, out uri) && uri.Scheme == "https"
            && uri.Host == "github.com" && uri.AbsolutePath.StartsWith("/openai/codex/releases/download/", StringComparison.Ordinal);
    }

    internal static string Digest(object asset)
    {
        string digest = Convert.ToString(Data.Get(asset, "digest"));
        if (!Regex.IsMatch(digest, "^sha256:[a-fA-F0-9]{64}$"))
            throw new Exception("La publicación oficial no incluye una suma de verificación válida. Inténtalo de nuevo más tarde.");
        return digest.Substring(7).ToLowerInvariant();
    }

    private static string Hash(string path)
    {
        using (var stream = File.OpenRead(path))
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
    }

    private static void Report(IProgress<SetupProgress> progress, string message, int percent)
    {
        if (progress != null) progress.Report(new SetupProgress(message, percent));
    }

    internal static async Task<string> Prepare(IProgress<SetupProgress> progress, CancellationToken cancellation, bool repair = false)
    {
        cancellation.ThrowIfCancellationRequested();
        if (!repair && IsReady()) return CodexClient.FindCodex();
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
        using (var http = new HttpClient())
        {
            deadline.CancelAfter(TimeSpan.FromMinutes(15));
            var token = deadline.Token;
            http.Timeout = TimeSpan.FromMinutes(2);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("CodexRings/1.0.1");
            http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            Report(progress, "Buscando la versión oficial de Codex…", 0);
            object release;
            using (var response = await http.GetAsync(ReleaseApi, token))
            {
                if (!response.IsSuccessStatusCode)
                    throw new Exception("No se pudo consultar la descarga oficial de Codex. Revisa tu conexión e inténtalo más tarde.");
                release = Data.Json().DeserializeObject(await response.Content.ReadAsStringAsync());
            }
            string version = Convert.ToString(Data.Get(release, "tag_name"));
            if (!Regex.IsMatch(version, "^[A-Za-z0-9._-]{1,80}$")) throw new Exception("La versión oficial recibida no es válida.");
            var assets = Data.Get(release, "assets") as object[];
            if (assets == null) throw new Exception("La publicación oficial no contiene archivos de instalación.");
            object archive = assets.FirstOrDefault(a => Convert.ToString(Data.Get(a, "name")) == AssetName + ".zip");
            object binary = assets.FirstOrDefault(a => Convert.ToString(Data.Get(a, "name")) == AssetName);
            if (archive == null || binary == null) throw new Exception("No se encontró la versión oficial de Codex para Windows x64.");
            string source = Convert.ToString(Data.Get(archive, "browser_download_url"));
            if (!OfficialDownload(source)) throw new Exception("La dirección de descarga no pertenece a la publicación oficial de Codex.");
            string archiveHash = Digest(archive), binaryHash = Digest(binary);
            string directory = Path.Combine(Data.Dir, "runtime", version);
            Directory.CreateDirectory(directory);
            string target = Path.Combine(directory, "codex.exe");
            if (File.Exists(target) && await Task.Run(() => Hash(target), token) == binaryHash)
            {
                token.ThrowIfCancellationRequested();
                File.WriteAllText(Path.Combine(Data.Dir, "codex-path.txt"), target);
                Report(progress, "Codex está listo. Conectando tu cuenta…", 100);
                return target;
            }
            string temporary = Path.Combine(directory, Guid.NewGuid().ToString("N"));
            string zip = temporary + ".zip", partial = temporary + ".exe";
            try
            {
                long expectedSize = Convert.ToInt64(Data.Get(archive, "size"));
                if (expectedSize <= 0 || expectedSize > 1024L * 1024 * 1024) throw new Exception("El tamaño de la publicación oficial no es compatible con esta versión de Rings.");
                using (var response = await http.GetAsync(source, HttpCompletionOption.ResponseHeadersRead, token))
                {
                    response.EnsureSuccessStatusCode();
                    using (var input = await response.Content.ReadAsStreamAsync())
                    using (var output = new FileStream(zip, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
                    {
                        var buffer = new byte[81920];
                        long total = 0;
                        int count, last = -1;
                        while ((count = await input.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                        {
                            await output.WriteAsync(buffer, 0, count, token);
                            total += count;
                            if (total > expectedSize) throw new Exception("El tamaño de la descarga no coincide con la publicación oficial.");
                            int percent = expectedSize > 0 ? (int)(total * 100 / expectedSize) : 0;
                            if (percent != last)
                            {
                                last = percent;
                                Report(progress, "Descargando Codex oficial · " + percent + "%", percent);
                            }
                        }
                        if (total != expectedSize) throw new Exception("La descarga quedó incompleta. Vuelve a intentarlo.");
                    }
                }
                Report(progress, "Verificando la descarga…", 100);
                if (await Task.Run(() => Hash(zip), token) != archiveHash) throw new Exception("La descarga no pasó la verificación SHA-256. No se ejecutó ningún archivo.");
                token.ThrowIfCancellationRequested();
                using (var package = ZipFile.OpenRead(zip))
                {
                    var entry = package.Entries.SingleOrDefault(e => Path.GetFileName(e.FullName.Replace('\\', '/')) == AssetName || Path.GetFileName(e.FullName.Replace('\\', '/')) == "codex.exe");
                    if (entry == null || entry.Length <= 0 || entry.Length > 1024L * 1024 * 1024)
                        throw new Exception("El paquete oficial no contiene un ejecutable de Codex válido.");
                    // Copy only the selected binary to our own fixed path; never extract archive paths.
                    using (var input = entry.Open())
                    using (var output = new FileStream(partial, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
                        await input.CopyToAsync(output, 81920, token);
                }
                if (await Task.Run(() => Hash(partial), token) != binaryHash) throw new Exception("El ejecutable no pasó la verificación SHA-256. No se ejecutó ningún archivo.");
                token.ThrowIfCancellationRequested();
                if (File.Exists(target)) File.Delete(target);
                File.Move(partial, target);
                File.WriteAllText(Path.Combine(Data.Dir, "codex-path.txt"), target);
                File.WriteAllText(Path.Combine(directory, "source.json"), Data.Json().Serialize(new { version = version, source = source, sha256 = binaryHash }));
                Report(progress, "Codex está listo. Conectando tu cuenta…", 100);
                return target;
            }
            finally
            {
                if (File.Exists(zip)) File.Delete(zip);
                if (File.Exists(partial)) File.Delete(partial);
            }
        }
    }
}
