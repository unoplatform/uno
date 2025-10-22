using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Uno.UI.RemoteControl.DevServer.Tests.Telemetry;
using Uno.UI.RemoteControl.DevServer.Tests.Helpers;
using Uno.UI.RemoteControl.VS.Helpers;

namespace Uno.UI.RemoteControl.DevServer.Tests.AppLaunch;

[TestClass]
public class RealAppLaunchIntegrationTests : TelemetryTestBase
{
	private const string? _targetFramework = "net10.0";

	[ClassInitialize]
	public static void ClassInitialize(TestContext context) => GlobalClassInitialize<RealAppLaunchIntegrationTests>(context);

	[TestMethod]
	public async Task WhenRealAppBuiltAndRunWithDevServer_RealConnectionEstablished()
	{
		// PRE-ARRANGE: Create a real Uno solution file (will contain desktop project)
		var solution = SolutionHelper!;
		await solution.CreateSolutionFileAsync(platforms: "desktop", targetFramework: _targetFramework);

		var filePath = Path.Combine(Path.GetTempPath(), GetTestTelemetryFileName("applaunch_app_success"));
		await using var helper = CreateTelemetryHelperWithExactPath(filePath, solutionPath: solution.SolutionFile, enableIdeChannel: false);

		Process? appProcess = null;
		try
		{
			// ARRANGE
			var started = await helper.StartAsync(CT);
			helper.EnsureStarted();

			// Build the App (Skia desktop) project with devserver configuration
			var projectPath = await BuildAppProjectAsync(solution, helper.Port);

			// ACT - STEP 1: Read MVID and Target Platform from built assembly and register app launch (IDE -> devserver)
			await RegisterAppLaunchAsync(projectPath, helper.Port);

			// ACT - STEP 2: Start the real Skia desktop application that will eventually connect to devserver
			appProcess = await StartSkiaDesktopAppAsync(projectPath, helper.Port);

			// ACT - STEP 3: Wait for the real connection to be established
			await WaitForAppToConnectoToDevServerAsync(helper, TimeSpan.FromSeconds(30));

			// ASSERT
			await Task.Delay(3000, CT);
			await helper.AttemptGracefulShutdownAsync(CT);

			var events = ParseTelemetryFileIfExists(filePath);
			started.Should().BeTrue("Dev server should start successfully");

			WriteEventsList(events);

			events.Should().NotBeEmpty();
			AssertHasEvent(events, "uno/dev-server/app-launch/launched");
			AssertHasEvent(events, "uno/dev-server/app-launch/connected");

			helper.ConsoleOutput.Length.Should().BeGreaterThan(0, "Dev server should produce some output");
#if DEBUG
			await solution.ShowDotnetVersionAsync();
#endif
		}
#if !DEBUG
		catch
		{
			await solution.ShowDotnetVersionAsync();
			throw;
		}
#endif
		finally
		{
			// Clean up app process if it's still running
			if (appProcess is { HasExited: false })
			{
				try
				{
					appProcess.Kill();
					appProcess.WaitForExit(5000);
				}
				catch (Exception ex)
				{
					TestContext!.WriteLine($"Error stopping Skia process: {ex.Message}");
				}
				appProcess.Dispose();
			}

			await helper.StopAsync(CT);
			DeleteIfExists(filePath);

			TestContext!.WriteLine("Dev Server Output:");
			TestContext.WriteLine(helper.ConsoleOutput);
		}
	}

	private async Task RegisterAppLaunchAsync(string projectPath, int httpPort)
	{
		var projectDir = Path.GetDirectoryName(projectPath)!;
		var assemblyName = Path.GetFileNameWithoutExtension(projectPath);
		var tfm = $"{_targetFramework}-desktop";
		var assemblyPath = Path.Combine(projectDir, "bin", "Debug", tfm, assemblyName + ".dll");

		TestContext!.WriteLine($"Reading assembly info from: {assemblyPath}");
		var (mvid, platformName) = AssemblyInfoReader.Read(assemblyPath);
		var platform = platformName ?? "Desktop";

		using (var http = new HttpClient())
		{
			var url = $"http://localhost:{httpPort}/applaunch/{mvid}?platform={Uri.EscapeDataString(platform)}&isDebug=false";
			TestContext!.WriteLine($"Registering app launch: {url}");
			var response = await http.GetAsync(url, CT);
			response.EnsureSuccessStatusCode();
		}
	}

	/// <summary>
	/// Builds the Skia desktop project from the generated solution with devserver configuration.
	/// </summary>
	private async Task<string> BuildAppProjectAsync(SolutionHelper solution, int devServerPort)
	{
		// Find the desktop project path in the generated solution
		var solutionDir = Path.GetDirectoryName(solution.SolutionFile)!;

		// Look for the project to compile (there's only one in the solution)
		var appProject = Directory.GetFiles(solutionDir, "*.csproj", SearchOption.AllDirectories).SingleOrDefault();

		if (appProject == null)
		{
			throw new InvalidOperationException("Could not find a project in the generated solution");
		}

		TestContext!.WriteLine($"Building desktop project: {appProject}");

		// Build the project with devserver configuration so the generators create the right ServerEndpointAttribute
		// Using MSBuild properties directly to override any .csproj.user or Directory.Build.props values
		// Explicitly targeting the detected framework (net10.0 on CI, net9.0 locally)
		var buildInfo = new ProcessStartInfo
		{
			FileName = "dotnet",
			Arguments = $"build \"{appProject}\" --configuration Debug --verbosity minimal -p:UnoRemoteControlHost=localhost -p:UnoRemoteControlPort={devServerPort}",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
			WorkingDirectory = Path.GetDirectoryName(appProject)!,
		};

		var (exitCode, output) = await ProcessUtil.RunProcessAsync(buildInfo);

		TestContext!.WriteLine($"Build output: {output}");

		if (exitCode != 0)
		{
			throw new InvalidOperationException($"dotnet build failed with exit code {exitCode}. Output:\n{output}");
		}

		return appProject;
	}

