using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Uno.HotReload.Tracking;

namespace Uno.Roslyn.MSBuild;

/// <summary>
/// Runs <c>dotnet restore</c> on a project in the same SDK-resolution context as Roslyn's
/// out-of-process BuildHost: the <c>dotnet</c> muxer from the host process <c>PATH</c>, the
/// environment inherited, and the project directory as working directory (so
/// <c>global.json</c> applies). Used by the missing-targeting-pack recovery (spec 049): the
/// restore materializes the <c>PackageDownload</c>-satisfied targeting packs into the NuGet
/// cache, from which the next design-time build resolves them.
/// </summary>
internal static class DotnetRestoreRunner
{
	private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(120);

	/// <summary>
	/// Restores <paramref name="projectPath"/>. Never throws: failures are reported and
	/// surface as <see langword="false"/> — the caller decides how to degrade.
	/// </summary>
	internal static async Task<bool> TryRestoreAsync(string projectPath, IReporter reporter, CancellationToken ct, TimeSpan? timeout = null)
	{
		try
		{
			var startInfo = new ProcessStartInfo
			{
				// "dotnet" from PATH, environment inherited: the same muxer resolution the
				// BuildHost uses, so the restored state matches the SDK of the workspace.
				FileName = "dotnet",
				WorkingDirectory = Path.GetDirectoryName(projectPath) ?? Environment.CurrentDirectory,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
			};
			startInfo.ArgumentList.Add("restore");
			startInfo.ArgumentList.Add(projectPath);

			using var process = new Process { StartInfo = startInfo };

			process.OutputDataReceived += (_, e) =>
			{
				if (e.Data is { Length: > 0 } line)
				{
					reporter.Verbose($"dotnet restore: {line}");
				}
			};
			process.ErrorDataReceived += (_, e) =>
			{
				if (e.Data is { Length: > 0 } line)
				{
					reporter.Verbose($"dotnet restore (stderr): {line}");
				}
			};

			reporter.Output($"Running 'dotnet restore \"{projectPath}\"' to recover the hot-reload workspace.");

			if (!process.Start())
			{
				reporter.Warn("Failed to start 'dotnet restore' (process did not start).");
				return false;
			}

			process.BeginOutputReadLine();
			process.BeginErrorReadLine();

			using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			timeoutCts.CancelAfter(timeout ?? _defaultTimeout);

			try
			{
				await process.WaitForExitAsync(timeoutCts.Token);
			}
			catch (OperationCanceledException) when (!ct.IsCancellationRequested)
			{
				reporter.Warn($"'dotnet restore' timed out after {(timeout ?? _defaultTimeout).TotalSeconds:F0}s; killing it.");
				try
				{
					process.Kill(entireProcessTree: true);
				}
				catch
				{
					// The process may have exited in the meantime; nothing else to do.
				}

				return false;
			}

			if (process.ExitCode == 0)
			{
				reporter.Output("'dotnet restore' completed successfully.");
				return true;
			}

			reporter.Warn($"'dotnet restore' exited with code {process.ExitCode}; the workspace will be reloaded anyway (a partial restore may still have materialized the missing packs).");
			return false;
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception e)
		{
			reporter.Warn($"'dotnet restore' failed to run: {e.Message}");
			return false;
		}
	}
}
