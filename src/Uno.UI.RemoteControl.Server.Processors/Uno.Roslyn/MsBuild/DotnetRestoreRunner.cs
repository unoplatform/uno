using System;
using System.Collections.Generic;
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
	/// Restores <paramref name="projectPath"/> under <paramref name="globalProperties"/> (the
	/// workspace's allow-listed evaluation context, forwarded as <c>-p:</c> arguments so the
	/// restore evaluates the same project graph the workspace opened). Only throws
	/// <see cref="OperationCanceledException"/> when <paramref name="ct"/> is cancelled — all other
	/// failures are reported via <paramref name="reporter"/> and surface as <see langword="false"/>,
	/// so the caller decides how to degrade.
	/// </summary>
	internal static async Task<bool> TryRestoreAsync(string projectPath, IReadOnlyDictionary<string, string> globalProperties, IReporter reporter, CancellationToken ct, TimeSpan? timeout = null)
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

			// Forward the workspace's evaluation context so the restore resolves the SAME project
			// graph the workspace opened. Without Configuration/Platform/Solution*, a restore of a
			// property-conditioned targeting pack (e.g. a Release-only TargetingPackVersion) would
			// evaluate the default (Debug) graph and leave the reloaded workspace just as broken.
			foreach (var (key, value) in globalProperties)
			{
				startInfo.ArgumentList.Add($"-p:{key}={value}");
			}

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
			catch (OperationCanceledException)
			{
				// WaitForExitAsync observed either the caller's token or the timeout — the restore
				// is still running in both cases. Kill the whole tree (and wait for it to exit) so a
				// cancelled or timed-out init never orphans a long-running 'dotnet restore'.
				await KillProcessTreeAsync(process);

				if (ct.IsCancellationRequested)
				{
					throw;
				}

				reporter.Warn($"'dotnet restore' timed out after {(timeout ?? _defaultTimeout).TotalSeconds:F0}s; killed it.");
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

	/// <summary>
	/// Best-effort kill of the process and its children, then a bounded, uncancellable wait for
	/// the tree to exit. Swallows every failure (already exited, kill raced with a natural exit,
	/// or the wait elapsed) — cleanup must never mask the cancellation/timeout being handled.
	/// </summary>
	private static async Task KillProcessTreeAsync(Process process)
	{
		try
		{
			if (process.HasExited)
			{
				return;
			}

			process.Kill(entireProcessTree: true);

			using var reapTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
			await process.WaitForExitAsync(reapTimeout.Token);
		}
		catch
		{
			// Best effort; nothing actionable if the kill or the bounded wait did not complete.
		}
	}
}
