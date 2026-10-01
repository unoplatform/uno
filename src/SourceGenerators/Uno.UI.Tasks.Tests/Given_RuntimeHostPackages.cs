using System.Xml.Linq;
using AwesomeAssertions;

namespace Uno.UI.Tasks.Tests;

/// <summary>
/// Every Uno.WinUI.Runtime.* host declares itself with UnoHasRuntimeHost. A host that forgets leaves its heads
/// on the reference facades with no more than a UNOB0025 warning, so the declaration is pinned here instead.
/// </summary>
[TestClass]
public class Given_RuntimeHostPackages
{
	/// <summary>Runtime projects that are libraries shared by the hosts rather than hosts themselves.</summary>
	private static readonly string[] NotHosts = ["Uno.UI.Runtime.Skia", "Uno.UI.Runtime.Skia.Win32.Support"];

	private static readonly string[] ExpectedHosts = ["Android", "AppleUIKit", "WebAssembly.Browser", "Headless", "Linux.FrameBuffer", "MacOS", "Win32", "X11"];

	public static IEnumerable<object[]> Hosts =>
		Directory.EnumerateDirectories(RepositoryPaths.Get("src"), "Uno.UI.Runtime.Skia.*")
			.Select(Path.GetFileName)
			.Where(name => !NotHosts.Contains(name))
			.Select(name => new object[] { name!["Uno.UI.Runtime.Skia.".Length..] });

	[TestMethod]
	public void When_Enumerating_Hosts_Then_Every_Platform_Is_Found()
	{
		Hosts.Select(row => (string)row[0]).Should().Contain(ExpectedHosts);
	}

	[TestMethod]
	[DynamicData(nameof(Hosts))]
	public void When_Referenced_Then_The_Host_Declares_UnoHasRuntimeHost(string host)
	{
		Property(host, "UnoHasRuntimeHost").Should().Be("true");
	}

	[TestMethod]
	[DynamicData(nameof(Hosts))]
	public void When_Referenced_Then_The_Host_Sets_No_Retired_Runtime_Identifier(string host)
	{
		// Separately versioned packages (Uno.Resizetizer) still branch on these, so a host setting one reclassifies its heads.
		foreach (var property in new[] { "UnoRuntimeIdentifier", "UnoUIRuntimeIdentifier", "UnoWinRTRuntimeIdentifier" })
		{
			Property(host, property).Should().BeNull($"Uno.UI.Runtime.Skia.{host} must not set {property}");
		}
	}

	private static string? Property(string host, string name)
	{
		var props = new[] { "build", "buildTransitive" }
			.Select(folder => RepositoryPaths.Get("src", $"Uno.UI.Runtime.Skia.{host}", folder))
			.Where(Directory.Exists)
			.SelectMany(folder => Directory.EnumerateFiles(folder, "Uno.WinUI.Runtime.*.props"))
			.ToList();

		props.Should().ContainSingle($"Uno.UI.Runtime.Skia.{host} ships the props a head imports");

		return XDocument.Load(props[0]).Descendants().FirstOrDefault(e => e.Name.LocalName == name)?.Value;
	}
}
