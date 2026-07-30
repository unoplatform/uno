using System.Collections.Generic;
using Uno.UI.RemoteControl.VS.Helpers;

namespace Uno.UI.RemoteControl.DevServer.Tests.Helpers;

/// <summary>
/// Pins the discovery JSON contract that <see cref="DevServerHostDiscovery"/> consumes
/// to detect already-running DevServer hosts and skip the in-process spawn that would
/// otherwise produce a duplicate (the CLI-driven launch flow + EntryPoint's legacy
/// EnsureServerAsync currently each spawn one host for the same solution; this code path
/// is the bridge that lets EntryPoint defer to the CLI's host).
/// </summary>
[TestClass]
public class DevServerHostDiscoveryTests
{
	private const int CurrentDevenvPid = 6080;
	private const string SolutionPath = @"C:\src\UnoApp1\UnoApp1.sln";

	[TestMethod]
	[Description("Disco emits a JSON payload with `activeServers[]`. The parser must accept the camelCase " +
		"shape produced by the CLI (PropertyNamingPolicy = CamelCase).")]
	public void ParseDiscoPayload_Reads_ActiveServers()
	{
		var json = """
			{
			  "selectedSolutionPath": "C:\\src\\UnoApp1\\UnoApp1.sln",
			  "activeServers": [
			    {
			      "processId": 14376,
			      "port": 51369,
			      "parentProcessId": 6080,
			      "solutionPath": "C:\\src\\UnoApp1\\UnoApp1.sln",
			      "ideChannelId": null,
			      "isInWorkspace": true
			    }
			  ]
			}
			""";

		var servers = DevServerHostDiscovery.ParseActiveServers(json);

		servers.Should().HaveCount(1);
		var server = servers[0];
		server.ProcessId.Should().Be(14376);
		server.Port.Should().Be(51369);
		server.ParentProcessId.Should().Be(6080);
		server.SolutionPath.Should().Be(@"C:\src\UnoApp1\UnoApp1.sln");
		server.IdeChannelId.Should().BeNull();
	}

	[TestMethod]
	[Description("Match must be solution-path + parent-process scoped: a host running for THIS devenv's " +
		"solution. This is what lets EntryPoint adopt the CLI-spawned sibling without confusing it for " +
		"some other VS instance's host.")]
	public void Match_PrefersSolutionPath_AndDevenvParent()
	{
		var servers = new List<DiscoveredDevServer>
		{
			new() { ProcessId = 100, Port = 5000, ParentProcessId = 9999, SolutionPath = SolutionPath },        // wrong ppid
			new() { ProcessId = 200, Port = 5001, ParentProcessId = CurrentDevenvPid, SolutionPath = @"C:\Other.sln" }, // wrong solution
			new() { ProcessId = 300, Port = 5002, ParentProcessId = CurrentDevenvPid, SolutionPath = SolutionPath },     // right
		};

		var match = DevServerHostDiscovery.Match(servers, SolutionPath, CurrentDevenvPid);

		match.Should().NotBeNull();
		match!.ProcessId.Should().Be(300);
	}

	[TestMethod]
	[Description("Solution-path matching is case-insensitive on Windows (it's where the VS extension runs). " +
		"`C:\\Src\\App\\App.sln` and `c:\\src\\app\\app.sln` denote the same file.")]
	public void Match_IsCaseInsensitive_ForSolutionPath()
	{
		var servers = new List<DiscoveredDevServer>
		{
			new() { ProcessId = 1, Port = 5000, ParentProcessId = CurrentDevenvPid, SolutionPath = @"C:\SRC\UnoApp1\UnoApp1.sln" }
		};

		var match = DevServerHostDiscovery.Match(servers, @"C:\src\unoapp1\unoapp1.sln", CurrentDevenvPid);

		match.Should().NotBeNull();
	}

	[TestMethod]
	[Description("If no host matches BOTH solution and ppid, the match is null — better to spawn fresh than " +
		"to adopt a sibling that belongs to another VS instance or another solution.")]
	public void Match_ReturnsNull_WhenNoActiveServerSatisfiesBothCriteria()
	{
		var servers = new List<DiscoveredDevServer>
		{
			new() { ProcessId = 1, Port = 5000, ParentProcessId = CurrentDevenvPid, SolutionPath = @"C:\Other.sln" }
		};

		var match = DevServerHostDiscovery.Match(servers, SolutionPath, CurrentDevenvPid);

		match.Should().BeNull();
	}

	[TestMethod]
	[Description("Empty activeServers (no host running yet) returns null — caller must spawn.")]
	public void Match_ReturnsNull_WhenNoActiveServers()
	{
		var match = DevServerHostDiscovery.Match([], SolutionPath, CurrentDevenvPid);

		match.Should().BeNull();
	}

	[TestMethod]
	[Description("Malformed/empty disco JSON must not blow up the host start path. The parser returns " +
		"an empty list and the caller falls through to the existing spawn logic — degraded but recoverable.")]
	public void ParseDiscoPayload_Tolerates_MalformedJson()
	{
		var servers = DevServerHostDiscovery.ParseActiveServers("not-valid-json");

		servers.Should().BeEmpty();
	}

	[TestMethod]
	[Description("Missing `activeServers` key (older or partial CLI payload) returns empty rather than throwing.")]
	public void ParseDiscoPayload_Tolerates_MissingActiveServersKey()
	{
		var servers = DevServerHostDiscovery.ParseActiveServers("""{ "selectedSolutionPath": "X" }""");

		servers.Should().BeEmpty();
	}
}
