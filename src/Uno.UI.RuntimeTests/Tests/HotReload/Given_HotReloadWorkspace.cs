#if HAS_UNO
#nullable enable

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Uno.Foundation.Logging;
using Microsoft.UI.Xaml;
using Uno.UI.RemoteControl;
using Microsoft.UI.Xaml.Tests.Common;
using Microsoft.UI.Composition;
using System.Threading;
using System.Xml;
using Windows.Storage.AccessCache;
using System.Linq;
using System.Collections.Immutable;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.HotReload;

// Hot reload tests are only available on Skia desktop targets
[TestClass]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
public partial class Given_HotReloadWorkspace
{
	private static Process? _process;
	private static int _remoteControlPort;
	private static Process? _testAppProcess;

	/// <remarks>
	/// This test is running C# hot reload tests in a separate app, located 
	/// in the HRApp folder. These tests are run as "runtime tests" in the HRApp, and the
	/// results are returned as an NUnit XML result file.
	/// 
	/// The results are expanded as a single unit test result for the test below which runs
	/// in the original SamplesApp.
	/// 
	/// This secondary app is launched alongside a DevServer and can request hot reload
	/// for its own files.
	/// 
	/// On the CI oor local console, the debug logs from the DevServer and HRApp are provided using the 
	/// <see cref="Uno.UI.RuntimeTests.Tests.HotReload.Given_HotReloadWorkspace" /> type, 
	/// and can be set in debug in the SamplesApp's startup.
	/// 
	/// Note that attaching a debugger to the HRApp will disable the hot reload subsystem 
	/// in the Core CLR, making all HR runtime tests fail.
	/// 
	/// </remarks>
	// Hot reload tests are only available on Skia desktop targets
	[Timeout(10 * 60 * 1000)]
	[Filters]
	[TestMethod]
	public async Task When_HotReloadScenario(string filters)
	{
		// Remove this class and this method from the filters
		filters = string.Join(";", (filters?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>()).ToImmutableArray().RemoveAll(x => x == nameof(Given_HotReloadWorkspace) || x == nameof(When_HotReloadScenario)));
		var resultFile = await RunTestApp(filters, CancellationToken.None);

		// Parse the nunit XML results file and extract all failed tests
		var tests = NUnitXmlParser.GetTests(resultFile);

		StringBuilder sb = new();

		foreach (var test in tests)
		{
			if (test.result != "Passed" && test.result != "Skipped")
			{
				sb.AppendLine($"Test {test.Name} failed with {test.errorMessage}");
			}
		}

		var resultMessage = sb.ToString();

		if (resultMessage.Length != 0)
		{
			Assert.Fail($"Tests failed:\n{resultMessage}");
		}
	}

	[TestInitialize]
	public async Task Initialize()
	{
#if !DEBUG
		if (Environment.GetEnvironmentVariable("UnoEnableHRuntimeTests") != "true")
		{
			Assert.Inconclusive($"HotReload workspace test are not enabled for this platform (UnoEnableHRuntimeTests env var is not defined)");
		}
#endif

		await InitializeServer();
		await BuildTestApp();
	}

	[TestCleanup]
	public void TestCleanupWrapper()
	{
		_testAppProcess?.Kill();
		_testAppProcess?.WaitForExit();
	}

	public static async Task InitializeServer()
	{
		if (_remoteControlPort == 0)
		{
			_remoteControlPort = GetTcpPort();

			StartServer(_remoteControlPort);
		}
	}

	public static async Task<string> RunTestApp(string filters, CancellationToken ct)
	{
		var testOutput = Path.GetTempFileName();

		var hrAppPath = GetHotReloadAppPath();

		typeof(Given_HotReloadWorkspace).Log().Debug($"Starting test app (path{hrAppPath})");
		var p = await ProcessHelpers.RunProcess(
			ct,
			"dotnet",
			new() {
				"run",
				"--configuration",

				// Use the debug configuration so that remote control
				// gets properly enabled.
				"Debug",

				"--no-build",
				"--",
				"--uitest",
				testOutput,
				"--filters",
				filters
			},
			hrAppPath,
			"HRApp",
			true,

			// Required when running in CI, as VS sets it automatically in debug
			new() { ["DOTNET_MODIFIABLE_ASSEMBLIES"] = "debug" }
		);

		_testAppProcess = p;

		if (p.ExitCode != 0)
		{
			typeof(Given_HotReloadWorkspace).Log().Error($"Failed to start (path{hrAppPath})");

			throw new InvalidOperationException("Failed to run HR app");
		}

		return testOutput;
	}

	public static async Task BuildTestApp()
	{
		var builder = new StringBuilder();
		var process = ProcessHelpers.StartProcess(
			"dotnet",
			new() {
				"build",
				$"-p:UnoRemoteControlPort={_remoteControlPort}",
				$"-p:UnoRemoteControlHost=127.0.0.1",
				$"-p:NoWarn=CS8619", // Workaround https://github.com/unoplatform/uno.ui.runtimetests.engine/issues/174#issuecomment-2104088957
				"--configuration",

				// Use the debug configuration so that remote control
				// gets properly enabled.
				"Debug"
			},
			GetHotReloadAppPath(),
			"HRAppBuild",
			output: builder
		);

		await process.WaitForExitAsync();

		if (process.ExitCode != 0)
		{
			throw new InvalidOperationException($"Failed to build app{Environment.NewLine}{builder}");
		}
	}

