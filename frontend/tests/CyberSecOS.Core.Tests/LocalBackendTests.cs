using CyberSecOS.Core.Services;

namespace CyberSecOS.Core.Tests;

public class LocalBackendTests
{
    private static readonly Uri Configured = new("http://127.0.0.1:8000/");

    private sealed class FakeProcess : IBackendProcess
    {
        public bool HasExited { get; set; }
        public bool Stopped { get; private set; }
        public void Stop() => Stopped = true;
        public void Dispose() { }
    }

    private sealed class FakeHost : IBackendHost
    {
        public bool ServerInstalled { get; set; } = true;
        public HashSet<int> HealthyPorts { get; } = [];
        public HashSet<int> BusyPorts { get; } = [];
        public int HealthyAfterStartChecks { get; set; } = 1;
        public bool ExitOnStart { get; set; }
        public FakeProcess? Process { get; private set; }
        public IReadOnlyList<string> Arguments { get; private set; } = [];
        public IReadOnlyDictionary<string, string> Environment { get; private set; } = new Dictionary<string, string>();
        private int _checksSinceStart;

        public bool FileExists(string path) => ServerInstalled;

        public Task<bool> IsHealthyAsync(Uri baseAddress, CancellationToken cancellationToken)
        {
            if (Process is not null && !Process.HasExited && ++_checksSinceStart >= HealthyAfterStartChecks)
            {
                HealthyPorts.Add(baseAddress.Port);
            }
            return Task.FromResult(HealthyPorts.Contains(baseAddress.Port));
        }

        public bool IsPortFree(int port) => !BusyPorts.Contains(port);

        public int GetFreePort() => 51234;

        public IBackendProcess Start(string serverPath, IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string> environment, string logPath)
        {
            Arguments = arguments;
            Environment = environment;
            Process = new FakeProcess { HasExited = ExitOnStart };
            return Process;
        }
    }

    private static LocalBackend Create(FakeHost host) =>
        new("server.exe", "data", host) { PollInterval = TimeSpan.Zero, StartTimeout = TimeSpan.FromSeconds(5), ParentProcessId = 42 };

    [Fact]
    public async Task UsesServerThatIsAlreadyRunning()
    {
        var host = new FakeHost();
        host.HealthyPorts.Add(8000);
        using var backend = Create(host);

        Assert.Equal(Configured, await backend.EnsureRunningAsync(Configured));
        Assert.Null(host.Process);
        Assert.False(backend.OwnsServer);
    }

    [Fact]
    public async Task LeavesAddressAloneWhenNoServerIsInstalled()
    {
        var host = new FakeHost { ServerInstalled = false };
        using var backend = Create(host);

        Assert.Equal(Configured, await backend.EnsureRunningAsync(Configured));
        Assert.Null(host.Process);
    }

    [Fact]
    public async Task NeverStartsServerForRemoteAddress()
    {
        var host = new FakeHost();
        using var backend = Create(host);
        var remote = new Uri("http://lab.example:8000/");

        Assert.Equal(remote, await backend.EnsureRunningAsync(remote));
        Assert.Null(host.Process);
    }

    [Fact]
    public async Task StartsBundledServerOnConfiguredPortWithDataFolder()
    {
        var host = new FakeHost { HealthyAfterStartChecks = 3 };
        using var backend = Create(host);

        var address = await backend.EnsureRunningAsync(Configured);

        Assert.Equal(Configured, address);
        Assert.True(backend.OwnsServer);
        Assert.Equal(["--host", "127.0.0.1", "--port", "8000", "--parent-pid", "42"], host.Arguments);
        Assert.Equal("data", host.Environment["CYBERSECOS_DATA_DIR"]);
    }

    [Fact]
    public async Task PicksAnotherPortWhenConfiguredOneIsTaken()
    {
        var host = new FakeHost();
        host.BusyPorts.Add(8000);
        using var backend = Create(host);

        var address = await backend.EnsureRunningAsync(Configured);

        Assert.Equal(new Uri("http://127.0.0.1:51234/"), address);
        Assert.Contains("51234", host.Arguments);
    }

    [Fact]
    public async Task ReportsServerThatExitsDuringStart()
    {
        var host = new FakeHost { ExitOnStart = true };
        using var backend = Create(host);

        var error = await Assert.ThrowsAsync<BackendStartException>(() => backend.EnsureRunningAsync(Configured));

        Assert.Contains("server.log", error.Message);
        Assert.False(backend.OwnsServer);
    }

    [Fact]
    public async Task StopsTheServerItStarted()
    {
        var host = new FakeHost();
        var backend = Create(host);
        await backend.EnsureRunningAsync(Configured);

        backend.Dispose();

        Assert.True(host.Process!.Stopped);
        Assert.False(backend.OwnsServer);
    }
}
