using System.IO;
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
		UnoToolsLocator.TryResolveHostTfm(System.Array.Empty<string>(), "net10.0").Should().BeNull();
	}

	[TestMethod]
	[Description("EnumerateHostTfms must silently return an empty array when the host directory " +
		"itself is missing — this happens for malformed packages and during cache eviction. Any " +
		"other behaviour would force the caller to wrap the enumeration in its own Directory.Exists " +
		"guard, duplicating the check.")]
	public void When_Host_Root_Directory_Does_Not_Exist_Then_Returns_Empty()
	{
		var missing = Path.Combine(Path.GetTempPath(), "uno-tests-does-not-exist-" + System.Guid.NewGuid().ToString("N"));

		UnoToolsLocator.EnumerateHostTfms(missing).Should().BeEmpty();
	}

	[TestMethod]
	[Description("EnumerateHostTfms must skip non-net TFM directories that happen to exist under " +
		"tools/rc/host/. Older Uno package versions occasionally carried legacy folders " +
		"(e.g. support tooling or templates) beside the TFM buckets; including those in the " +
		"candidate list would break TryResolveHostTfm's major/minor comparison.")]
	public void When_Host_Root_Contains_Non_Net_Folders_Then_They_Are_Filtered_Out()
	{
		var root = Path.Combine(Path.GetTempPath(), "uno-tests-host-" + System.Guid.NewGuid().ToString("N"));
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
