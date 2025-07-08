using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.RemoteControl.DevServer.Tests.Helpers;

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
	}

	public async Task CreateSolutionFile()
	{
		if (isDisposed)
		{
			throw new ObjectDisposedException(nameof(SolutionHelper));
		}

		if (!Directory.Exists(_tempFolder))
		{
			Directory.CreateDirectory(_tempFolder);
		}

		var startInfo = new ProcessStartInfo
		{
			FileName = "dotnet",
			Arguments = $"new unoapp -n {_solutionFileName} -o {_tempFolder}",
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

	public static void EnsureUnoTemplatesInstalled()
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
					CreateNoWindow = true
				};
				var (checkExit, checkOutput) = ProcessUtil.RunProcessAsync(checkInfo).GetAwaiter().GetResult();

				if (!checkOutput.Contains("unoapp", StringComparison.OrdinalIgnoreCase))
				{
					Console.WriteLine("[DEBUG_LOG] unoapp template not found, attempting to install Uno.ProjectTemplates...");

					// Try to install the Uno templates
					var installInfo = new ProcessStartInfo
					{
						FileName = "dotnet",
						Arguments = "new install Uno.ProjectTemplates",
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						UseShellExecute = false,
						CreateNoWindow = true
					};
					var (installExit, installOutput) = ProcessUtil.RunProcessAsync(installInfo).GetAwaiter().GetResult();

					if (installExit != 0)
					{
						throw new InvalidOperationException($"Failed to install Uno.ProjectTemplates. Exit code: {installExit}. Output: {installOutput}");
					}

					Console.WriteLine("[DEBUG_LOG] Uno.ProjectTemplates installed successfully");

					// Verify the template is now available
					var verifyInfo = new ProcessStartInfo
					{
						FileName = "dotnet",
						Arguments = "new list unoapp",
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						UseShellExecute = false,
						CreateNoWindow = true
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
