using System.Collections;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using AwesomeAssertions;
using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.DevServer.Cli.Tests;

/// <summary>
/// On Windows the host is started through its apphost, which finds .NET through DOTNET_ROOT or the machine-wide
/// install, never PATH. These tests cover how the CLI points it at the install that picked the host TFM.
/// </summary>
[TestClass]
public class Given_AppHostDotnetRoot
{
	private const string ShimPath = @"C:\Users\me\.dotnet\tools\uno-devserver.exe";

	private static readonly Func<Architecture, string?> NoMachineWideInstall = _ => null;

	private static readonly string ArchitectureVariable =
		"DOTNET_ROOT_" + RuntimeInformation.ProcessArchitecture.ToString().ToUpperInvariant();

	private string _tempDir = null!;

	[TestInitialize]
	public void Setup()
	{
		_tempDir = Path.Combine(Path.GetTempPath(), "uno-dotnet-root-tests-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_tempDir);
	}

	[TestCleanup]
	public void Cleanup()
	{
		try
		{
			Directory.Delete(_tempDir, recursive: true);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
		}
	}

	[TestMethod]
	[OSCondition(OperatingSystems.Windows)]
	[Description("Through `dotnet dnx` the CLI runs in a muxer, and `dotnet --version` resolves to that muxer whatever PATH says.")]
	public void TryLocate_WhenProcessIsDotnet_ShouldUseItsInstall()
	{
		var muxerRoot = CreateInstall("muxer");
		var pathRoot = CreateInstall("on-path");

		var install = DotnetRootLocator.TryLocate(Path.Combine(muxerRoot, "dotnet.exe"), pathRoot);

		install.Should().Be(new DotnetInstall(muxerRoot, RuntimeInformation.ProcessArchitecture));
	}

	[TestMethod]
	[OSCondition(OperatingSystems.Windows)]
	[Description("Through the global-tool shim, `dotnet --version` resolves to the first dotnet.exe on PATH.")]
	public void TryLocate_WhenProcessIsNotDotnet_ShouldUseFirstDotnetOnPath()
	{
		var noDotnet = CreateFolder("no-dotnet");
		var first = CreateInstall("first");
		var second = CreateInstall("second");

		var install = DotnetRootLocator.TryLocate(ShimPath, string.Join(Path.PathSeparator, noDotnet, first, second));

		install.Should().Be(new DotnetInstall(first, RuntimeInformation.ProcessArchitecture));
	}

	[TestMethod]
	[OSCondition(OperatingSystems.Windows)]
	[Description("Windows doesn't search a quoted or padded PATH entry, so neither does the locator.")]
	public void TryLocate_WhenPathEntriesAreQuotedOrPadded_ShouldSkipThemLikeWindows()
	{
		var quoted = CreateInstall("quoted");
		var padded = CreateInstall("padded");
		var plain = CreateInstall("plain");

		var install = DotnetRootLocator.TryLocate(ShimPath, $"\"{quoted}\"; {padded} ;;{plain}");

		install.Should().Be(new DotnetInstall(plain, RuntimeInformation.ProcessArchitecture));
	}

	[TestMethod]
	[OSCondition(OperatingSystems.Windows)]
	[Description("The first dotnet.exe on PATH is the one Windows runs: when it isn't a .NET install (e.g. a package manager shim), nothing is guessed.")]
	public void TryLocate_WhenFirstDotnetOnPathIsNotAnInstall_ShouldReturnNull()
	{
		var shim = CreateInstall("shim", withHostFxr: false);
		var install = CreateInstall("real");

		DotnetRootLocator.TryLocate(ShimPath, string.Join(Path.PathSeparator, shim, install))
			.Should().BeNull();
	}

	[TestMethod]
	[OSCondition(OperatingSystems.Windows)]
	[Description("A dotnet.exe that isn't an executable image gives no architecture, so no install.")]
	public void TryLocate_WhenDotnetIsNotAnExecutable_ShouldReturnNull()
	{
		var root = CreateInstall("broken", validExecutable: false);

		DotnetRootLocator.TryLocate(ShimPath, root).Should().BeNull();
	}

	[TestMethod]
	[OSCondition(OperatingSystems.Windows)]
	[DataRow(null)]
	[DataRow("")]
	[DataRow("C:\\does-not-exist-9f2c")]
	public void TryLocate_WhenNoDotnetIsOnPath_ShouldReturnNull(string? pathVariable)
		=> DotnetRootLocator.TryLocate(ShimPath, pathVariable).Should().BeNull();

	[TestMethod]
	[OSCondition(OperatingSystems.Windows)]
	[Description("The reported case: .NET only on PATH, no machine-wide install, no DOTNET_ROOT.")]
	public void SetAppHostDotnetRoot_WhenTheHostWouldMissTheInstall_ShouldPointItThere()
	{
		var root = CreateInstall("on-path");
		var environment = NewEnvironment();

		DevServerProcessHelper.SetAppHostDotnetRoot(environment, CreateAppHost(), ShimPath, root, NoMachineWideInstall);

		environment.Should().ContainSingle()
			.Which.Should().Be(new KeyValuePair<string, string?>(ArchitectureVariable, root));
	}

	[TestMethod]
	[OSCondition(OperatingSystems.Windows)]
	[Description("Through `dotnet dnx`, the CLI's own muxer picks the host's version. It is used when it is also the first dotnet on PATH.")]
	public void SetAppHostDotnetRoot_WhenTheCliRunsOnTheDotnetOnPath_ShouldPointTheHostAtIt()
	{
		var root = CreateInstall("on-path");
		var environment = NewEnvironment();

		DevServerProcessHelper.SetAppHostDotnetRoot(
			environment, CreateAppHost(), Path.Combine(root, "dotnet.exe"), root, NoMachineWideInstall);

		environment.Should().ContainSingle()
			.Which.Should().Be(new KeyValuePair<string, string?>(ArchitectureVariable, root));
	}

	[TestMethod]
	[OSCondition(OperatingSystems.Windows)]
	[Description("The CLI runs on another dotnet than the first one on PATH (e.g. an IDE set to another .NET): the host's child "
		+ "processes run the one on PATH and would inherit a variable pointing elsewhere, so nothing is set.")]
	public void SetAppHostDotnetRoot_WhenTheCliRunsOnAnotherDotnetThanTheOneOnPath_ShouldSetNothing()
	{
		var muxerRoot = CreateInstall("muxer");
		var pathRoot = CreateInstall("on-path");
		var environment = NewEnvironment();

		DevServerProcessHelper.SetAppHostDotnetRoot(
			environment, CreateAppHost(), Path.Combine(muxerRoot, "dotnet.exe"), pathRoot, NoMachineWideInstall);

		environment.Should().BeEmpty();
	}

	[TestMethod]
	[OSCondition(OperatingSystems.Windows)]
	[Description("A machine-wide install older than the one on PATH: the host must use the one that picked its version.")]
	public void SetAppHostDotnetRoot_WhenAnotherInstallIsMachineWide_ShouldPointTheHostAtTheOneOnPath()
	{
		var root = CreateInstall("on-path");
		var machineWide = CreateInstall("machine-wide");
		var environment = NewEnvironment();

		DevServerProcessHelper.SetAppHostDotnetRoot(environment, CreateAppHost(), ShimPath, root, _ => machineWide);

		environment.Should().ContainSingle()
			.Which.Should().Be(new KeyValuePair<string, string?>(ArchitectureVariable, root));
	}

	[TestMethod]
	[OSCondition(OperatingSystems.Windows)]
	[Description("The host finds the machine-wide install on its own: nothing is added, so its child processes see no change.")]
	public void SetAppHostDotnetRoot_WhenTheInstallIsTheMachineWideOne_ShouldSetNothing()
	{
		var root = CreateInstall("machine-wide");
		var environment = NewEnvironment();

		DevServerProcessHelper.SetAppHostDotnetRoot(
			environment, CreateAppHost(), ShimPath, root, _ => root.ToUpperInvariant() + Path.DirectorySeparatorChar);

		environment.Should().BeEmpty();
	}

	[TestMethod]
	[OSCondition(OperatingSystems.Windows)]
	[DataRow("DOTNET_ROOT")]
	[DataRow("DOTNET_ROOT_{ARCH}")]
	[Description("A variable the host reads, set by the user, wins: the environment is left as it is.")]
	public void SetAppHostDotnetRoot_WhenAVariableTheHostReadsIsSet_ShouldLeaveItAlone(string variableName)
	{
		variableName = variableName.Replace("_{ARCH}", ArchitectureVariable["DOTNET_ROOT".Length..]);
		var root = CreateInstall("on-path");
		var environment = NewEnvironment();
		environment[variableName] = @"D:\my\dotnet";

		DevServerProcessHelper.SetAppHostDotnetRoot(environment, CreateAppHost(), ShimPath, root, NoMachineWideInstall);

		environment.Should().ContainSingle()
			.Which.Should().Be(new KeyValuePair<string, string?>(variableName, @"D:\my\dotnet"));
	}

	[TestMethod]
	[OSCondition(OperatingSystems.Windows)]
	[Description("The host ignores another architecture's variable, so it doesn't stop the fix.")]
	public void SetAppHostDotnetRoot_WhenOnlyAnotherArchitectureVariableIsSet_ShouldStillPointTheHost()
	{
		var otherVariable = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "DOTNET_ROOT_X64" : "DOTNET_ROOT_ARM64";
		var root = CreateInstall("on-path");
		var environment = NewEnvironment();
		environment[otherVariable] = @"D:\other\dotnet";

		DevServerProcessHelper.SetAppHostDotnetRoot(environment, CreateAppHost(), ShimPath, root, NoMachineWideInstall);

		environment.Should().HaveCount(2)
			.And.Contain(ArchitectureVariable, root)
			.And.Contain(otherVariable, @"D:\other\dotnet");
	}

	[TestMethod]
	[OSCondition(OperatingSystems.Windows)]
	[Description("A host can't run on an install of another architecture (e.g. an x64 host and an Arm64 .NET): nothing is set.")]
	public void SetAppHostDotnetRoot_WhenTheInstallHasAnotherArchitecture_ShouldSetNothing()
	{
		var otherMachine = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? Machine.Amd64 : Machine.Arm64;
		var root = CreateInstall("other-architecture", machine: otherMachine);
		var environment = NewEnvironment();

		DevServerProcessHelper.SetAppHostDotnetRoot(environment, CreateAppHost(), ShimPath, root, NoMachineWideInstall);

		environment.Should().BeEmpty();
	}

	[TestMethod]
	[OSCondition(OperatingSystems.Windows)]
	public void SetAppHostDotnetRoot_WhenNoInstallIsFound_ShouldSetNothing()
	{
		var environment = NewEnvironment();

		DevServerProcessHelper.SetAppHostDotnetRoot(environment, CreateAppHost(), ShimPath, CreateFolder("empty"), NoMachineWideInstall);

		environment.Should().BeEmpty();
	}

	[TestMethod]
	[OSCondition(OperatingSystems.Windows)]
	[DoNotParallelize] // Changes this process's PATH and DOTNET_ROOT variables while it runs.
	[Description("Starting the host .exe points it at the install found from this process and PATH; `dotnet <dll>` needs nothing.")]
	public void CreateDotnetProcessStartInfo_ShouldPointOnlyTheAppHostAtTheInstall()
	{
		var root = CreateInstall("on-path");
		var appHost = CreateAppHost();
		var saved = Environment.GetEnvironmentVariables().Cast<DictionaryEntry>()
			.Select(entry => (Name: (string)entry.Key, Value: (string?)entry.Value))
			.Where(variable => variable.Name.StartsWith("DOTNET_ROOT", StringComparison.OrdinalIgnoreCase)
				|| variable.Name.Equals("PATH", StringComparison.OrdinalIgnoreCase))
			.ToList();
		try
		{
			saved.ForEach(variable => Environment.SetEnvironmentVariable(variable.Name, null));
			Environment.SetEnvironmentVariable("PATH", root);

			var expected = NewEnvironment();
			DevServerProcessHelper.SetAppHostDotnetRoot(
				expected, appHost, Environment.ProcessPath, root, DotnetRootLocator.TryGetMachineWideRoot);
			expected.Should().ContainKey(ArchitectureVariable, "this test host isn't a dotnet muxer, so the install on PATH is used");

			var exe = DevServerProcessHelper.CreateDotnetProcessStartInfo(appHost, [], _tempDir, redirectOutput: false);
			var dll = DevServerProcessHelper.CreateDotnetProcessStartInfo(Path.ChangeExtension(appHost, ".dll"), [], _tempDir, redirectOutput: false);

			exe.Environment.Should().Contain(ArchitectureVariable, expected[ArchitectureVariable]);
			DevServerProcessHelper.GetAddedDotnetRoot(exe).Should().Be((ArchitectureVariable, expected[ArchitectureVariable]!));
			dll.Environment.Should().NotContainKey(ArchitectureVariable);
			DevServerProcessHelper.GetAddedDotnetRoot(dll).Should().BeNull();
		}
		finally
		{
			Environment.SetEnvironmentVariable("PATH", null);
			saved.ForEach(variable => Environment.SetEnvironmentVariable(variable.Name, variable.Value));
		}
	}

	private static Dictionary<string, string?> NewEnvironment()
		=> new(StringComparer.OrdinalIgnoreCase);

	private string CreateFolder(string name)
		=> Directory.CreateDirectory(Path.Combine(_tempDir, name)).FullName;

	// Any executable image of this process's architecture stands in for the host .exe and for dotnet.exe.
	private string CreateAppHost()
	{
		var appHost = Path.Combine(CreateFolder("host"), "Uno.UI.RemoteControl.Host.exe");
		File.Copy(Environment.ProcessPath!, appHost, overwrite: true);
		return appHost;
	}

	private string CreateInstall(string name, bool withHostFxr = true, bool validExecutable = true, Machine? machine = null)
	{
		var root = CreateFolder(name);
		if (withHostFxr)
		{
			Directory.CreateDirectory(Path.Combine(root, "host", "fxr"));
		}

		var dotnet = Path.Combine(root, "dotnet.exe");
		if (!validExecutable)
		{
			File.WriteAllText(dotnet, "not an executable");
			return root;
		}

		var image = File.ReadAllBytes(Environment.ProcessPath!);
		if (machine is { } target)
		{
			// The COFF header's Machine field sits right after the "PE\0\0" signature.
			var peHeaderOffset = BitConverter.ToInt32(image, 0x3C);
			BitConverter.TryWriteBytes(image.AsSpan(peHeaderOffset + 4, 2), (ushort)target);
		}

		File.WriteAllBytes(dotnet, image);
		return root;
	}
}
