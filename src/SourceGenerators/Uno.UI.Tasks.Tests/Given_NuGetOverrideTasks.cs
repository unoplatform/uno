using System.Diagnostics;
using System.Text.RegularExpressions;
using AwesomeAssertions;

namespace Uno.UI.Tasks.Tests;

/// <summary>
/// The NuGet override (UnoNugetOverrideVersion) installs locally built Uno.UI.Tasks.v0.dll into a published uno.winui package, whose targets
/// load the tasks by the commit SHA that UpdateTasksSHA put in place of "v0": the override has to switch those targets over to "v0".
/// </summary>
[TestClass]
public class Given_NuGetOverrideTasks
{
	private const string Sha = "0123456789abcdef0123456789abcdef01234567";
	private const string OtherSha = "fedcba9876543210fedcba9876543210fedcba98";

	// Split so that UpdateTasksSHA, which replaces "v0" in these sources on CI, leaves the suffix of locally built tasks alone.
	private const string LocalSuffix = "v" + "0";

	private static readonly string[] BoundFiles =
	[
		"Uno.UI.Tasks.targets",
		"uno.ui.tasks.assets.targets",
		"uno.winui.runtime-replace.targets",
		Path.Combine("uap10.0.19041", "uno.winui.runtime-replace.targets"),
		"uno.winui.winappsdk.targets",
	];

	private const string UnrelatedFile = "uno.winui.unrelated.targets";

	[TestMethod]
	public void When_Package_Targets_Load_Sha_Tasks_Then_They_Are_Switched_To_V0()
	{
		using var package = ScratchPackage.Create(Sha);

		var (exitCode, output) = package.RunOverride();

		exitCode.Should().Be(0, output);
		foreach (var file in BoundFiles)
		{
			package.Read(file).TrimEnd().Should().Be(package.Original(file).Replace(Sha, LocalSuffix).TrimEnd(), file);
		}

		package.Read(UnrelatedFile).Should().Be(package.Original(UnrelatedFile), "files without the SHA are not rewritten");
		Directory.GetFiles(package.PackageFolder, "*", SearchOption.AllDirectories).Should().NotContain(path => path.EndsWith(".override-tmp", StringComparison.Ordinal));
	}

	[TestMethod]
	public void When_The_Shipped_Targets_Are_Overridden_Then_No_Sha_Is_Left()
	{
		using var package = ScratchPackage.FromShippedTargets(out var packageSha);

		var (exitCode, output) = package.RunOverride();

		exitCode.Should().Be(0, output);
		foreach (var file in package.Files)
		{
			package.Read(file).TrimEnd().Should().Be(package.Original(file).Replace(packageSha, LocalSuffix).TrimEnd(), file);
		}
	}

	[TestMethod]
	public void When_Override_Runs_Again_Then_The_Targets_Are_Left_Alone()
	{
		using var package = ScratchPackage.Create(Sha);
		package.RunOverride().ExitCode.Should().Be(0);
		var afterFirstRun = BoundFiles.ToDictionary(file => file, package.Read);
		var writeTimes = BoundFiles.ToDictionary(file => file, package.LastWriteTime);

		var (exitCode, output) = package.RunOverride();

		exitCode.Should().Be(0, output);
		output.Should().NotContain("Pointing the package targets at the");
		foreach (var file in BoundFiles)
		{
			package.Read(file).Should().Be(afterFirstRun[file], file);
			package.LastWriteTime(file).Should().Be(writeTimes[file], file);
		}
	}

	[TestMethod]
	public void When_A_Previous_Override_Was_Interrupted_Then_The_Remaining_Targets_Are_Switched()
	{
		using var package = ScratchPackage.Create(Sha);
		foreach (var file in BoundFiles.Where(file => !file.Contains("runtime-replace", StringComparison.Ordinal)))
		{
			package.Overwrite(file, package.Original(file).Replace(Sha, LocalSuffix));
		}

		var (exitCode, output) = package.RunOverride();

		exitCode.Should().Be(0, output);
		foreach (var file in BoundFiles)
		{
			package.Read(file).TrimEnd().Should().Be(package.Original(file).Replace(Sha, LocalSuffix).TrimEnd(), file);
		}
	}

