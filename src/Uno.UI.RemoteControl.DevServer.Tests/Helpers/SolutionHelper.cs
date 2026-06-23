using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Uno.UI.RemoteControl.DevServer.Tests.Helpers;

#pragma warning disable VSTHRD002 // Async methods must run in sync context here

public class SolutionHelper : IDisposable
{
	// Should be `null` when targeting stable .NET versions, "netX.0" for prerelease .NET versions.
	internal static readonly string? OverridePrereleaseTargetFrameworkVersion = "net11.0";

	private readonly TestContext _testContext;
	private readonly string _solutionFileName;
	private readonly string _tempFolder;

	public string SolutionFolder => _tempFolder;
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

	public async Task<string> CreateSolutionFileAsync(
		string platforms = "wasm,desktop",
		string? targetFramework = null)
	{
		if (_isDisposed)
		{
			throw new ObjectDisposedException(nameof(SolutionHelper));
		}

		var platformArgs = string.Join(" ", platforms.Split(',').Select(p => $"-platforms \"{p.Trim()}\""));
		var tfmArg = targetFramework != null ? $"-tfm \"{targetFramework}\"" : "";
		var arguments = $"new unoapp -n {_solutionFileName} -o {_tempFolder} -preset \"recommended\" {platformArgs} {tfmArg}".Trim();

		var startInfo = new ProcessStartInfo
		{
			FileName = GetDotnetPath(),
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

		if (OverridePrereleaseTargetFrameworkVersion != null)
		{
			var globalJsonPath = Path.Combine(_tempFolder, "global.json");
			var globalJsonContents = File.ReadAllText(Path.Combine(_tempFolder, "global.json"));
			globalJsonContents = globalJsonContents.Replace("allowPrerelease\": false", "allowPrerelease\": true");
			File.WriteAllText(globalJsonPath, globalJsonContents);

			var projectCsprojPath = Path.Combine(_tempFolder, _solutionFileName, $"{_solutionFileName}.csproj");
			var projectCsprojContents = File.ReadAllText(projectCsprojPath);
#pragma warning disable SYSLIB1045
			// error SYSLIB1045: Use 'GeneratedRegexAttribute' to generate the regular expression implementation at compile-time
			projectCsprojContents = Regex.Replace(projectCsprojContents, @"net[^.]+\.0", OverridePrereleaseTargetFrameworkVersion!);
#pragma warning restore SYSLIB1045
			File.WriteAllText(projectCsprojPath, projectCsprojContents);
		}

		return _solutionFileName;
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
					FileName = GetDotnetPath(),
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
						FileName = GetDotnetPath(),
						Arguments = "new install Uno.Templates@*-*",
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
						FileName = GetDotnetPath(),
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

	private static string? _dotnetPath;

	private static string GetDotnetPath()
	{
		if (_dotnetPath != null)
		{
			return _dotnetPath;
		}
		var dotnetInstallDir = Environment.GetEnvironmentVariable("DOTNET_INSTALL_DIR");
		if (string.IsNullOrEmpty(dotnetInstallDir))
		{
			return _dotnetPath = "dotnet";
		}
		return _dotnetPath = Path.Combine(dotnetInstallDir, "dotnet");
	}

	public async Task ShowDotnetVersionAsync()
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = GetDotnetPath(),
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
