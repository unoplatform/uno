using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RemoteControl.Host;

namespace Uno.UI.RemoteControl.DevServer.Tests.Helpers;

[TestClass]
public class AmbientRegistryTests
{
	[TestMethod]
	public void ResolveLocalApplicationDataPath_ReturnsAbsolutePath()
	{
		var path = AmbientRegistry.ResolveLocalApplicationDataPath();

		path.Should().NotBeNullOrWhiteSpace();
		Path.IsPathRooted(path).Should().BeTrue();
	}

	[TestMethod]
	public void UpdateIdeChannel_WhenRegistered_UpdatesTheActiveRegistration()
	{
		using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(b => { });
		var logger = loggerFactory.CreateLogger("test");
		var registry = new AmbientRegistry(logger);

		registry.Register(solution: @"D:\src\repo\Repo.sln", ppid: 123, httpPort: 61616, ideChannelId: null);
		try
		{
			registry.UpdateIdeChannel("new-channel");

			var updated = registry.GetActiveDevServerForPort(61616);
			updated.Should().NotBeNull();
			updated!.IdeChannelId.Should().Be("new-channel");
		}
		finally
		{
			registry.Unregister();
		}
	}

	[TestMethod]
	[Description("The CLI tool spawns hosts via direct process launch and writes a side-by-side " +
		"auxiliary record carrying metadata the host itself didn't register (notably ideChannelId, " +
		"absent from registrations produced by Uno.WinUI.DevServer versions older than the commit " +
		"that added IdeChannelId to AmbientRegistry). Disco's view must read this sidecar so " +
		"downstream consumers (uno.studio's DevServerLauncher) can adopt running hosts without " +
		"requiring a Uno.WinUI.DevServer upgrade.")]
	public void GetActiveDevServers_OverlaysSidecarIdeChannelIdWhenPrimaryRegistrationLacksIt()
	{
		using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(b => { });
		var logger = loggerFactory.CreateLogger("test");
		var registry = new AmbientRegistry(logger);

		// The "host" registers itself the way OLD Uno.WinUI.DevServer does — no ideChannelId.
		// Use this process's pid so the IsProcessRunning probe in GetActiveDevServers returns true.
		var hostPid = Environment.ProcessId;
		registry.Register(solution: @"D:\src\repo\Repo.sln", ppid: 999, httpPort: 51717, ideChannelId: null);
		try
		{
			// The CLI, which DID know the channel id (it passed --ideChannel to the host),
			// drops a sidecar record with that information.
			registry.WriteAuxiliaryRegistration(targetProcessId: hostPid, ideChannelId: "abc-123");

			var found = registry.GetActiveDevServerForPort(51717);
			found.Should().NotBeNull();
			found!.IdeChannelId.Should().Be("abc-123",
				"sidecar must overlay an ideChannelId when the primary registration left it null");
		}
		finally
		{
			registry.Unregister();
		}
	}

	[TestMethod]
	[Description("Sidecar must defer to the host's own registration when the host is recent enough " +
		"to register an ideChannelId itself. The host's value is authoritative; the sidecar is a " +
		"backfill mechanism for older hosts only.")]
	public void GetActiveDevServers_DoesNotOverrideExistingPrimaryIdeChannelId()
	{
		using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(b => { });
		var logger = loggerFactory.CreateLogger("test");
		var registry = new AmbientRegistry(logger);

		var hostPid = Environment.ProcessId;
		registry.Register(solution: @"D:\src\repo\Repo.sln", ppid: 999, httpPort: 51718, ideChannelId: "primary-channel");
		try
		{
			registry.WriteAuxiliaryRegistration(targetProcessId: hostPid, ideChannelId: "sidecar-channel");

			var found = registry.GetActiveDevServerForPort(51718);
			found.Should().NotBeNull();
			found!.IdeChannelId.Should().Be("primary-channel",
				"the host's own ideChannelId wins over the sidecar — the sidecar is only a backfill for old hosts");
		}
		finally
		{
			registry.Unregister();
		}
	}

	[TestMethod]
	[Description("Sidecars must be cleaned up when their target process is no longer running, " +
		"otherwise they accumulate forever and risk attaching stale ideChannelIds to recycled PIDs.")]
	public void GetActiveDevServers_CleansStaleSidecarsForDeadProcesses()
	{
		using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(b => { });
		var logger = loggerFactory.CreateLogger("test");
		var registry = new AmbientRegistry(logger);

		// Pick a PID that we're sure is dead.
		const int deadPid = int.MaxValue;
		registry.WriteAuxiliaryRegistration(targetProcessId: deadPid, ideChannelId: "stale-channel");

		// Trigger the read path which performs cleanup.
		_ = registry.GetActiveDevServers().ToList();

		var sidecarPath = Path.Combine(
			AmbientRegistry.ResolveLocalApplicationDataPath(),
			"Uno Platform", "DevServers",
			$"devserver-{deadPid}.aux.json");
		File.Exists(sidecarPath).Should().BeFalse("dead-PID sidecars must be removed on read");
	}
}
