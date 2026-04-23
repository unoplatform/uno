using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AwesomeAssertions;
using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.DevServer.Cli.Tests;

[TestClass]
public class Given_UnoToolsLocator
{
	[TestMethod]
	[DataRow("net10.0", 10, 0)]
	[DataRow("net9.0", 9, 0)]
	[DataRow("net11.0", 11, 0)]
	[DataRow("net5.0", 5, 0)]
	[Description("TryParseNetTfm must accept any net{major}.{minor} moniker with major >= 5. " +
		"Guards forward compatibility — a net11.0 or net12.0 host directory shipped by a future " +
		"Uno.Sdk must be recognised without a code change, otherwise adding a new TFM would " +
		"silently break the resolver the way the net9 → net10 transition did for Uno.Sdk " +
		"6.1.0-dev.30 (issue followed up from uno.studio PR #1024 / Chefs sample).")]
	public void When_Parsing_Valid_Net_Tfm_Then_Returns_Version(string input, int expectedMajor, int expectedMinor)
	{
		var success = UnoToolsLocator.TryParseNetTfm(input, out var major, out var minor);

		success.Should().BeTrue();
		major.Should().Be(expectedMajor);
		minor.Should().Be(expectedMinor);
	}

	[TestMethod]
	[DataRow(null)]
	[DataRow("")]
	[DataRow("netstandard2.0")]
	[DataRow("netcoreapp3.1")]
	[DataRow("net472")]
	[DataRow("net10.0-windows10.0.19041")]
	[DataRow("net4.8")]
	[DataRow("random-folder")]
	[Description("TryParseNetTfm must reject monikers the resolver cannot compare on a " +
		"single (major, minor) axis: pre-net5 frameworks (netstandard, netcoreapp, netfx), " +
		"platform-suffixed TFMs (net10.0-windows…), and anything that doesn't parse as " +
		"net{int}.{int}. Unexpected subdirectories under tools/rc/host/ must be ignored " +
		"rather than crashing discovery.")]
	public void When_Parsing_Invalid_Or_Non_Net_Tfm_Then_Returns_False(string? input)
	{
		var success = UnoToolsLocator.TryParseNetTfm(input, out _, out _);

		success.Should().BeFalse();
	}

	[TestMethod]
	[Description("Exact TFM match always wins over fallback candidates. Mandatory: we must not " +
		"accidentally \"fall back\" from net10 to net9 when the host actually ships a net10 " +
		"directory — that would forego all forward-targeting fixes the host gains in each new " +
		"major.")]
	public void When_Requested_Tfm_Is_Available_Then_Returns_Exact_Match()
	{
		var tfms = new[] { "net8.0", "net9.0", "net10.0" };

		UnoToolsLocator.TryResolveHostTfm(tfms, "net10.0").Should().Be("net10.0");
	}

	[TestMethod]
	[Description("When the requested TFM is not shipped (older Uno.Sdk predating the dotnet SDK " +
		"currently installed), return the highest available TFM <= the request. Regresses the " +
		"Chefs.sln case from the PR follow-up: Uno.Sdk 6.1.0-dev.30 only ships net8 / net9 hosts " +
		"but the user has .NET 10.0.201 — we must pick net9 so the host starts (paired with " +
		"DOTNET_ROLL_FORWARD=Major for the runtime side).")]
	public void When_Requested_Tfm_Is_Missing_Then_Falls_Back_To_Highest_Compatible()
	{
		var tfms = new[] { "net8.0", "net9.0" };

		UnoToolsLocator.TryResolveHostTfm(tfms, "net10.0").Should().Be("net9.0");
	}

	[TestMethod]
	[Description("Future-proofing for net11 and later: if the user still has .NET 10 but the " +
		"package now ships net11 only, the resolver must refuse to pick net11 because a net10 " +
		"runtime cannot load a net11 assembly. Returning null here lets the caller surface a " +
		"clean \"no compatible TFM\" error instead of handing back a host that would simply " +
		"crash at startup.")]
	public void When_Only_Higher_Tfms_Are_Available_Then_Returns_Null()
	{
		var tfms = new[] { "net11.0", "net12.0" };

		UnoToolsLocator.TryResolveHostTfm(tfms, "net10.0").Should().BeNull();
	}

