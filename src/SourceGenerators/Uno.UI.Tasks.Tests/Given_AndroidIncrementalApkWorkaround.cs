using System.Diagnostics;
using AwesomeAssertions;

namespace Uno.UI.Tasks.Tests;

/// <summary>
/// net10.0-android's incremental BuildArchive drops the Java resources of AAR/JAR dependencies (dotnet/android#11543),
/// so Uno.Sdk must hand it no previous archive whenever the package is rebuilt (#24520). The .NET Android packaging
/// targets are stubbed with the same names, conditions and Inputs/Outputs properties.
/// </summary>
[TestClass]
public class Given_AndroidIncrementalApkWorkaround
{
	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void When_Package_Is_Stale_Then_The_Previous_Archive_Is_Deleted(bool embedAssemblies)
	{
		var archiveExisted = Package(embedAssemblies, archiveIsUpToDate: false);

		archiveExisted.Should().BeFalse();
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void When_Package_Is_Up_To_Date_Then_The_Archive_Is_Kept(bool embedAssemblies)
	{
		var archiveExisted = Package(embedAssemblies, archiveIsUpToDate: true);

		archiveExisted.Should().BeTrue();
	}

	[TestMethod]
	public void When_Targeting_Net11_Then_The_Archive_Is_Kept()
	{
		var archiveExisted = Package(embedAssemblies: false, archiveIsUpToDate: false, "-p:TargetFrameworkVersion=v11.0");

		archiveExisted.Should().BeTrue();
	}

	[TestMethod]
	public void When_Opted_Out_Then_The_Archive_Is_Kept()
	{
		var archiveExisted = Package(embedAssemblies: false, archiveIsUpToDate: false, "-p:UnoDisableIncrementalApkWorkaround=true");

		archiveExisted.Should().BeTrue();
	}

	private static bool Package(bool embedAssemblies, bool archiveIsUpToDate, params string[] properties)
	{
		var directory = Path.Combine(Path.GetTempPath(), "uno-android-apk-workaround", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		try
		{
			var projectPath = Path.Combine(directory, "Head.proj");
			File.WriteAllText(projectPath, $"""
				<Project>
					<PropertyGroup>
						<TargetFrameworkVersion>v10.0</TargetFrameworkVersion>
						<EmbedAssembliesIntoApk>{embedAssemblies}</EmbedAssembliesIntoApk>
						<ApkFileIntermediate>$(MSBuildThisFileDirectory)app.apk</ApkFileIntermediate>
						<_BaseZipIntermediate>$(MSBuildThisFileDirectory)base.zip</_BaseZipIntermediate>
						<_BuildApkEmbedInputs>$(MSBuildThisFileDirectory)packaged_resources</_BuildApkEmbedInputs>
						<_BuildApkFastDevStaticInputs>$(MSBuildThisFileDirectory)packaged_resources</_BuildApkFastDevStaticInputs>
					</PropertyGroup>

					<Import Project="{RepositoryPaths.Get("src", "Uno.Sdk", "targets", "Uno.Common.Android.targets")}" />

					<Target Name="_PrepareBuildApk">
						<PropertyGroup>
							<_BuildApkEmbedOutputs>$(ApkFileIntermediate)</_BuildApkEmbedOutputs>
						</PropertyGroup>
					</Target>
					<Target Name="_BuildApkEmbed" DependsOnTargets="_PrepareBuildApk" Condition="'$(EmbedAssembliesIntoApk)' == 'True'">
						<Message Importance="high" Text="ArchiveExisted=$([System.IO.File]::Exists('$(ApkFileIntermediate)'))" />
					</Target>
					<Target Name="_BuildApkFastDev" DependsOnTargets="_PrepareBuildApk" Condition="'$(EmbedAssembliesIntoApk)' != 'True'">
						<Message Importance="high" Text="ArchiveExisted=$([System.IO.File]::Exists('$(ApkFileIntermediate)'))" />
					</Target>
					<Target Name="Package" DependsOnTargets="_BuildApkEmbed;_BuildApkFastDev" />
				</Project>
				""");

			var resources = Path.Combine(directory, "packaged_resources");
			var archive = Path.Combine(directory, "app.apk");
			File.WriteAllText(resources, "");
			File.WriteAllText(archive, "");
			var now = DateTime.UtcNow;
			File.SetLastWriteTimeUtc(resources, archiveIsUpToDate ? now.AddMinutes(-1) : now);
			File.SetLastWriteTimeUtc(archive, archiveIsUpToDate ? now : now.AddMinutes(-1));

			var (exitCode, output) = MSBuild(directory, [projectPath, "-t:Package", .. properties]);

			exitCode.Should().Be(0, output);
			output.Should().Contain("ArchiveExisted=");
			return output.Contains("ArchiveExisted=True");
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	private static (int ExitCode, string Output) MSBuild(string workingDirectory, string[] arguments)
	{
		var startInfo = new ProcessStartInfo("dotnet")
		{
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			WorkingDirectory = workingDirectory,
		};
		string[] allArguments = ["msbuild", "-nologo", "-nodeReuse:false", .. arguments];
		foreach (var argument in allArguments)
		{
			startInfo.ArgumentList.Add(argument);
		}
		startInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";

		using var process = Process.Start(startInfo)!;
		var stdout = process.StandardOutput.ReadToEndAsync();
		var stderr = process.StandardError.ReadToEndAsync();
		process.WaitForExit();

		return (process.ExitCode, stdout.Result + stderr.Result);
	}
}