	[TestMethod]
	[DataRow(false, DisplayName = "In different files")]
	[DataRow(true, DisplayName = "In the same file")]
	public void When_Package_Targets_Use_Different_Shas_Then_All_Are_Switched(bool sameFile)
	{
		using var package = ScratchPackage.Create(Sha);
		var mixedFile = BoundFiles[0];
		var original = package.Original(mixedFile);
		package.Overwrite(mixedFile, sameFile
			? original.Replace($"<RuntimeAssetsSelectorTask_{Sha} ", $"<RuntimeAssetsSelectorTask_{OtherSha} ")
			: original.Replace(Sha, OtherSha));

		var (exitCode, output) = package.RunOverride();

		exitCode.Should().Be(0, output);
		foreach (var file in BoundFiles)
		{
			package.Read(file).TrimEnd().Should().Be(package.Original(file).Replace(Sha, LocalSuffix).TrimEnd(), file);
		}
	}

	[TestMethod]
	public void When_Package_Targets_Already_Use_V0_Then_Nothing_Is_Rewritten()
	{
		using var package = ScratchPackage.Create(LocalSuffix);

		var (exitCode, output) = package.RunOverride();

		exitCode.Should().Be(0, output);
		foreach (var file in BoundFiles)
		{
			package.Read(file).Should().Be(package.Original(file), file);
		}
	}

	[TestMethod]
	[DataRow(Sha, DisplayName = "Package on its own tasks")]
	[DataRow(LocalSuffix, DisplayName = "Package already switched")]
	public void When_Other_Values_Look_Like_A_Sha_Then_They_Are_Left_Alone(string suffix)
	{
		using var package = ScratchPackage.Create(suffix);
		var lookalike = $"<UnoAssetId>asset_{OtherSha}</UnoAssetId>";
		package.Overwrite(UnrelatedFile, package.Original(UnrelatedFile).Replace("<UnoUnrelated>true</UnoUnrelated>", lookalike));
		var boundFile = BoundFiles[0];
		package.Overwrite(boundFile, package.Original(boundFile).Replace("</Project>", $"<PropertyGroup>{lookalike}</PropertyGroup></Project>"));

		var (exitCode, output) = package.RunOverride();

		exitCode.Should().Be(0, output);
		package.Read(UnrelatedFile).Should().Contain(lookalike);
		package.Read(boundFile).Should().Contain(lookalike).And.NotContain(Sha);
	}

	[TestMethod]
	public void When_Local_Tasks_Are_Installed_Then_The_Package_Switches_To_Them_Before_Its_Own_Are_Deleted()
	{
		using var package = ScratchPackage.Create(Sha);

		var (exitCode, output) = package.RunInstall(LocalSuffix);

		exitCode.Should().Be(0, output);
		package.TasksFiles().Should().BeEquivalentTo(ScratchPackage.TasksFilesFor(LocalSuffix));
		foreach (var file in BoundFiles)
		{
			package.Read(file).TrimEnd().Should().Be(package.Original(file).Replace(Sha, LocalSuffix).TrimEnd(), file);
		}

		var copied = output.IndexOf($"Uno.UI.Tasks.{LocalSuffix}.dll\"", StringComparison.Ordinal);
		var switched = output.IndexOf("Pointing the package targets at the", StringComparison.Ordinal);
		var deleted = output.IndexOf($"Uno.UI.Tasks.{Sha}.dll\"", StringComparison.Ordinal);
		copied.Should().BeGreaterThan(-1, output);
		switched.Should().BeGreaterThan(copied, "the targets are switched once the local tasks are installed");
		deleted.Should().BeGreaterThan(switched, "the package's own tasks are deleted last");
	}

