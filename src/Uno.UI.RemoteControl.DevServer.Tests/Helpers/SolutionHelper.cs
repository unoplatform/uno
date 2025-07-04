using System.Diagnostics;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.Utilities;

namespace Uno.UI.RemoteControl.DevServer.Tests;

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
			WorkingDirectory = _tempFolder, // Set working directory to ensure all dependencies are found
		};

		using var process = Process.Start(startInfo);

		if (process == null)
		{
			throw new InvalidOperationException("Failed to start dotnet process");
		}

		var output = new StringBuilder();

		process.OutputDataReceived += (_, args) => output.AppendLine(args.Data);
		process.ErrorDataReceived += (_, args) => output.AppendLine(args.Data);

		process.BeginOutputReadLine();
		process.BeginErrorReadLine();

		await process.WaitForExitAsync();

		if (process.ExitCode != 0)
		{
			throw new InvalidOperationException($"dotnet new sln failed with exit code {process.ExitCode} / {output}");
		}
	}

	public void Dispose()
	{
		isDisposed = true;

		// Force delete temp folder
		Directory.Delete(_tempFolder, true);
	}
}