	[TestMethod]
	[Description("One-major cap on the fallback: DOTNET_ROLL_FORWARD=Major (the env var the " +
		"spawn path sets when the resolver returns a fallback host) allows exactly one major " +
		"version jump. Picking a two-major-or-more gap (e.g. net5 host under a net10 runtime) " +
		"would hand back a binary the runtime cannot actually load and make the roll-forward " +
		"claim in HostLaunchPlan a lie. Regression guard against skeptic review of " +
		"https://github.com/unoplatform/uno/pull/23124.")]
	public void When_Only_Tfms_More_Than_One_Major_Below_Exist_Then_Returns_Null()
	{
		var tfms = new[] { "net5.0", "net6.0", "net7.0" };

		UnoToolsLocator.TryResolveHostTfm(tfms, "net10.0").Should().BeNull();
	}

	[TestMethod]
	[Description("Cap boundary: when the requested runtime is net10, a net9 host is acceptable " +
		"(one-major jump, covered by DOTNET_ROLL_FORWARD=Major) but a net8 host is not. The " +
		"resolver must pick net9 even when net8 is also present.")]
	public void When_Both_One_And_Two_Majors_Below_Are_Available_Then_Picks_One_Major_Below()
	{
		var tfms = new[] { "net8.0", "net9.0" };

		UnoToolsLocator.TryResolveHostTfm(tfms, "net10.0").Should().Be("net9.0");
	}

	[TestMethod]
	[Description("EnumerateHostTfms must sort numerically by (major, minor), not by ordinal " +
		"string. Ordinal sort places `net10.0` before `net9.0` because '1' < '9', which would " +
		"make the user-facing \"present TFMs\" list in the error log confusing and would break " +
		"any future consumer that assumes the returned array is ordered by version.")]
	public void When_Host_Root_Contains_Mixed_Majors_Then_Result_Is_Sorted_By_Version()
	{
		var root = Path.Combine(Path.GetTempPath(), "uno-tests-host-sort-" + Guid.NewGuid().ToString("N"));
		try
		{
			Directory.CreateDirectory(Path.Combine(root, "net9.0"));
			Directory.CreateDirectory(Path.Combine(root, "net10.0"));
			Directory.CreateDirectory(Path.Combine(root, "net8.0"));

			UnoToolsLocator.EnumerateHostTfms(root).Should().Equal("net8.0", "net9.0", "net10.0");
		}
		finally
		{
			if (Directory.Exists(root))
			{
				Directory.Delete(root, recursive: true);
			}
		}
	}

	[TestMethod]
	[Description("TryResolveHostLaunchPlan picks the fallback net9 host when the exact net10 " +
		"directory is missing and flags RequiresMajorRollForward=true so the spawn path will " +
		"set DOTNET_ROLL_FORWARD=Major. End-to-end guard for issue #23123 (Chefs.sln + " +
		"Uno.Sdk 6.1.0-dev.30 + .NET 10 SDK): `ResolveHostExecutableAsync` and `DiscoverAsync` " +
		"both go through this resolver, so one fixture pins both entry points.")]
	public void When_Exact_Tfm_Missing_And_Fallback_Exists_Then_Plan_Flags_RollForward()
	{
		var pkg = Path.Combine(Path.GetTempPath(), "uno-tests-pkg-" + Guid.NewGuid().ToString("N"));
		try
		{
			var hostDir = Path.Combine(pkg, "tools", "rc", "host", "net9.0");
			Directory.CreateDirectory(hostDir);
			File.WriteAllText(Path.Combine(hostDir, "Uno.UI.RemoteControl.Host.dll"), "stub");

			var plan = UnoToolsLocator.TryResolveHostLaunchPlan(pkg, "net10.0");

			plan.Should().NotBeNull();
			plan!.HostPath.Should().Be(Path.Combine(hostDir, "Uno.UI.RemoteControl.Host.dll"));
			plan.RequiresMajorRollForward.Should().BeTrue();
		}
		finally
		{
			if (Directory.Exists(pkg))
			{
				Directory.Delete(pkg, recursive: true);
			}
		}
	}