	[TestMethod]
	public void When_Local_Tasks_Are_Installed_Again_Then_The_Package_Is_Left_Alone()
	{
		using var package = ScratchPackage.Create(Sha);
		package.RunInstall(LocalSuffix).ExitCode.Should().Be(0);
		var afterFirstRun = BoundFiles.ToDictionary(file => file, package.Read);

		var (exitCode, output) = package.RunInstall(LocalSuffix);

		exitCode.Should().Be(0, output);
		output.Should().NotContain("Pointing the package targets at the");
		package.TasksFiles().Should().BeEquivalentTo(ScratchPackage.TasksFilesFor(LocalSuffix));
		foreach (var file in BoundFiles)
		{
			package.Read(file).Should().Be(afterFirstRun[file], file);
		}
	}

	[TestMethod]
	public void When_Local_Tasks_Are_Built_With_The_Package_Sha_Then_The_Package_Is_Left_On_It()
	{
		using var package = ScratchPackage.Create(Sha);

		var (exitCode, output) = package.RunInstall(Sha);

		exitCode.Should().Be(0, output);
		package.TasksFiles().Should().BeEquivalentTo(ScratchPackage.TasksFilesFor(Sha));
		foreach (var file in BoundFiles)
		{
			package.Read(file).Should().Be(package.Original(file), file);
		}
	}

	[TestMethod]
	public void When_Local_Tasks_Built_With_A_Sha_Follow_A_V0_Override_Then_The_Package_Switches_To_Them()
	{
		using var package = ScratchPackage.Create(Sha);
		package.RunInstall(LocalSuffix).ExitCode.Should().Be(0);

		var (exitCode, output) = package.RunInstall(OtherSha);

		exitCode.Should().Be(0, output);
		package.TasksFiles().Should().BeEquivalentTo(ScratchPackage.TasksFilesFor(OtherSha));
		foreach (var file in BoundFiles)
		{
			package.Read(file).TrimEnd().Should().Be(package.Original(file).Replace(Sha, OtherSha).TrimEnd(), file);
		}
	}

	[TestMethod]
	public void When_The_Package_Has_No_Targets_Then_The_Local_Tasks_Are_Still_Installed()
	{
		using var package = ScratchPackage.Create(Sha);
		package.DeleteTargets();

		var (exitCode, output) = package.RunInstall(LocalSuffix);

		exitCode.Should().Be(0, output);
		package.TasksFiles().Should().BeEquivalentTo(ScratchPackage.TasksFilesFor(LocalSuffix));
	}

	[TestMethod]
	public void When_Local_Tasks_Fail_To_Copy_Then_The_Package_Keeps_Its_Own()
	{
		using var package = ScratchPackage.Create(Sha);
		package.BlockTasksFile($"Uno.UI.Tasks.{LocalSuffix}.dll");

		var (exitCode, output) = package.RunInstall(LocalSuffix);

		exitCode.Should().Be(0, output);
		package.TasksFiles().Should().Contain($"Uno.UI.Tasks.{Sha}.dll");
		foreach (var file in BoundFiles)
		{
			package.Read(file).Should().Be(package.Original(file), file);
		}
	}

	/// <summary>
	/// A package folder shaped like a published uno.winui package: targets under buildTransitive, and the tasks they load in
	/// buildTransitive/Uno.UI.Tasks.
	/// </summary>
	private sealed class ScratchPackage : IDisposable
	{
		private readonly string _directory = Path.Combine(Path.GetTempPath(), "uno-override-tasks", Guid.NewGuid().ToString("N"));
		private readonly Dictionary<string, string> _originals = new();