	private static string GetHotReloadAppPath()
	{
		var basePath = Path.GetDirectoryName(Application.Current.GetType().Assembly.Location)!;

		var searchPaths = new[] {
			Path.Combine(basePath, "..", "..", "..", "..", "..", "Uno.UI.RuntimeTests", "Tests", "HotReload", "Frame", "HRApp"),
			Path.Combine(basePath, "..", "..", "src", "Uno.UI.RuntimeTests", "Tests", "HotReload", "Frame", "HRApp"),
		};

		var hrAppPath = searchPaths
			.Where(Directory.Exists)
			.FirstOrDefault();

		if (hrAppPath is null)
		{
			throw new InvalidOperationException("Unable to find HRApp folder in " + string.Join(", ", searchPaths));
		}

		return hrAppPath;
	}

	private static string GetRCHostAppPath()
	{
		var basePath = Path.GetDirectoryName(Application.Current.GetType().Assembly.Location)!;

		var searchPaths = new[] {
			Path.Combine(basePath, "..", "..", "..", "..", "..", "Uno.UI.RemoteControl.Host", "bin"),
			Path.Combine(basePath, "..", "..", "src", "Uno.UI.RemoteControl.Host", "bin"),
		};

		var hrAppPath = searchPaths
			.Where(Directory.Exists)
			.FirstOrDefault();

		if (hrAppPath is null)
		{
			throw new InvalidOperationException("Unable to find RCHost folder in " + string.Join(", ", searchPaths));
		}

		return hrAppPath;
	}

	private static void StartServer(int port)
	{
		if (_process is null or { HasExited: true })
		{
			var version = GetDotnetMajorVersion();
			var runtimeVersionPath = $"net{version}.0";

			// Use the debug configuration so that remote control
			// gets properly enabled.
			var toolsPath = Path.Combine(GetRCHostAppPath(), "Debug");
			var hostBinPath = Path.Combine(toolsPath, runtimeVersionPath, "Uno.UI.RemoteControl.Host.dll");

			// GetDotnetMajorVersion() will return the .NET SDK version.
			// We may be building with TargetFramework being one version earlier.
			if (!File.Exists(hostBinPath))
			{
				version--;
				runtimeVersionPath = $"net{version}.0";
				hostBinPath = Path.Combine(toolsPath, runtimeVersionPath, "Uno.UI.RemoteControl.Host.dll");
			}

			if (!File.Exists(hostBinPath))
			{
				typeof(Given_HotReloadWorkspace).Log().Error($"RCHost {hostBinPath} does not exist");
				throw new InvalidOperationException($"Unable to find {hostBinPath}");
			}

			var arguments = $"\"{hostBinPath}\" --httpPort {port} --ppid {Process.GetCurrentProcess().Id} --metadata-updates true";

			Console.WriteLine($"Starting RCHost: {arguments}");

			var pi = new ProcessStartInfo("dotnet", arguments)
			{
				UseShellExecute = false,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				WorkingDirectory = Path.Combine(toolsPath),
			};

			// redirect the output
			pi.RedirectStandardOutput = true;
			pi.RedirectStandardError = true;

			_process = new System.Diagnostics.Process();

			// hookup the eventhandlers to capture the data that is received
			_process.OutputDataReceived += (sender, args) => typeof(Given_HotReloadWorkspace).Log().Debug($"[{DateTime.Now}] RCHost: " + args.Data ?? "");
			_process.ErrorDataReceived += (sender, args) => typeof(Given_HotReloadWorkspace).Log().Error($"[{DateTime.Now}] RCHost: " + args.Data ?? "");

			_process.StartInfo = pi;
			_process.Start();

			// start our event pumps
			_process.BeginOutputReadLine();
			_process.BeginErrorReadLine();
		}
	}

	private static int GetDotnetMajorVersion()
	{
		if (Application.Current.GetType().Assembly.Location is { Length: 0 })
		{
			throw new InvalidOperationException("The Application location must have a physical path location");
		}

		var result = ProcessHelpers.RunProcess("dotnet", "--version", Path.GetDirectoryName(Application.Current.GetType().Assembly.Location));

		if (result.exitCode != 0)
		{
			throw new InvalidOperationException($"Unable to detect current dotnet version (\"dotnet --version\" exited with code {result.exitCode})");
		}

		if (result.output.Contains("."))
		{
			if (int.TryParse(result.output.Substring(0, result.output.IndexOf('.')), out int majorVersion))
			{
				return majorVersion;
			}
		}

		throw new InvalidOperationException($"Unable to detect current dotnet version (\"dotnet --version\" returned \"{result.output}\")");
	}

	private static int GetTcpPort()
	{
		var l = new TcpListener(IPAddress.Loopback, 0);
		l.Start();
		var port = ((IPEndPoint)l.LocalEndpoint).Port;
		l.Stop();
		return port;
	}
}
#endif
