using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace CyberSecOS.Core.Services;

/// <summary>A started backend server process.</summary>
public interface IBackendProcess : IDisposable
{
    bool HasExited { get; }

    void Stop();
}

/// <summary>The operating-system calls <see cref="LocalBackend"/> needs, so its logic can be tested.</summary>
public interface IBackendHost
{
    bool FileExists(string path);

    Task<bool> IsHealthyAsync(Uri baseAddress, CancellationToken cancellationToken);

    bool IsPortFree(int port);

    int GetFreePort();

    IBackendProcess Start(string serverPath, IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string> environment, string logPath);
}

/// <summary>
/// Starts the bundled API server (installed next to the desktop app) when no
/// server is already answering, and stops it again when the app closes. A
/// developer running the API by hand keeps working: a healthy server wins.
/// </summary>
public sealed class LocalBackend : IDisposable
{
    private readonly IBackendHost _host;
    private IBackendProcess? _process;

    public LocalBackend(string serverPath, string dataDirectory, IBackendHost host)
    {
        ServerPath = serverPath;
        DataDirectory = dataDirectory;
        _host = host;
    }

    public string ServerPath { get; }

    public string DataDirectory { get; }

    /// <summary>First start seeds the whole catalogue, so allow it a generous while.</summary>
    public TimeSpan StartTimeout { get; init; } = TimeSpan.FromSeconds(90);

    public TimeSpan PollInterval { get; init; } = TimeSpan.FromMilliseconds(250);

    public int ParentProcessId { get; init; } = Environment.ProcessId;

    /// <summary>True when this app started the server and will stop it.</summary>
    public bool OwnsServer => _process is not null;

    public string LogPath => Path.Combine(DataDirectory, "server.log");

    /// <summary>The data folder of an installed build: %LOCALAPPDATA%\CyberSecOS\data.</summary>
    public static string DefaultDataDirectory() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CyberSecOS", "data");

    /// <summary>
    /// Returns the address the app should use: <paramref name="configured"/> if
    /// a server already answers there (or nothing can be started), otherwise the
    /// address of a freshly started bundled server.
    /// </summary>
    public async Task<Uri> EnsureRunningAsync(Uri configured, CancellationToken cancellationToken = default)
    {
        if (await _host.IsHealthyAsync(configured, cancellationToken))
        {
            return configured;
        }
        if (!configured.IsLoopback || !_host.FileExists(ServerPath))
        {
            return configured;
        }

        var port = _host.IsPortFree(configured.Port) ? configured.Port : _host.GetFreePort();
        var address = new Uri($"http://127.0.0.1:{port}/");
        _process = _host.Start(
            ServerPath,
            ["--host", "127.0.0.1", "--port", port.ToString(), "--parent-pid", ParentProcessId.ToString()],
            new Dictionary<string, string> { ["CYBERSECOS_DATA_DIR"] = DataDirectory },
            LogPath);

        var deadline = DateTime.UtcNow + StartTimeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await _host.IsHealthyAsync(address, cancellationToken))
            {
                return address;
            }
            if (_process.HasExited)
            {
                Stop();
                throw new BackendStartException($"The CyberSecOS server stopped while starting. Details are in {LogPath}.");
            }
            await Task.Delay(PollInterval, cancellationToken);
        }
        Stop();
        throw new BackendStartException($"The CyberSecOS server did not start within {StartTimeout.TotalSeconds:0} seconds. Details are in {LogPath}.");
    }

    public void Stop()
    {
        if (_process is null)
        {
            return;
        }
        try
        {
            _process.Stop();
        }
        finally
        {
            _process.Dispose();
            _process = null;
        }
    }

    public void Dispose() => Stop();
}

public sealed class BackendStartException(string message) : Exception(message);

/// <summary>Real processes, sockets and HTTP.</summary>
public sealed class SystemBackendHost : IBackendHost
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(2) };

    public bool FileExists(string path) => File.Exists(path);

    public async Task<bool> IsHealthyAsync(Uri baseAddress, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await Http.GetAsync(new Uri(baseAddress, "api/health"), cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    public bool IsPortFree(int port)
    {
        try
        {
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    public int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public IBackendProcess Start(string serverPath, IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string> environment, string logPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        var info = new ProcessStartInfo(serverPath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(serverPath)!,
        };
        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }
        foreach (var (key, value) in environment)
        {
            info.Environment[key] = value;
        }
        return new SystemBackendProcess(Process.Start(info)!, logPath);
    }

    private sealed class SystemBackendProcess : IBackendProcess
    {
        private readonly Process _process;
        private readonly StreamWriter _log;

        public SystemBackendProcess(Process process, string logPath)
        {
            _process = process;
            _log = new StreamWriter(logPath, append: false) { AutoFlush = true };
            _process.OutputDataReceived += (_, e) => Write(e.Data);
            _process.ErrorDataReceived += (_, e) => Write(e.Data);
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }

        public bool HasExited => _process.HasExited;

        private void Write(string? line)
        {
            if (line is null)
            {
                return;
            }
            lock (_log)
            {
                _log.WriteLine(line);
            }
        }

        public void Stop()
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(5000);
            }
        }

        public void Dispose()
        {
            _process.Dispose();
            lock (_log)
            {
                _log.Dispose();
            }
        }
    }
}
