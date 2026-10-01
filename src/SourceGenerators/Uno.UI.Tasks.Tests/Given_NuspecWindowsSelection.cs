using System.Xml.Linq;
using AwesomeAssertions;
using NuGet.Frameworks;

namespace Uno.UI.Tasks.Tests;

/// <summary>
/// NuGet picks the nearest dependency group and lib folder on its own, and a newer generic TFM (net11.0) beats an
/// older platform one (net10.0-windows). A package with a WinAppSDK flavour must therefore list every Windows TFM,
/// or a newer Windows head silently gets the Skia assembly and Uno.WinUI.
/// </summary>
[TestClass]
public class Given_NuspecWindowsSelection
{
	public static IEnumerable<object[]> WindowsNuspecs =>
		Directory.EnumerateFiles(RepositoryPaths.Get("build", "nuget"), "*.nuspec")
			.Where(path => LibFolders(path).Concat(DependencyGroups(path)).Any(IsWindows))
			.Select(path => new object[] { Path.GetFileName(path) });

	public static IEnumerable<object[]> WindowsConsumers =>
		ReadNetVersions().Select(net => new object[] { $"{net}-windows10.0.19041.0" });

	[TestMethod]
	public void When_Enumerating_Nuspecs_Then_The_WinAppSDK_Flavoured_Ones_Are_Found()
	{
		WindowsNuspecs.Select(row => (string)row[0])
			.Should().Contain(["Uno.WinUI.nuspec", "Uno.WinUI.Graphics2DSK.nuspec", "Uno.WinUI.MSAL.nuspec"]);
	}

	[TestMethod]
	[DynamicData(nameof(WindowsNuspecs))]
	public void When_A_Windows_Head_Restores_Then_It_Gets_The_Windows_Lib_Folder(string nuspec)
	{
		var folders = LibFolders(RepositoryPaths.Get("build", "nuget", nuspec)).ToArray();

		foreach (var consumer in WindowsConsumers.Select(row => (string)row[0]))
		{
			var nearest = Nearest(consumer, folders);
			IsWindows(nearest).Should().BeTrue($"{nuspec} resolves lib/{nearest?.GetShortFolderName()} for {consumer}");
		}
	}

	[TestMethod]
	[DynamicData(nameof(WindowsNuspecs))]
	public void When_A_Windows_Head_Restores_Then_It_Gets_The_Windows_Dependencies(string nuspec)
	{
		var groups = DependencyGroups(RepositoryPaths.Get("build", "nuget", nuspec)).ToArray();

		foreach (var consumer in WindowsConsumers.Select(row => (string)row[0]))
		{
			var nearest = Nearest(consumer, groups);
			IsWindows(nearest).Should().BeTrue($"{nuspec} resolves the {nearest?.GetShortFolderName()} dependency group for {consumer}");
		}
	}

	private static NuGetFramework? Nearest(string consumer, IEnumerable<NuGetFramework> candidates)
		=> new FrameworkReducer().GetNearest(NuGetFramework.Parse(consumer), candidates);

	private static bool IsWindows(NuGetFramework? framework)
		=> string.Equals(framework?.Platform, "windows", StringComparison.OrdinalIgnoreCase);

	private static IEnumerable<NuGetFramework> LibFolders(string nuspec)
		=> XDocument.Load(nuspec)
			.Descendants().Where(e => e.Name.LocalName == "file")
			.Select(e => ((string?)e.Attribute("target") ?? "").Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries))
			// $winuitargetpath$ and friends are substituted at pack time.
			.Where(parts => parts is ["lib", var tfm, ..] && !tfm.Contains('$'))
			.Select(parts => NuGetFramework.Parse(parts[1]))
			.Distinct();

	private static IEnumerable<NuGetFramework> DependencyGroups(string nuspec)
		=> XDocument.Load(nuspec)
			.Descendants().Where(e => e.Name.LocalName == "group")
			.Select(e => (string?)e.Attribute("targetFramework"))
			.Where(tfm => !string.IsNullOrEmpty(tfm))
			.Select(tfm => NuGetFramework.Parse(tfm!))
			.Distinct();

	private static IEnumerable<string> ReadNetVersions()
	{
		var properties = XDocument.Load(RepositoryPaths.Get("Directory.Build.props"))
			.Descendants().Where(e => e.Name.LocalName is "NetPrevious" or "NetCurrent")
			.Select(e => e.Value.Trim())
			.ToArray();

		properties.Should().HaveCount(2, "Directory.Build.props defines NetPrevious and NetCurrent");
		return properties;
	}
}