		private ScratchPackage(IEnumerable<(string File, string Contents)> files, string tasksSuffix)
		{
			PackageFolder = Path.Combine(_directory, "uno.winui", "1.2.3-override");

			foreach (var (file, contents) in files)
			{
				var path = PathOf(file);
				Directory.CreateDirectory(Path.GetDirectoryName(path)!);
				File.WriteAllText(path, contents);
				_originals[file] = contents;
			}

			Directory.CreateDirectory(TasksFolder);
			foreach (var file in TasksFilesFor(tasksSuffix))
			{
				File.WriteAllText(Path.Combine(TasksFolder, file), "package");
			}
		}

		/// <summary>
		/// A package whose targets bind the tasks by <paramref name="suffix"/>.
		/// </summary>
		public static ScratchPackage Create(string suffix)
			=> new(BoundFiles.Select(file => (file, Contents(file, suffix))).Append((UnrelatedFile, UnrelatedContents)), suffix);

		/// <summary>
		/// A package holding the targets this repository ships, with the SHA that UpdateTasksSHA gives them (on CI, the sources already carry it).
		/// </summary>
		public static ScratchPackage FromShippedTargets(out string packageSha)
		{
			(string Source, string File)[] shipped =
			[
				(RepositoryPaths.Get("src", "SourceGenerators", "Uno.UI.Tasks", "Content", "Uno.UI.Tasks.targets"), "Uno.UI.Tasks.targets"),
				(RepositoryPaths.Get("src", "SourceGenerators", "Uno.UI.Tasks", "Content", "uno.ui.tasks.assets.targets"), "uno.ui.tasks.assets.targets"),
				(RepositoryPaths.Get("build", "nuget", "uno.winui.runtime-replace.targets"), "uno.winui.runtime-replace.targets"),
				(RepositoryPaths.Get("build", "nuget", "uno.winui.runtime-replace.targets"), Path.Combine("uap10.0.19041", "uno.winui.runtime-replace.targets")),
				(RepositoryPaths.Get("build", "nuget", "uno.winui.winappsdk.targets"), "uno.winui.winappsdk.targets"),
			];
			var files = shipped.Select(s => (s.File, File.ReadAllText(s.Source).Replace(LocalSuffix, Sha))).ToArray();

			packageSha = Regex.Match(files[0].Item2, @"Uno\.UI\.Tasks\.([0-9a-f]{40})\.dll").Groups[1].Value;
			packageSha.Should().NotBeEmpty("the shipped Uno.UI.Tasks.targets loads the tasks assembly by its SHA");

			return new(files, packageSha);
		}

		public static string[] TasksFilesFor(string suffix) => [$"Uno.UI.Tasks.{suffix}.dll", $"Uno.UI.Tasks.{suffix}.pdb"];

		public string PackageFolder { get; }

		public IEnumerable<string> Files => _originals.Keys;

		private string TasksFolder => Path.Combine(PackageFolder, "buildTransitive", "Uno.UI.Tasks");

		public string Original(string file) => _originals[file];

		public string Read(string file) => File.ReadAllText(PathOf(file));

		public void Overwrite(string file, string contents) => File.WriteAllText(PathOf(file), contents);

		public DateTime LastWriteTime(string file) => File.GetLastWriteTimeUtc(PathOf(file));

		public string[] TasksFiles() => Directory.GetFileSystemEntries(TasksFolder).Select(path => Path.GetFileName(path)).ToArray();

		public void DeleteTargets()
		{
			foreach (var file in _originals.Keys)
			{
				File.Delete(PathOf(file));
			}
		}

		// A directory in the way of a file makes copying it fail.
		public void BlockTasksFile(string file) => Directory.CreateDirectory(Path.Combine(TasksFolder, file));

		public (int ExitCode, string Output) RunOverride() => Run("_UnoPointOverriddenPackageAtLocalTasks");

