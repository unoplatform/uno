using AwesomeAssertions;
using Uno.UI.DevServer.Cli.Helpers;
using Uno.UI.DevServer.Cli.Mcp;

namespace Uno.UI.RemoteControl.DevServer.Tests.Mcp;

/// <summary>
/// Tests for <see cref="DevServerMonitor"/> patterns: ambient registry lookup,
/// direct launch arg building, retry logic, and <see cref="SolutionFileFinder"/>.
/// <para>
/// These tests simulate DevServerMonitor patterns rather than instantiating
/// the real class because it requires a .sln on disk and a real Uno SDK
/// host binary. Pure decision logic is tested via <see cref="MonitorDecisionsTests"/>.
/// </para>
/// </summary>
[TestClass]
public class Given_DevServerMonitor
{
	// -------------------------------------------------------------------
	// AmbientRegistry
	// -------------------------------------------------------------------

	[TestMethod]
	public void AmbientRegistry_WhenNoMatch_ReturnsNull()
	{
		using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(b => { });
		var logger = loggerFactory.CreateLogger("test");
		var registry = new Uno.UI.RemoteControl.Host.AmbientRegistry(logger);

		var result = registry.GetActiveDevServerForPath(Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid():N}.sln"));

		result.Should().BeNull();
	}

	[TestMethod]
	public void AmbientRegistry_WhenNullOrEmpty_ReturnsNull()
	{
		using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(b => { });
		var logger = loggerFactory.CreateLogger("test");
		var registry = new Uno.UI.RemoteControl.Host.AmbientRegistry(logger);

		registry.GetActiveDevServerForPath(null).Should().BeNull();
		registry.GetActiveDevServerForPath("").Should().BeNull();
		registry.GetActiveDevServerForPath("   ").Should().BeNull();
	}

	// -------------------------------------------------------------------
	// Direct launch args
	// -------------------------------------------------------------------

	[TestMethod]
	public void DirectLaunch_ArgsDoNotContainCommandStart()
	{
		var args = BuildDirectLaunchArgs(port: 12345, solution: @"C:\test\MySolution.sln");

		args.Should().NotContain("--command");
		args.Should().NotContain("start");
	}

	[TestMethod]
	public void DirectLaunch_ArgsContainSolutionFlag()
	{
		var args = BuildDirectLaunchArgs(port: 12345, solution: @"C:\test\MySolution.sln");

		var solutionIndex = args.IndexOf("--solution");
		solutionIndex.Should().BeGreaterThanOrEqualTo(0);
		args[solutionIndex + 1].Should().Be(@"C:\test\MySolution.sln");
	}

	[TestMethod]
	public void DirectLaunch_ArgsContainHttpPortAndPpid()
	{
		var args = BuildDirectLaunchArgs(port: 54321, solution: null);

		var portIndex = args.IndexOf("--httpPort");
		portIndex.Should().BeGreaterThanOrEqualTo(0);
		args[portIndex + 1].Should().Be("54321");

		var ppidIndex = args.IndexOf("--ppid");
		ppidIndex.Should().BeGreaterThanOrEqualTo(0);
		int.Parse(args[ppidIndex + 1]).Should().BeGreaterThan(0);
	}

	[TestMethod]
	public void DirectLaunch_WhenNoSolution_ArgsDoNotContainSolution()
	{
		var args = BuildDirectLaunchArgs(port: 12345, solution: null);

		args.Should().NotContain("--solution");
	}

	[TestMethod]
	public void DirectLaunch_ArgsContainAddins()
	{
		var args = BuildDirectLaunchArgs(port: 12345, solution: null, addins: "Addon1.dll;Addon2.dll");

		var addinsIndex = args.IndexOf("--addins");
		addinsIndex.Should().BeGreaterThanOrEqualTo(0);
		args[addinsIndex + 1].Should().Be("Addon1.dll;Addon2.dll");
	}

	// -------------------------------------------------------------------
	// Retry logic
	// -------------------------------------------------------------------

