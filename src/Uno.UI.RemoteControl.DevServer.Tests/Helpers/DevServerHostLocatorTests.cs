using System.IO;
using Uno.UI.RemoteControl.VS.Helpers;

namespace Uno.UI.RemoteControl.DevServer.Tests.Helpers;

/// <summary>
/// The Visual Studio extension (<c>EntryPoint.EnsureServerAsync</c>) picks the Dev Server host
/// from the package's <c>tools/rc/host/net{X}.{Y}</c> folders with <see cref="DevServerHostLocator"/>.
/// Uno 6.x packages ship <c>net9.0</c> and <c>net10.0</c> hosts only, so on a machine whose default
/// SDK is .NET 11 (Visual Studio 18.12) the exact <c>net11.0</c> host doesn't exist.
/// </summary>
[TestClass]
public class DevServerHostLocatorTests
{
	private string _hostRoot = null!;

	[TestInitialize]
	public void Setup()
		=> _hostRoot = Path.Combine(Path.GetTempPath(), "uno-host-locator-" + Guid.NewGuid().ToString("N"), "tools", "rc", "host");

	[TestCleanup]
	public void Cleanup()
	{
		var root = Path.GetFullPath(Path.Combine(_hostRoot, "..", "..", ".."));
		if (Directory.Exists(root))
		{
			Directory.Delete(root, recursive: true);
		}
	}

	private void CreateHost(string tfm, bool withAssembly = true)
	{
		var dir = Path.Combine(_hostRoot, tfm);
		Directory.CreateDirectory(dir);
		if (withAssembly)
		{
			File.WriteAllText(Path.Combine(dir, DevServerHostLocator.HostAssemblyName), "");
		}
	}

	[TestMethod]
	[Description("Uno 6.x package (net9.0 + net10.0 hosts) with the .NET 11 SDK as default: use the net10.0 " +
		"host with a major roll-forward, instead of the missing net11.0 host that made the start fail.")]
	public void TryResolve_WhenSdkIsOneMajorNewerThanEveryHost_UsesHighestHostWithRollForward()
	{
		CreateHost("net9.0");
		CreateHost("net10.0");

		var host = DevServerHostLocator.TryResolve(_hostRoot, dotnetMajorVersion: 11);

		host.Should().NotBeNull();
		var (hostPath, requiresMajorRollForward) = host!.Value;
		hostPath.Should().Be(Path.Combine(_hostRoot, "net10.0", DevServerHostLocator.HostAssemblyName));
		requiresMajorRollForward.Should().BeTrue();
	}

	[TestMethod]
	[DataRow(10, "net10.0")]
	[DataRow(9, "net9.0")]
	public void TryResolve_WhenExactHostExists_UsesItWithoutRollForward(int sdkMajor, string expectedTfm)
	{
		CreateHost("net9.0");
		CreateHost("net10.0");

		var host = DevServerHostLocator.TryResolve(_hostRoot, sdkMajor);

		host.Should().NotBeNull();
		var (hostPath, requiresMajorRollForward) = host!.Value;
		hostPath.Should().Be(Path.Combine(_hostRoot, expectedTfm, DevServerHostLocator.HostAssemblyName));
		requiresMajorRollForward.Should().BeFalse();
	}

	[TestMethod]
	[Description("Uno 7.0 package (net10.0 + net11.0 hosts) with the .NET 11 SDK: unchanged behavior.")]
	public void TryResolve_WhenNewerPackageHasTheExactHost_UsesIt()
	{
		CreateHost("net10.0");
		CreateHost("net11.0");

		var host = DevServerHostLocator.TryResolve(_hostRoot, dotnetMajorVersion: 11);

		host.Should().NotBeNull();
		var (hostPath, requiresMajorRollForward) = host!.Value;
		hostPath.Should().Be(Path.Combine(_hostRoot, "net11.0", DevServerHostLocator.HostAssemblyName));
		requiresMajorRollForward.Should().BeFalse();
	}

	[TestMethod]
	[Description("A roll-forward covers one major only, so a host two majors below the SDK is not used.")]
	public void TryResolve_WhenOnlyHostsTwoMajorsBelowExist_ReturnsNull()
	{
		CreateHost("net9.0");

		DevServerHostLocator.TryResolve(_hostRoot, dotnetMajorVersion: 11).Should().BeNull();
	}