		/// <summary>
		/// Runs the whole override of the tasks, installing a build output of tasks named with <paramref name="suffix"/>.
		/// </summary>
		public (int ExitCode, string Output) RunInstall(string suffix)
		{
			var outputFolder = Path.Combine(_directory, "bin") + Path.DirectorySeparatorChar;
			if (Directory.Exists(outputFolder))
			{
				Directory.Delete(outputFolder, recursive: true);
			}
			Directory.CreateDirectory(outputFolder);
			foreach (var file in TasksFilesFor(suffix))
			{
				File.WriteAllText(Path.Combine(outputFolder, file), "local");
			}

			return Run("_UnoOverrideTasksPackage", $"-p:TargetDir={outputFolder}", $"-p:TargetName=Uno.UI.Tasks.{suffix}", $"-p:TargetFileName=Uno.UI.Tasks.{suffix}.dll");
		}

		public void Dispose()
		{
			try
			{
				Directory.Delete(_directory, recursive: true);
			}
			catch (Exception e) when (e is IOException or UnauthorizedAccessException)
			{
				// Leaving a temp folder behind must never fail a test run.
			}
		}

		private (int ExitCode, string Output) Run(string target, params string[] properties)
		{
			var projectPath = Path.Combine(_directory, "Override.proj");
			var logPath = Path.Combine(_directory, $"{target}-{Guid.NewGuid():N}.log");
			File.WriteAllText(projectPath, $"""
				<Project>
					<Import Project="{RepositoryPaths.Get("src", "SourceGenerators", "Uno.UI.Tasks", "Uno.UI.Tasks.Override.targets")}" />
				</Project>
				""");

			string[] arguments =
			[
				"msbuild", projectPath, "-nologo", "-nodeReuse:false", "-tl:off", $"-flp:LogFile={logPath};Verbosity=normal", $"-t:{target}",
				"-p:UnoNugetOverrideVersion=1.2.3-override", $"-p:_UnoOverriddenTasksPackageFolder={PackageFolder}",
				.. properties,
			];
			var startInfo = new ProcessStartInfo("dotnet")
			{
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				WorkingDirectory = _directory,
			};
			foreach (var argument in arguments)
			{
				startInfo.ArgumentList.Add(argument);
			}
			startInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";

			using var process = Process.Start(startInfo)!;
			var stdout = process.StandardOutput.ReadToEndAsync();
			var stderr = process.StandardError.ReadToEndAsync();
			process.WaitForExit();

			var log = File.Exists(logPath) ? File.ReadAllText(logPath) : stdout.Result + stderr.Result;
			return (process.ExitCode, log);
		}

		private string PathOf(string file) => Path.Combine(PackageFolder, "buildTransitive", file);

		private const string UnrelatedContents = """
			<Project>
				<PropertyGroup>
					<UnoUnrelated>true</UnoUnrelated>
				</PropertyGroup>
			</Project>
			""";

		// Like the published package, the runtime-replace targets only use the tasks by name, without loading the assembly themselves.
		private static string Contents(string file, string suffix) => Path.GetFileName(file).StartsWith("uno.winui.runtime-replace", StringComparison.Ordinal)
			? $"""
				<Project>
					<Target Name="Select_{Path.GetFileNameWithoutExtension(file).Replace('.', '_')}">
						<RuntimeAssetsSelectorTask_{suffix} Condition="'$(A)' != '' and '$(B)' == 'x;y'" />
					</Target>
				</Project>
				"""
			: $"""
				<Project>
					<UsingTask AssemblyFile="$(UnoUIMSBuildTasksPath)\Uno.UI.Tasks.{suffix}.dll" TaskName="Uno.UI.Tasks.RuntimeAssetsSelector.RuntimeAssetsSelectorTask_{suffix}" />
					<Target Name="Select_{Path.GetFileNameWithoutExtension(file).Replace('.', '_')}">
						<RuntimeAssetsSelectorTask_{suffix} Condition="'$(A)' != '' and '$(B)' == 'x;y'" />
					</Target>
				</Project>
				""";
	}
}