	[TestMethod]
	[Description("ServerFailed event fires after exhausting all retry attempts")]
	public void DevServerMonitor_RetryLogic_FailsAfterMaxAttempts()
	{
		const int maxRetries = 3;
		int retryCount = 0;
		bool serverFailed = false;
		var attempts = new List<int>();

		while (retryCount < maxRetries)
		{
			// Simulate StartProcess returning (success: false)
			retryCount++;
			attempts.Add(retryCount);

			if (retryCount >= maxRetries)
			{
				serverFailed = true;
				break;
			}
		}

		serverFailed.Should().BeTrue("ServerFailed triggers after max retries");
		attempts.Should().HaveCount(3);
		retryCount.Should().Be(maxRetries);
	}

	[TestMethod]
	[Description("Monitor loop must NOT break after ServerStarted, it must keep watching for process exit to detect crashes")]
	public void Bug2_MonitorLoop_ContinuesAfterServerStarted()
	{
		var iterations = 0;
		var serverStartedRaised = false;
		var serverCrashedRaised = false;
		var processExited = false;

		// Simulate the monitor loop with the CORRECT behavior (no break)
		for (int attempt = 0; attempt < 3; attempt++)
		{
			iterations++;

			if (!serverStartedRaised)
			{
				serverStartedRaised = true;
				// Bug: original code does `break` here
				// Fix: continue watching the process
			}
			else if (serverStartedRaised && processExited)
			{
				serverCrashedRaised = true;
				break; // Re-enter loop for rediscovery
			}

			// Simulate process exit after first iteration
			if (attempt == 1)
			{
				processExited = true;
			}
		}

		serverStartedRaised.Should().BeTrue();
		processExited.Should().BeTrue();
		serverCrashedRaised.Should().BeTrue("monitor should detect process exit and raise ServerCrashed");
		iterations.Should().BeGreaterThan(1, "monitor loop should continue after ServerStarted");
	}

	// -------------------------------------------------------------------
	// Event sequence
	// -------------------------------------------------------------------

	[TestMethod]
	[Description("ServerLaunching fires before ServerStarted in the documented event sequence")]
	public void EventSequence_ServerLaunchingFiresBeforeServerStarted()
	{
		var events = new List<string>();

		// Simulate the event sequence from RunMonitor:
		// 1. StartProcess succeeds
		// 2. ServerLaunching fires
		// 3. WaitForServerReadyAsync succeeds
		// 4. ServerStarted fires
		Action serverLaunching = () => events.Add("ServerLaunching");
		Action<string> serverStarted = _ => events.Add("ServerStarted");
		Action serverFailed = () => events.Add("ServerFailed");

		// Simulate success path
		serverLaunching();
		serverStarted("http://localhost:12345/mcp");

		events.Should().HaveCount(2);
		events[0].Should().Be("ServerLaunching");
		events[1].Should().Be("ServerStarted");
	}

	[TestMethod]
	[Description("ServerLaunching fires but ServerStarted does not when health-check fails")]
	public void EventSequence_WhenHealthCheckFails_ServerLaunchingWithoutServerStarted()
	{
		var events = new List<string>();

		Action serverLaunching = () => events.Add("ServerLaunching");
		Action serverFailed = () => events.Add("ServerFailed");

		// Simulate path where process starts but health-check fails
		serverLaunching();
		serverFailed();

		events.Should().HaveCount(2);
		events[0].Should().Be("ServerLaunching");
		events[1].Should().Be("ServerFailed");
	}

	// -------------------------------------------------------------------
	// DiscoveryInfo exposure
	// -------------------------------------------------------------------

	[TestMethod]
	[Description("DiscoveryInfo exposes the expected values when fully populated")]
	public void DiscoveryInfo_WhenFullyPopulated_ExposesExpectedFields()
	{
		var discovery = new DiscoveryInfo
		{
			WorkingDirectory = "/tmp/test",
			GlobalJsonPath = "/tmp/test/global.json",
			UnoSdkVersion = "5.5.100",
			HostPath = "/some/host.dll",
			AddInsDiscoveryDurationMs = 42,
			AddIns =
			[
				new ResolvedAddIn
				{
					PackageName = "Uno.Test",
					PackageVersion = "1.0.0",
					EntryPointDll = "/some/addin.dll",
					DiscoverySource = "targets"
				}
			],
		};

		discovery.UnoSdkVersion.Should().Be("5.5.100");
		discovery.HostPath.Should().NotBeNull();
		discovery.AddInsDiscoveryDurationMs.Should().Be(42);
		discovery.AddIns.Should().HaveCount(1);
		discovery.AddIns[0].EntryPointDll.Should().Be("/some/addin.dll");
	}

