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
using Windows.UI.Xaml;
using Uno.UI.RemoteControl;
using Windows.UI.Xaml.Tests.Common;
using Windows.UI.Composition;
using System.Threading;
using System.Xml;
using Windows.Storage.AccessCache;
using System.Linq;

namespace Uno.UI.RuntimeTests.Tests.HotReload;

[TestClass]
#if !__SKIA__
[Ignore("Hot reload tests are only available on Skia targets")]
#endif
internal class Given_HotReloadWorkspace
{
	private static Process? _process;
	private static int _remoteControlPort;
	private static Process? _testAppProcess;

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
	public async Task TestCleanupWrapper()
	{
		_testAppProcess?.Kill();
	}

	[TestMethod]
	[Timeout(5 * 60 * 1000)]
	public async Task When_HotReloadScenario()
	{
		var resultFile = await RunTestApp(CancellationToken.None);

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

	private class NUnitXmlParser
	{
		public record TestResult(string result, string Name, string? errorMessage);

		internal static TestResult[] GetTests(string resultFile)
		{
			var doc = new XmlDocument();
			doc.Load(resultFile);

			List<TestResult> results = new();

			var testCases = doc.SelectNodes("//test-case");

			if (testCases is not null)
			{
				foreach (var testCase in testCases)
				{
					if (testCase is XmlNode node
						&& node.Attributes is not null)
					{
						var result = node.Attributes["result"]?.Value ?? "N/A";

						var name = node.Attributes["name"]?.Value;
						var fullName = node.Attributes["fullname"]?.Value;
						var error = node.SelectSingleNode("failure/message")?.InnerText;

						results.Add(new(result, name ?? "Unknown", error!));
					}
				}
			}

			return results.ToArray();
		}
	}

	public static async Task InitializeServer()
	{
		if (_remoteControlPort == 0)
		{
			_remoteControlPort = GetTcpPort();

			StartServer(_remoteControlPort);
		}
	}

	public static async Task<string> RunTestApp(CancellationToken ct)
	{
		var testOutput = Path.GetTempFileName();

		var hrAppPath = GetHotReloadAppPath();

		typeof(Given_HotReloadWorkspace).Log().Debug($"Starting test app (path{hrAppPath})");

		var p = await RunProcess(
			ct,
			"dotnet",
			new() {
				"run",
				"--configuration",

				// Use the debug configuration so that remote control
				// gets properly enabled.
				"Debug",

				"--no-build",
				"--uitest",
				testOutput
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

	private static async Task<Process> RunProcess(
		CancellationToken ct,
		string executable,
		List<string> parameters,
		string workingDirectory,
		string logPrefix,
		bool waitForExit,
		Dictionary<string, string>? environmentVariables = null)
	{
		var process = StartProcess(executable, parameters, workingDirectory, logPrefix, environmentVariables);

		typeof(Given_HotReloadWorkspace).Log().Debug(logPrefix + $" waiting for process exit");

		if (waitForExit)
		{
			await process.WaitForExitAsync();
		}

		return process;
	}

	private static Process StartProcess(
		string executable,
		List<string> parameters,
		string workingDirectory,
		string logPrefix,
		Dictionary<string, string>? environmentVariables = null)
	{
		var hrAppPath = GetHotReloadAppPath();

		var pi = new ProcessStartInfo(executable)
		{
			UseShellExecute = false,
			CreateNoWindow = true,
			WindowStyle = ProcessWindowStyle.Hidden,
			WorkingDirectory = workingDirectory,
		};

		foreach (var parm in parameters)
		{
			pi.ArgumentList.Add(parm);
		}

		if (environmentVariables is not null)
		{
			foreach (var env in environmentVariables)
			{
				pi.EnvironmentVariables[env.Key] = env.Value;
			}
		}

		// redirect the output
		pi.RedirectStandardOutput = true;
		pi.RedirectStandardError = true;

		var process = new System.Diagnostics.Process();

		// hookup the event handlers to capture the data that is received
		process.OutputDataReceived += (sender, args) => typeof(Given_HotReloadWorkspace).Log().Debug(logPrefix + ": " + args.Data ?? "<Empty>");
		process.ErrorDataReceived += (sender, args) => typeof(Given_HotReloadWorkspace).Log().Error(logPrefix + ": " + args.Data ?? "<Empty>");

		process.StartInfo = pi;

		typeof(Given_HotReloadWorkspace).Log().Debug($"Started process (wd:{pi.WorkingDirectory}): {pi.FileName} {string.Join(" ", pi.ArgumentList)})");

		process.Start();

		// start our event pumps
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();

		return process;
	}

	public static async Task BuildTestApp()
	{
		var process = StartProcess(
			"dotnet",
			new() {
				"build",
				$"-p:UnoRemoteControlPort={_remoteControlPort}",
				$"-p:UnoRemoteControlHost=127.0.0.1",
				"--configuration",

				// Use the debug configuration so that remote control
				// gets properly enabled.
				"Debug"
			},
			GetHotReloadAppPath(),
			"HRAppBuild"
		);

		await process.WaitForExitAsync();

		await process.WaitForExitAsync();

		if (process.ExitCode != 0)
		{
			throw new InvalidOperationException("Failed to build app");
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
		if (_process?.HasExited ?? true)
		{
			var version = GetDotnetMajorVersion();
			var runtimeVersionPath = version <= 5 ? "netcoreapp3.1" : $"net{version}.0";

			var basePath = Path.GetDirectoryName(Application.Current.GetType().Assembly.Location)!;

			// Use the debug configuration so that remote control
			// gets properly enabled.
			var toolsPath = Path.Combine(GetRCHostAppPath(), "Debug");

			var sb = new StringBuilder();

			var hostBinPath = Path.Combine(toolsPath, runtimeVersionPath, "Uno.UI.RemoteControl.Host.dll");

			if (!File.Exists(hostBinPath))
			{
				typeof(Given_HotReloadWorkspace).Log().Error($"RCHost {hostBinPath} does not exist");
				throw new InvalidOperationException($"Unable to find {hostBinPath}");
			}

			string arguments = $"\"{hostBinPath}\" --httpPort {port} --ppid {Process.GetCurrentProcess().Id} --metadata-updates true";
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
			_process.OutputDataReceived += (sender, args) => typeof(Given_HotReloadWorkspace).Log().Debug("RCHost: " + args.Data ?? "");
			_process.ErrorDataReceived += (sender, args) => typeof(Given_HotReloadWorkspace).Log().Error("RCHost: " + args.Data ?? "");

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
