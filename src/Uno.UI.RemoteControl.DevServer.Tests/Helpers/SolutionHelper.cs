using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.RemoteControl.DevServer.Tests.Helpers;

#pragma warning disable VSTHRD002 // Async methods must run in sync context here

public class SolutionHelper : IDisposable
{
	private readonly string _solutionFileName;
	private readonly string _tempFolder;

	public string TempFolder => _tempFolder;
	public string SolutionFile => Path.Combine(_tempFolder, _solutionFileName + ".sln");

	private bool isDisposed;

	public SolutionHelper(string solutionFileName = "MyApp")
	{
		_solutionFileName = solutionFileName;
		_tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

		if (!Directory.Exists(_tempFolder))
		{
			Directory.CreateDirectory(_tempFolder);
		}
	}

	public async Task CreateSolutionFileAsync()
	{
		if (isDisposed)
		{
			throw new ObjectDisposedException(nameof(SolutionHelper));
		}

		var startInfo = new ProcessStartInfo
		{
			FileName = "dotnet",
			Arguments = $"new unoapp -n {_solutionFileName} -o {_tempFolder} -preset \"recommended\" -platforms \"wasm\" -platforms \"desktop\"",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
			WorkingDirectory = _tempFolder,
		};

		var (exitCode, output) = await ProcessUtil.RunProcessAsync(startInfo);
		if (exitCode != 0)
		{
			throw new InvalidOperationException($"dotnet new unoapp failed with exit code {exitCode} / {output}");
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

	public void Dispose()
	{
		isDisposed = true;

		// Force delete temp folder
		Directory.Delete(_tempFolder, true);
	}
}