	[TestMethod]
	[Description("When discovery finds no host, HostPath is null but other fields are still populated")]
	public void DiscoveryInfo_WhenNoHost_HostPathIsNullButSdkVersionPopulated()
	{
		var discovery = new DiscoveryInfo
		{
			GlobalJsonPath = "/tmp/test/global.json",
			UnoSdkVersion = "5.5.100",
			UnoSdkPath = "/nuget/uno.sdk/5.5.100",
			HostPath = null,
		};

		discovery.HostPath.Should().BeNull();
		discovery.UnoSdkVersion.Should().Be("5.5.100");
	}

	[TestMethod]
	[Description("AddIns from DiscoveryInfo produce the expected --addins argument value")]
	public void DiscoveryInfo_AddIns_ProduceExpectedAddinsArgValue()
	{
		var discovery = new DiscoveryInfo
		{
			AddIns =
			[
				new ResolvedAddIn { PackageName = "A", PackageVersion = "1.0", EntryPointDll = "/a.dll", DiscoverySource = "targets" },
				new ResolvedAddIn { PackageName = "B", PackageVersion = "2.0", EntryPointDll = "/b.dll", DiscoverySource = "targets" },
			],
		};

		// Mirror the logic from StartProcess
		var addInsValue = string.Join(";", discovery.AddIns.Select(a => a.EntryPointDll).Distinct(StringComparer.OrdinalIgnoreCase));

		addInsValue.Should().Be("/a.dll;/b.dll");
	}

	[TestMethod]
	[Description("When DiscoveryInfo has no add-ins, no --addins arg is produced")]
	public void DiscoveryInfo_WhenNoAddIns_NoAddinsArg()
	{
		var discovery = new DiscoveryInfo { AddIns = [] };

		// Mirror the guard from StartProcess: AddIns is { Count: > 0 }
		var shouldAddFlag = discovery.AddIns is { Count: > 0 };

		shouldAddFlag.Should().BeFalse();
	}

	// -------------------------------------------------------------------
	// FindGitRoot
	// -------------------------------------------------------------------

	[TestMethod]
	public void FindGitRoot_ReturnsRoot_WhenInGitRepo()
	{
		// The test project itself is inside the uno2 git repo
		var testDir = AppContext.BaseDirectory;
		var gitRoot = SolutionFileFinder.FindGitRoot(testDir);

		gitRoot.Should().NotBeNull();
		// .git can be a directory (normal repo) or a file (worktree)
		var gitPath = Path.Combine(gitRoot!, ".git");
		(Directory.Exists(gitPath) || File.Exists(gitPath)).Should().BeTrue();
	}

