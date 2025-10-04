using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.RemoteControl.DevServer.Tests.Helpers;

#pragma warning disable VSTHRD002 // Async methods must run in sync context here

public class SolutionHelper : IDisposable
{
	private readonly TestContext _testContext;
	private readonly string _solutionFileName;
	private readonly string _tempFolder;

	public string TempFolder => _tempFolder;
	public string SolutionFile => Path.Combine(_tempFolder, _solutionFileName + ".sln");

	private bool _isDisposed;

	public SolutionHelper(TestContext testContext, string solutionFileName = "MyApp")
	{
		_testContext = testContext;
		_solutionFileName = solutionFileName;
		_tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

		if (!Directory.Exists(_tempFolder))
		{
			Directory.CreateDirectory(_tempFolder);
		}
	}

	public async Task CreateSolutionFileAsync(
		bool copyGlobalJson = false,
		string platforms = "wasm,desktop")
	{
		if (_isDisposed)
		{
			throw new ObjectDisposedException(nameof(SolutionHelper));
		}

		var platformArgs = string.Join(" ", platforms.Split(',').Select(p => $"-platforms \"{p.Trim()}\""));
		var arguments = $"new unoapp -n {_solutionFileName} -o {_tempFolder} -preset \"recommended\" {platformArgs}";

		var startInfo = new ProcessStartInfo
		{
			FileName = "dotnet",
			Arguments = arguments,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
			WorkingDirectory = _tempFolder,
		};

		var (exitCode, output) = await ProcessUtil.RunProcessAsync(startInfo);
		if (exitCode != 0)
		{
			throw new InvalidOperationException($"dotnet new unoapp failed with exit code {exitCode} / {output}.\n>dotnet {arguments}");
		}

		// Optionally copy the global.json from the Uno repo to the temp folder to ensure we use the correct SDK version
		// This is critical for CI environments where .NET 10 prerelease might be installed
		if (copyGlobalJson)
		{
			CopyGlobalJsonToTempFolder();
		}
	}

	private void CopyGlobalJsonToTempFolder()
	{
		// Find the global.json in the Uno repo (walking up from the current assembly location)
		var assemblyLocation = Path.GetDirectoryName(typeof(SolutionHelper).Assembly.Location)!;
		var currentDir = assemblyLocation;
		string? globalJsonPath = null;

		// Walk up the directory tree to find global.json
		while (currentDir != null)
		{
			var candidatePath = Path.Combine(currentDir, "global.json");
			if (File.Exists(candidatePath))
			{
				globalJsonPath = candidatePath;
				break;
			}
			currentDir = Path.GetDirectoryName(currentDir);
		}

		if (globalJsonPath != null)
		{
			var targetPath = Path.Combine(_tempFolder, "global.json");
			File.Copy(globalJsonPath, targetPath, overwrite: true);
			Console.WriteLine($"[DEBUG_LOG] Copied global.json from {globalJsonPath} to {targetPath}");
		}
		else
		{
			Console.WriteLine("[DEBUG_LOG] Warning: Could not find global.json in parent directories");
		}
	}

	private static object _lock = new();

	public void EnsureUnoTemplatesInstalled()
	{
		lock (_lock)
		{
			try
			{
				// First, check if the unoapp template is already available
				var checkInfo = new ProcessStartInfo
				{
					FileName = "dotnet",
					Arguments = "new list unoapp",
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true,
					WorkingDirectory = _tempFolder,
				};
				var (checkExit, checkOutput) = ProcessUtil.RunProcessAsync(checkInfo).Result;

				if (checkExit != 0)
				{
					Console.WriteLine("[DEBUG_LOG] unoapp template not found, attempting to install Uno.Templates...");

					// Try to install the Uno templates
					var installInfo = new ProcessStartInfo
					{
						FileName = "dotnet",
						Arguments = "new install Uno.Templates::*-*",
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						UseShellExecute = false,
						CreateNoWindow = true,
						WorkingDirectory = _tempFolder,
					};
					var (installExit, installOutput) = ProcessUtil.RunProcessAsync(installInfo).Result;

					if (installExit != 0)
					{
						throw new InvalidOperationException($"Failed to install Uno.Templates. Exit code: {installExit}. Output: {installOutput}");
					}

					Console.WriteLine("[DEBUG_LOG] Uno.Templates installed successfully");

					// Verify the template is now available
					var verifyInfo = new ProcessStartInfo
					{
						FileName = "dotnet",
						Arguments = "new list unoapp",
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						UseShellExecute = false,
						CreateNoWindow = true,
						WorkingDirectory = _tempFolder,
					};
					var (verifyExit, verifyOutput) = ProcessUtil.RunProcessAsync(verifyInfo).GetAwaiter().GetResult();

					if (!verifyOutput.Contains("unoapp", StringComparison.OrdinalIgnoreCase))
					{
						throw new InvalidOperationException($"unoapp template still not available after installation. Verify output: {verifyOutput}");
					}

					Console.WriteLine("[DEBUG_LOG] unoapp template verified as available");
				}
				else
				{
					Console.WriteLine("[DEBUG_LOG] unoapp template is already available");
				}
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"Unable to ensure Uno.ProjectTemplates are installed: {ex.Message}", ex);
			}
		}
	}

	public async Task ShowDotnetVersionAsync()
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = "dotnet",
			Arguments = "--info",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
			WorkingDirectory = _tempFolder,
		};

		var (exitCode, output) = await ProcessUtil.RunProcessAsync(startInfo);
		_testContext.WriteLine($"dotnet --info output:\n{output}");
	}

	public void Dispose()
	{
		_isDisposed = true;

		// Force delete temp folder
		Directory.Delete(_tempFolder, true);
	}
}
