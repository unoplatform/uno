using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.DevServer.Cli.Tests;

[TestClass]
public class Given_DevServerProcessHelper
{
	[TestMethod]
	[Description("A host that exits before listening is reported right away, not after the whole readiness timeout.")]
	public async Task RunDirectProcessAsync_WhenHostExitsBeforeListening_ShouldNotWaitForTheTimeout()
	{
		var port = GetFreePort();
		// The helper probes the port it finds after --httpPort in the arguments; the shell ignores it.
		var startInfo = OperatingSystem.IsWindows()
			? new ProcessStartInfo("cmd.exe", $"/c exit 3 --httpPort {port}")
			: new ProcessStartInfo("/bin/sh", $"-c \"exit 3\" --httpPort {port}");
		startInfo.UseShellExecute = false;
		startInfo.CreateNoWindow = true;

		var stopwatch = Stopwatch.StartNew();
		var result = await DevServerProcessHelper.RunDirectProcessAsync(startInfo, NullLogger.Instance, readinessTimeoutMs: 30_000);
		stopwatch.Stop();

		result.Failure.Should().Be(DirectSpawnFailure.DiedBeforeReady);
		result.HostExitCode.Should().Be(3);
		stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10));
	}

	private static int GetFreePort()
	{
		var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		var port = ((IPEndPoint)listener.LocalEndpoint).Port;
		listener.Stop();
		return port;
	}
}
