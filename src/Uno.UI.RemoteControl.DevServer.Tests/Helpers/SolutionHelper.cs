using System.Diagnostics;
using System.Text;

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

	/// <summary>
	/// Vérifie et installe le template Uno si nécessaire (dotnet new install Uno.ProjectTemplates).
	/// Cette installation automatique est surtout utile pour le CI, où le template peut ne pas être préinstallé.
	/// En local, il est recommandé d'installer le template manuellement pour éviter les lenteurs ou erreurs d'environnement.
	/// Si le SDK .NET requis n'est pas disponible, l'installation est ignorée avec un avertissement.
	/// </summary>
	public static void EnsureUnoTemplatesInstalled()
	{
		try
		{
			var checkProcess = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "dotnet",
					Arguments = "new list unoapp",
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true
				}
			};

			checkProcess.Start();
			string output = checkProcess.StandardOutput.ReadToEnd();
			string error = checkProcess.StandardError.ReadToEnd();
			checkProcess.WaitForExit();

			if (!output.Contains("unoapp", StringComparison.OrdinalIgnoreCase))
			{
				// Installer le template Uno (utile surtout en CI)
				var installProcess = new Process
				{
					StartInfo = new ProcessStartInfo
					{
						FileName = "dotnet",
						Arguments = "new install Uno.ProjectTemplates",
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						UseShellExecute = false,
						CreateNoWindow = true
					}
				};
				installProcess.Start();
				string installOutput = installProcess.StandardOutput.ReadToEnd();
				string installError = installProcess.StandardError.ReadToEnd();
				installProcess.WaitForExit();

				if (installProcess.ExitCode != 0)
				{
					Console.WriteLine($"[WARNING] dotnet new install Uno.ProjectTemplates failed (best effort): {installOutput}\n{installError}");
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[WARNING] Unable to check or install Uno.ProjectTemplates (best effort, CI only): {ex.Message}");
		}
	}

	public void Dispose()
	{
		isDisposed = true;

		// Force delete temp folder
		Directory.Delete(_tempFolder, true);
	}
}