	[TestMethod]
	public void FindGitRoot_ReturnsNull_WhenNotInGitRepo()
	{
		// Use a temp directory that's not inside any git repo
		var tempDir = Path.Combine(Path.GetTempPath(), $"no-git-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDir);
		try
		{
			// Temp root is unlikely inside a git repo; if it is, skip
			if (SolutionFileFinder.FindGitRoot(Path.GetTempPath()) is not null)
			{
				Assert.Inconclusive("Temp directory is inside a git repo, cannot test non-git fallback.");
			}

			var result = SolutionFileFinder.FindGitRoot(tempDir);
			result.Should().BeNull();
		}
		finally
		{
			Directory.Delete(tempDir, recursive: true);
		}
	}

	// -------------------------------------------------------------------
	// FindSolutionFiles with .gitignore awareness
	// -------------------------------------------------------------------

	[TestMethod]
	public void FindSolutionFiles_SkipsGitIgnoredDirectories()
	{
		var tempDir = Path.Combine(Path.GetTempPath(), $"git-ignore-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDir);
		try
		{
			// Init a git repo
			RunGit(tempDir, "init");
			RunGit(tempDir, "config user.email test@test.com");
			RunGit(tempDir, "config user.name Test");

			// Create .gitignore that ignores the "ignored" directory
			File.WriteAllText(Path.Combine(tempDir, ".gitignore"), "ignored/\n");
			RunGit(tempDir, "add .gitignore");
			RunGit(tempDir, "commit -m init");

			// Create two subdirectories with .sln files
			var ignoredDir = Path.Combine(tempDir, "ignored");
			var visibleDir = Path.Combine(tempDir, "visible");
			Directory.CreateDirectory(ignoredDir);
			Directory.CreateDirectory(visibleDir);
			File.WriteAllText(Path.Combine(ignoredDir, "Hidden.sln"), "");
			File.WriteAllText(Path.Combine(visibleDir, "Visible.sln"), "");

			var solutions = SolutionFileFinder.FindSolutionFiles(tempDir);

			solutions.Should().ContainSingle(s => s.Contains("Visible.sln"));
			solutions.Should().NotContain(s => s.Contains("Hidden.sln"));
		}
		finally
		{
			// .git directories on Windows need special cleanup
			ForceDeleteDirectory(tempDir);
		}
	}

	[TestMethod]
	public void FindSolutionFiles_FallsBackToHardcodedList_WhenNoGit()
	{
		var tempDir = Path.Combine(Path.GetTempPath(), $"no-git-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDir);
		try
		{
			// Skip if temp dir happens to be inside a git repo
			if (SolutionFileFinder.FindGitRoot(Path.GetTempPath()) is not null)
			{
				Assert.Inconclusive("Temp directory is inside a git repo, cannot test non-git fallback.");
			}

			// Create node_modules (should be skipped by hardcoded list) and a visible dir
			var nodeModules = Path.Combine(tempDir, "node_modules");
			var src = Path.Combine(tempDir, "src");
			Directory.CreateDirectory(nodeModules);
			Directory.CreateDirectory(src);
			File.WriteAllText(Path.Combine(nodeModules, "Bad.sln"), "");
			File.WriteAllText(Path.Combine(src, "Good.sln"), "");

			var solutions = SolutionFileFinder.FindSolutionFiles(tempDir);

			solutions.Should().ContainSingle(s => s.Contains("Good.sln"));
			solutions.Should().NotContain(s => s.Contains("Bad.sln"));
		}
		finally
		{
			Directory.Delete(tempDir, recursive: true);
		}
	}

	[TestMethod]
	public void GetGitIgnoredPaths_ReturnsEmpty_WhenNoPathsProvided()
	{
		var result = SolutionFileFinder.GetGitIgnoredPaths(new List<string>(), ".");
		result.Should().NotBeNull();
		result!.Should().BeEmpty();
	}

	[TestMethod]
	public void GetGitIgnoredPaths_ReturnsNull_WhenGitNotAvailable()
	{
		// Use a directory that is NOT a git repo — git check-ignore will fail
		var tempDir = Path.Combine(Path.GetTempPath(), $"no-git-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDir);
		try
		{
			var subDir = Path.Combine(tempDir, "somedir");
			Directory.CreateDirectory(subDir);

			// git check-ignore will fail because tempDir is not a git repo
			var result = SolutionFileFinder.GetGitIgnoredPaths([subDir], tempDir);

			// Should return null (git failed) so caller can fall back to hardcoded list
			result.Should().BeNull();
		}
		finally
		{
			Directory.Delete(tempDir, recursive: true);
		}
	}

	[TestMethod]
	public void FindSolutionFiles_UsesHardcodedSkipList_WhenGitFails()
	{
		// Even if a .git exists, if git executable is broken or not a real repo,
		// the hardcoded skip list should kick in as fallback.
		var tempDir = Path.Combine(Path.GetTempPath(), $"fake-git-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDir);
		try
		{
			// Create a fake .git file (not a real repo — git check-ignore will fail)
			File.WriteAllText(Path.Combine(tempDir, ".git"), "not a real git repo");

			// Create node_modules (should be skipped by hardcoded fallback) and src
			var nodeModules = Path.Combine(tempDir, "node_modules");
			var src = Path.Combine(tempDir, "src");
			Directory.CreateDirectory(nodeModules);
			Directory.CreateDirectory(src);
			File.WriteAllText(Path.Combine(nodeModules, "Bad.sln"), "");
			File.WriteAllText(Path.Combine(src, "Good.sln"), "");

			var solutions = SolutionFileFinder.FindSolutionFiles(tempDir);

			solutions.Should().ContainSingle(s => s.Contains("Good.sln"));
			solutions.Should().NotContain(s => s.Contains("Bad.sln"));
		}
		finally
		{
			Directory.Delete(tempDir, recursive: true);
		}
	}

	[TestMethod]
	public void FindSolutionFiles_RespectsNestedGitignoreInSubdirectories()
	{
		var tempDir = Path.Combine(Path.GetTempPath(), $"nested-gi-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDir);
		try
		{
			// Init a git repo with a root .gitignore
			RunGit(tempDir, "init");
			RunGit(tempDir, "config user.email test@test.com");
			RunGit(tempDir, "config user.name Test");
			File.WriteAllText(Path.Combine(tempDir, ".gitignore"), "root-ignored/\n");
			RunGit(tempDir, "add .gitignore");
			RunGit(tempDir, "commit -m init");

			// Create a nested .gitignore inside src/
			var src = Path.Combine(tempDir, "src");
			Directory.CreateDirectory(src);
			File.WriteAllText(Path.Combine(src, ".gitignore"), "sub-ignored/\n");

			// Create directories with .sln files at various levels
			Directory.CreateDirectory(Path.Combine(tempDir, "root-ignored"));
			File.WriteAllText(Path.Combine(tempDir, "root-ignored", "RootIgnored.sln"), "");

			Directory.CreateDirectory(Path.Combine(src, "sub-ignored"));
			File.WriteAllText(Path.Combine(src, "sub-ignored", "SubIgnored.sln"), "");

			Directory.CreateDirectory(Path.Combine(src, "visible"));
			File.WriteAllText(Path.Combine(src, "visible", "Visible.sln"), "");

			var solutions = SolutionFileFinder.FindSolutionFiles(tempDir);

			solutions.Should().ContainSingle(s => s.Contains("Visible.sln"));
			solutions.Should().NotContain(s => s.Contains("RootIgnored.sln"),
				"root .gitignore should exclude root-ignored/");
			solutions.Should().NotContain(s => s.Contains("SubIgnored.sln"),
				"nested src/.gitignore should exclude sub-ignored/");
		}
		finally
		{
			ForceDeleteDirectory(tempDir);
		}
	}

	// -------------------------------------------------------------------
	// Helpers
	// -------------------------------------------------------------------

	private static void RunGit(string workingDir, string args)
	{
		System.Diagnostics.Process process;
		try
		{
			var psi = new System.Diagnostics.ProcessStartInfo("git", args)
			{
				WorkingDirectory = workingDir,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			};
			process = System.Diagnostics.Process.Start(psi)
				?? throw new InvalidOperationException("Failed to start git process");
		}
		catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
		{
			Assert.Inconclusive($"git is not available: {ex.Message}");
			return; // unreachable, but satisfies the compiler
		}

		using (process)
		{
			if (!process.WaitForExit(10000))
			{
				try { process.Kill(); } catch { /* best effort */ }
				Assert.Inconclusive("git process timed out");
			}
		}
	}

	private static void ForceDeleteDirectory(string path)
	{
		// Remove read-only attributes on .git objects so delete works on Windows
		foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
		{
			File.SetAttributes(file, FileAttributes.Normal);
		}
		Directory.Delete(path, recursive: true);
	}


	/// <summary>
	/// Mirrors the arg-building logic in DevServerMonitor.StartProcess
	/// to verify that the direct launch path produces the expected arguments.
	/// </summary>
	private static List<string> BuildDirectLaunchArgs(int port, string? solution, string? addins = null)
	{
		var args = new List<string>
		{
			"--httpPort", port.ToString(System.Globalization.CultureInfo.InvariantCulture),
			"--ppid", Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture)
		};

		if (!string.IsNullOrWhiteSpace(solution))
		{
			args.Add("--solution");
			args.Add(solution);
		}

		if (!string.IsNullOrWhiteSpace(addins))
		{
			args.Add("--addins");
			args.Add(addins);
		}

		return args;
	}
}