	[TestMethod]
	[Description("TryResolveHostLaunchPlan with an exact TFM match must not flag the " +
		"RollForward requirement: pinned majors (via global.json or DOTNET_ROLL_FORWARD ambient) " +
		"would otherwise be silently overridden on every exact-match launch.")]
	public void When_Exact_Tfm_Available_Then_Plan_Does_Not_Flag_RollForward()
	{
		var pkg = Path.Combine(Path.GetTempPath(), "uno-tests-pkg-exact-" + Guid.NewGuid().ToString("N"));
		try
		{
			var hostDir = Path.Combine(pkg, "tools", "rc", "host", "net10.0");
			Directory.CreateDirectory(hostDir);
			File.WriteAllText(Path.Combine(hostDir, "Uno.UI.RemoteControl.Host.exe"), "stub");

			var plan = UnoToolsLocator.TryResolveHostLaunchPlan(pkg, "net10.0");

			plan.Should().NotBeNull();
			plan!.HostPath.Should().Be(Path.Combine(hostDir, "Uno.UI.RemoteControl.Host.exe"));
			plan.RequiresMajorRollForward.Should().BeFalse();
		}
		finally
		{
			if (Directory.Exists(pkg))
			{
				Directory.Delete(pkg, recursive: true);
			}
		}
	}

	[TestMethod]
	[Description(".exe shim is preferred over the managed .dll when both exist in the fallback " +
		"host directory — matches the resolution order of ResolveHostExecutableAsync prior to " +
		"the fallback refactor. Guards against a regression where the fallback path silently " +
		"switched to the .dll and bypassed the native shim on Windows.")]
	public void When_Both_Exe_And_Dll_Present_In_Fallback_Directory_Then_Exe_Wins()
	{
		var pkg = Path.Combine(Path.GetTempPath(), "uno-tests-pkg-exe-wins-" + Guid.NewGuid().ToString("N"));
		try
		{
			var hostDir = Path.Combine(pkg, "tools", "rc", "host", "net9.0");
			Directory.CreateDirectory(hostDir);
			File.WriteAllText(Path.Combine(hostDir, "Uno.UI.RemoteControl.Host.exe"), "stub-exe");
			File.WriteAllText(Path.Combine(hostDir, "Uno.UI.RemoteControl.Host.dll"), "stub-dll");

			var plan = UnoToolsLocator.TryResolveHostLaunchPlan(pkg, "net10.0");

			plan.Should().NotBeNull();
			plan!.HostPath.Should().EndWith(".exe");
			plan.RequiresMajorRollForward.Should().BeTrue();
		}
		finally
		{
			if (Directory.Exists(pkg))
			{
				Directory.Delete(pkg, recursive: true);
			}
		}
	}

	[TestMethod]
	[Description("Defensive: an empty host directory (package extracted but layout corrupted) " +
		"must surface as null — the caller turns that into a clean \"no compatible TFM\" error " +
		"instead of returning a launch plan with a bogus path.")]
	public void When_Host_Directory_Exists_But_Is_Empty_Then_Plan_Is_Null()
	{
		var pkg = Path.Combine(Path.GetTempPath(), "uno-tests-pkg-empty-" + Guid.NewGuid().ToString("N"));
		try
		{
			var hostDir = Path.Combine(pkg, "tools", "rc", "host", "net10.0");
			Directory.CreateDirectory(hostDir);

			UnoToolsLocator.TryResolveHostLaunchPlan(pkg, "net10.0").Should().BeNull();
		}
		finally
		{
			if (Directory.Exists(pkg))
			{
				Directory.Delete(pkg, recursive: true);
			}
		}
	}

	[TestMethod]
	[Description("Integration guard for DevServerProcessHelper.CreateDotnetProcessStartInfo: " +
		"when the caller opts in via enableMajorRollForward=true, DOTNET_ROLL_FORWARD=Major " +
		"must actually land on the spawned process' Environment dictionary. Without this " +
		"assertion, nothing in the suite actually exercises the roll-forward contract — a " +
		"refactor could silently drop the env var and every other test would still pass.")]
	public void When_EnableMajorRollForward_Is_True_Then_DOTNET_ROLL_FORWARD_Major_Is_Set_On_Process_Environment()
	{
		var psi = DevServerProcessHelper.CreateDotnetProcessStartInfo(
			hostPath: "stub.dll",
			arguments: Array.Empty<string>(),
			workingDirectory: Path.GetTempPath(),
			redirectOutput: true,
			enableMajorRollForward: true);

		psi.Environment.Should().ContainKey("DOTNET_ROLL_FORWARD")
			.WhoseValue.Should().Be("Major");
	}

