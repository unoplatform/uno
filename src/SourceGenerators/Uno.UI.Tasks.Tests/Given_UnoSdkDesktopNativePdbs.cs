using System.Text.Json;
using AwesomeAssertions;

namespace Uno.UI.Tasks.Tests;

/// <summary>
/// The SkiaSharp and HarfBuzzSharp native packages carry their PDBs as native assets, so DebugType doesn't reach them,
/// and a desktop app would ship 107 MB of symbols for libraries it doesn't build.
/// </summary>
[TestClass]
public class Given_UnoSdkDesktopNativePdbs
{
	[TestMethod]
	public void When_Assets_Are_Resolved_Then_Only_Skia_And_HarfBuzz_Native_Pdbs_Are_Removed()
	{
		const string itemName = "NativeCopyLocalItems";

		var directory = Directory.CreateTempSubdirectory("uno-sdk-native-pdbs-").FullName;
		try
		{
			var projectPath = Path.Join(directory, "Project.proj");
			File.WriteAllText(projectPath, $"""
				<Project>
					<PropertyGroup>
						<TargetFramework>net10.0-desktop</TargetFramework>
						<OutputType>Exe</OutputType>
						<UnoSingleProject>true</UnoSingleProject>
					</PropertyGroup>
					<ItemGroup>
						<{itemName} Include="libSkiaSharp.dll" NuGetPackageId="SkiaSharp.NativeAssets.Win32" />
						<{itemName} Include="libSkiaSharp.pdb" NuGetPackageId="SkiaSharp.NativeAssets.Win32" />
						<{itemName} Include="libHarfBuzzSharp.dll" NuGetPackageId="HarfBuzzSharp.NativeAssets.Win32" />
						<{itemName} Include="libHarfBuzzSharp.pdb" NuGetPackageId="HarfBuzzSharp.NativeAssets.Win32" />
						<{itemName} Include="contoso.pdb" NuGetPackageId="Contoso.Native" />
					</ItemGroup>
					<Import Project="{RepositoryPaths.Get("src", "Uno.Sdk", "targets", "Uno.Common.targets")}" />
				</Project>
				""");

			var result = DotnetMSBuild.Run(RepositoryPaths.Root, [projectPath, "-t:_UnoRemoveNativePdbs", $"-getItem:{itemName}"]);
			result.ExitCode.Should().Be(0, result.Output);

			using var json = JsonDocument.Parse(result.StandardOutput);
			var remaining = json.RootElement.GetProperty("Items").GetProperty(itemName)
				.EnumerateArray()
				.Select(item => item.GetProperty("Identity").GetString()!)
				.ToArray();

			remaining.Should().BeEquivalentTo(["libSkiaSharp.dll", "libHarfBuzzSharp.dll", "contoso.pdb"]);
		}
		finally
		{
			DotnetMSBuild.TryDeleteDirectory(directory);
		}
	}
}
