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
			throw new InvalidOperationException($"dotnet new sln failed with exit code {exitCode} / {output}");
		}
	}

	public static void EnsureUnoTemplatesInstalled()
	{
		try
		{
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
					Console.WriteLine(
						$"[WARNING] dotnet new install Uno.ProjectTemplates failed (best effort): {installOutput}");
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(
				$"[WARNING] Unable to check or install Uno.ProjectTemplates (best effort, CI only): {ex.Message}");
		}
	}

	public void Dispose()
	{
		isDisposed = true;

		// Force delete temp folder
		Directory.Delete(_tempFolder, true);
	}
}