	[TestMethod]
	[Description("Symmetric guard: exact TFM match (no fallback) must leave the pinned-major " +
		"scenario alone — the helper must not unconditionally force DOTNET_ROLL_FORWARD=Major " +
		"when the caller opted out. If the parent already sets it explicitly we propagate it; " +
		"what we forbid is the helper overwriting that to Major on its own.")]
	public void When_EnableMajorRollForward_Is_False_Then_Helper_Does_Not_Force_Major()
	{
		var psi = DevServerProcessHelper.CreateDotnetProcessStartInfo(
			"stub.dll", Array.Empty<string>(), Path.GetTempPath(),
			redirectOutput: true, enableMajorRollForward: false);

		if (psi.Environment.TryGetValue("DOTNET_ROLL_FORWARD", out var value))
		{
			value.Should().NotBe("Major");
		}
	}

	[TestMethod]
	[Description("When several compatible candidates exist we pick the highest of them, not the " +
		"lowest — sticking with the most recent host surface area the package happens to ship. " +
		"Covers the happy path of the fallback when multiple older TFMs are present.")]
	public void When_Multiple_Compatible_Tfms_Exist_Then_Returns_Highest()
	{
		var tfms = new[] { "net8.0", "net9.0" };

		UnoToolsLocator.TryResolveHostTfm(tfms, "net10.0").Should().Be("net9.0");
	}

	[TestMethod]
	[Description("Minor-version ordering: net10.5 must be preferred over net10.0 when both are " +
		"present and the request is net10.5 or higher. Minor differences are unlikely in practice " +
		"(uno.winui.devserver currently only ships net{major}.0) but the resolver supports them " +
		"so that hypothetical future service-release builds are handled correctly.")]
	public void When_Candidates_Differ_By_Minor_Then_Highest_Minor_Wins()
	{
		var tfms = new[] { "net10.0", "net10.5" };

		UnoToolsLocator.TryResolveHostTfm(tfms, "net10.5").Should().Be("net10.5");
		UnoToolsLocator.TryResolveHostTfm(tfms, "net11.0").Should().Be("net10.5");
	}

	[TestMethod]
	[Description("Empty tools/rc/host directories must surface as null rather than NRE-ing — " +
		"callers translate null into a user-facing \"no compatible TFM available\" error that " +
		"points at a likely cause (missing/partial package restore).")]
	public void When_No_Tfms_Are_Available_Then_Returns_Null()
	{
		UnoToolsLocator.TryResolveHostTfm(Array.Empty<string>(), "net10.0").Should().BeNull();
	}

	[TestMethod]
	[Description("EnumerateHostTfms must silently return an empty array when the host directory " +
		"itself is missing — this happens for malformed packages and during cache eviction. Any " +
		"other behaviour would force the caller to wrap the enumeration in its own Directory.Exists " +
		"guard, duplicating the check.")]
	public void When_Host_Root_Directory_Does_Not_Exist_Then_Returns_Empty()
	{
		var missing = Path.Combine(Path.GetTempPath(), "uno-tests-does-not-exist-" + Guid.NewGuid().ToString("N"));

		UnoToolsLocator.EnumerateHostTfms(missing).Should().BeEmpty();
	}

	[TestMethod]
	[Description("EnumerateHostTfms must skip non-net TFM directories that happen to exist under " +
		"tools/rc/host/. Older Uno package versions occasionally carried legacy folders " +
		"(e.g. support tooling or templates) beside the TFM buckets; including those in the " +
		"candidate list would break TryResolveHostTfm's major/minor comparison.")]
	public void When_Host_Root_Contains_Non_Net_Folders_Then_They_Are_Filtered_Out()
	{
		var root = Path.Combine(Path.GetTempPath(), "uno-tests-host-" + Guid.NewGuid().ToString("N"));
		try
		{
			Directory.CreateDirectory(Path.Combine(root, "net9.0"));
			Directory.CreateDirectory(Path.Combine(root, "net10.0"));
			Directory.CreateDirectory(Path.Combine(root, "netstandard2.0"));
			Directory.CreateDirectory(Path.Combine(root, "some-feature-folder"));

			var tfms = UnoToolsLocator.EnumerateHostTfms(root);

			tfms.Should().BeEquivalentTo(new[] { "net9.0", "net10.0" });
		}
		finally
		{
			if (Directory.Exists(root))
			{
				Directory.Delete(root, recursive: true);
			}
		}
	}
}