	/// <summary>
	/// Starts the Skia desktop application with devserver connection enabled.
	/// </summary>
	private async Task<Process> StartSkiaDesktopAppAsync(string projectPath, int devServerPort)
	{
		var appTfm = $"{_targetFramework}-desktop";

		// Before starting the app, make sure it will run with the freshly compiled RemoteControlClient
		try
		{
			var projectDir = Path.GetDirectoryName(projectPath)!;
			var appOutputDir = Path.Combine(projectDir, "bin", "Debug", appTfm);
			var freshRcDll = typeof(Uno.UI.RemoteControl.RemoteControlClient).Assembly.Location;
			var destRcDll = Path.Combine(appOutputDir, Path.GetFileName(freshRcDll));

			Directory.CreateDirectory(appOutputDir);
			File.Copy(freshRcDll, destRcDll, overwrite: true);
			// Also copy PDB if available for better diagnostics
			var freshRcPdb = Path.ChangeExtension(freshRcDll, ".pdb");
			var destRcPdb = Path.ChangeExtension(destRcDll, ".pdb");
			if (File.Exists(freshRcPdb))
			{
				File.Copy(freshRcPdb, destRcPdb, overwrite: true);
			}
			TestContext!.WriteLine($"Overwrote RemoteControlClient assembly: {destRcDll}");
		}
		catch (Exception copyEx)
		{
			TestContext!.WriteLine($"Warning: Failed to overwrite RemoteControlClient assembly: {copyEx}");
		}

		var runInfo = new ProcessStartInfo
		{
			FileName = "dotnet",
			Arguments = $"run --project \"{projectPath}\" --configuration Debug --framework {appTfm} --no-build",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
			WorkingDirectory = Path.GetDirectoryName(projectPath)!,
		};

		// Set environment for clean execution (no devserver config needed here since it's baked into the build)
		runInfo.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en";

		var process = new Process { StartInfo = runInfo };

		// Set up output capturing for diagnostic purposes
		var outputBuilder = new StringBuilder();
		process.OutputDataReceived += (sender, e) =>
		{
			if (e.Data != null)
			{
				outputBuilder.AppendLine(e.Data);
				TestContext!.WriteLine($"[APP-OUT] {e.Data}");
			}
		};

		process.ErrorDataReceived += (sender, e) =>
		{
			if (e.Data != null)
			{
				outputBuilder.AppendLine(e.Data);
				TestContext!.WriteLine($"[APP-ERR] {e.Data}");
			}
		};

		process.Start();
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();

		TestContext!.WriteLine($"Started Skia desktop app process with PID: {process.Id}");

		// Wait a moment for the app to start
		await Task.Delay(2000, CT);

		if (process.HasExited)
		{
			throw new InvalidOperationException($"Skia app exited immediately with code {process.ExitCode}. Output: {outputBuilder}");
		}

		return process;
	}

	/// <summary>
	/// Waits for the Skia application to connect to the devserver.
	/// This will be a real connection test - the app should connect on its own through the generated ServerEndpointAttribute.
	/// </summary>
	private async Task WaitForAppToConnectoToDevServerAsync(DevServerTestHelper helper, TimeSpan timeout)
	{
		var startTime = Stopwatch.GetTimestamp();

		TestContext!.WriteLine("Waiting for real Skia app to connect to devserver...");
		TestContext!.WriteLine("The app should connect automatically via the generated ServerEndpointAttribute during build.");

		// For this integration test, we'll wait a reasonable time for the app to start
		// and assume success if no catastrophic errors occur. The goal is to verify
		// that a real Skia Desktop app can be built and launched with devserver configuration.

		var connectionDetected = false;
		var iterations = 0;
		const int maxIterations = 15; // 15 seconds max wait

		while (iterations < maxIterations && !connectionDetected)
		{
			await Task.Delay(1000, CT);
			iterations++;

			// Check if we can see the app connection in devserver output
			var devServerOutput = helper.ConsoleOutput;

			TestContext!.WriteLine($"[{iterations}/{maxIterations}] Checking devserver output... ({devServerOutput.Length} chars)");

			// Look for connection success indicators
			if (devServerOutput.Contains("App Connected:"))
			{
				TestContext!.WriteLine("✅ SUCCESS: Skia app successfully connected to devserver!");
				TestContext!.WriteLine("Connection detected in devserver logs - integration test objective achieved.");
				connectionDetected = true;
				break;
			}
		}

		if (!connectionDetected)
		{
			TestContext!.WriteLine("⚠️ Connection not detected in logs, but test may still be successful.");
			TestContext!.WriteLine("The real Skia app was built and launched successfully with devserver configuration.");
			TestContext!.WriteLine($"DevServer output: {helper.ConsoleOutput}");
		}
	}

	private static List<(string Prefix, JsonDocument Json)> ParseTelemetryFileIfExists(string path)
		=> File.Exists(path) ? ParseTelemetryEvents(File.ReadAllText(path)) : [];

	private static void DeleteIfExists(string path)
	{
		if (File.Exists(path)) { try { File.Delete(path); } catch { } }
	}
}
