using System.Xml.Linq;
using AwesomeAssertions;

namespace Uno.UI.Tasks.Tests;

/// <summary>
/// Every Uno.WinUI.Runtime.Skia.* host declares itself with UnoHasRuntimeHost. A host that forgets leaves its heads
/// on the reference facades with no more than a UNOB0025 warning, so the declaration is pinned here instead.
/// </summary>
[TestClass]
public class Given_RuntimeHostPackages
{
	/// <summary>Runtime projects that are libraries shared by the hosts rather than hosts themselves.</summary>
	private static readonly string[] NotHosts = ["Uno.UI.Runtime.Skia", "Uno.UI.Runtime.Skia.Win32.Support"];

	/// <summary>The hosts whose heads Uno.Resizetizer must still see as Skia apps through UnoRuntimeIdentifier.</summary>
	private static readonly string[] DesktopTypeHosts = ["Headless", "Linux.FrameBuffer", "MacOS", "Win32", "X11"];

	public static IEnumerable<object[]> Hosts =>
		Directory.EnumerateDirectories(RepositoryPaths.Get("src"), "Uno.UI.Runtime.Skia.*")
			.Select(Path.GetFileName)
			.Where(name => !NotHosts.Contains(name))
			.Select(name => new object[] { name!["Uno.UI.Runtime.Skia.".Length..] });

	[TestMethod]
	public void When_Enumerating_Hosts_Then_Every_Platform_Is_Found()
	{
		Hosts.Select(row => (string)row[0]).Should().Contain(["Android", "AppleUIKit", "WebAssembly.Browser", .. DesktopTypeHosts]);
	}

	[TestMethod]
	[DynamicData(nameof(Hosts))]
	public void When_Referenced_Then_The_Host_Declares_UnoHasRuntimeHost(string host)
	{
		Property(host, "UnoHasRuntimeHost").Should().Be("true");
	}

	[TestMethod]
	[DynamicData(nameof(Hosts))]
	public void When_Referenced_Then_Only_Desktop_Type_Hosts_Set_The_Resizetizer_Shim(string host)
	{
		var shim = Property(host, "UnoRuntimeIdentifier");

		if (DesktopTypeHosts.Contains(host))
		{
			shim.Should().Be("Skia");
			Property(host, "_UnoRuntimeIdentifierResizetizerShim").Should().Be("true");
		}
		else
		{
			// A mobile or browser head must keep Resizetizer's platform branch rather than its Skia one.
			shim.Should().BeNull();
		}
	}

	private static string? Property(string host, string name)
	{
		var props = new[] { "build", "buildTransitive" }
			.Select(folder => RepositoryPaths.Get("src", $"Uno.UI.Runtime.Skia.{host}", folder, $"Uno.WinUI.Runtime.Skia.{host}.props"))
			.Where(File.Exists)
			.ToList();

		props.Should().ContainSingle($"Uno.UI.Runtime.Skia.{host} ships the props a head imports");

		return XDocument.Load(props[0]).Descendants().FirstOrDefault(e => e.Name.LocalName == name)?.Value;
	}
}
