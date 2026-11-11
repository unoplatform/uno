using System.Text.Json;
using AwesomeAssertions;

namespace Uno.UI.Tasks.Tests;

/// <summary>
/// A package's <c>.uprimarker</c> is copied next to its assembly, but must not pose as it: the .NET 11 WASM SDK keeps
/// one candidate per relative path, so a marker with the assembly's DestinationSubPath replaced the DLL (#26289).
/// </summary>
[TestClass]
public class Given_PackageAssetMarkers
{
	[TestMethod]
	public void When_Marker_Is_Found_Then_It_Does_Not_Inherit_The_Assembly_Metadata()
	{
		var directory = Path.Join(Path.GetTempPath(), "uno-asset-markers", Guid.NewGuid().ToString("N"));
		var lib = Path.Join(directory, "pkg", "lib", "net11.0");
		Directory.CreateDirectory(lib);
		try
		{
			File.WriteAllText(Path.Join(lib, "Foo.dll"), "");
			File.WriteAllText(Path.Join(lib, "Foo.uprimarker"), "");

			var projectPath = Path.Join(directory, "Head.proj");
			File.WriteAllText(projectPath, $"""
				<Project>
					<PropertyGroup>
						<IsUnoHead>true</IsUnoHead>
					</PropertyGroup>

					<Import Project="{RepositoryPaths.Get("src", "SourceGenerators", "Uno.UI.Tasks", "Content", "uno.ui.tasks.assets.targets")}" />

					<ItemGroup>
						<RuntimeCopyLocalItems Include="pkg/lib/net11.0/Foo.dll"
											   AssetType="runtime"
											   CopyLocal="true"
											   DestinationSubPath="Foo.dll"
											   PathInPackage="lib/net11.0/Foo.dll"
											   NuGetPackageId="Foo" />
					</ItemGroup>
				</Project>
				""");

			var result = DotnetMSBuild.Run(directory, [projectPath, "-t:_UnoFindPackageAssetMarkers", "-getItem:ReferenceCopyLocalPaths"]);

			result.ExitCode.Should().Be(0, result.Output);
			var marker = JsonDocument.Parse(result.StandardOutput)
				.RootElement.GetProperty("Items").GetProperty("ReferenceCopyLocalPaths")
				.EnumerateArray()
				.Single();

			marker.GetProperty("Filename").GetString().Should().Be("Foo");
			marker.GetProperty("Extension").GetString().Should().Be(".uprimarker");
			marker.GetProperty("DestinationSubPath").GetString().Should().Be("Foo.uprimarker");
			marker.TryGetProperty("AssetType", out _).Should().BeFalse();
			marker.TryGetProperty("PathInPackage", out _).Should().BeFalse();
			marker.GetProperty("NuGetPackageId").GetString().Should().Be("Foo");
		}
		finally
		{
			DotnetMSBuild.TryDeleteDirectory(directory);
		}
	}
}