	[TestMethod]
	[Description(".NET can't run a host built for a newer runtime than the SDK's.")]
	public void TryResolve_WhenOnlyNewerHostsExist_ReturnsNull()
	{
		CreateHost("net11.0");

		DevServerHostLocator.TryResolve(_hostRoot, dotnetMajorVersion: 10).Should().BeNull();
	}

	[TestMethod]
	public void TryResolve_WhenHostFolderHasNoAssembly_ReturnsNull()
	{
		CreateHost("net10.0", withAssembly: false);

		DevServerHostLocator.TryResolve(_hostRoot, dotnetMajorVersion: 10).Should().BeNull();
	}

	[TestMethod]
	public void TryResolve_WhenHostRootIsMissing_ReturnsNull()
	{
		DevServerHostLocator.TryResolve(_hostRoot, dotnetMajorVersion: 11).Should().BeNull();
		DevServerHostLocator.EnumerateHostTfms(_hostRoot).Should().BeEmpty();
	}

	[TestMethod]
	[Description("Sorted by version, not by string (net10.0 after net9.0); non-host folders ignored.")]
	public void EnumerateHostTfms_ReturnsHostFoldersSortedByVersion()
	{
		CreateHost("net10.0");
		CreateHost("net9.0");
		CreateHost("net11.0");
		Directory.CreateDirectory(Path.Combine(_hostRoot, "netstandard2.0"));
		Directory.CreateDirectory(Path.Combine(_hostRoot, "net10.0-windows10.0.19041"));
		Directory.CreateDirectory(Path.Combine(_hostRoot, "assets"));

		DevServerHostLocator.EnumerateHostTfms(_hostRoot).Should().Equal("net9.0", "net10.0", "net11.0");
	}

	[TestMethod]
	[DataRow(new[] { "net9.0", "net10.0" }, "net10.0", "net10.0")]
	[DataRow(new[] { "net9.0", "net10.0" }, "net11.0", "net10.0")]
	[DataRow(new[] { "net8.0", "net9.0", "net10.0" }, "net11.0", "net10.0")]
	[DataRow(new[] { "net10.0", "net10.5" }, "net11.0", "net10.5")]
	[DataRow(new[] { "net10.0", "net10.5" }, "net10.5", "net10.5")]
	[DataRow(new[] { "net9.0" }, "net11.0", null)]
	[DataRow(new[] { "net11.0" }, "net10.0", null)]
	[DataRow(new string[0], "net11.0", null)]
	public void TryResolveHostTfm_FollowsTheCliRules(string[] available, string requested, string? expected)
		=> DevServerHostLocator.TryResolveHostTfm(available, requested).Should().Be(expected);

	[TestMethod]
	[DataRow("net10.0", 10, 0)]
	[DataRow("net11.0", 11, 0)]
	[DataRow("NET9.0", 9, 0)]
	[DataRow("net5.0", 5, 0)]
	public void TryParseNetTfm_WhenShortNetTfm_ReturnsVersion(string input, int expectedMajor, int expectedMinor)
	{
		DevServerHostLocator.TryParseNetTfm(input, out var major, out var minor).Should().BeTrue();
		major.Should().Be(expectedMajor);
		minor.Should().Be(expectedMinor);
	}

	[TestMethod]
	[DataRow(null)]
	[DataRow("")]
	[DataRow("netstandard2.0")]
	[DataRow("netcoreapp3.1")]
	[DataRow("net472")]
	[DataRow("net4.8")]
	[DataRow("net10.0-windows10.0.19041")]
	[DataRow("net10")]
	[DataRow("assets")]
	public void TryParseNetTfm_WhenNotAShortNetTfm_ReturnsFalse(string? input)
		=> DevServerHostLocator.TryParseNetTfm(input, out _, out _).Should().BeFalse();

	[TestMethod]
	[Description("Guards the fix: EntryPoint must not build the host path from the exact SDK major again.")]
	public void EntryPoint_ResolvesTheHostThroughTheLocator()
	{
		var testDir = Path.GetDirectoryName(typeof(DevServerHostLocatorTests).Assembly.Location)!;
		var source = Path.Combine(Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", "..")), "Uno.UI.RemoteControl.VS", "EntryPoint.cs");
		if (!File.Exists(source))
		{
			Assert.Inconclusive($"EntryPoint.cs not found at {source}. This test requires the source tree.");
		}

		var text = File.ReadAllText(source);
		text.Should().Contain("DevServerHostLocator.TryResolve(");
		text.Should().NotContain("$\"net{version}.0\"");
	}
}
