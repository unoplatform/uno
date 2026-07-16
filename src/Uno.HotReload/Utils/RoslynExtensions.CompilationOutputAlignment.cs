using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;
using Uno.HotReload.Tracking;

namespace Uno.HotReload.Utils;

public static partial class RoslynExtensions
{
	/// <summary>
	/// Re-points the compilation outputs of the head-project flavor(s) kept in the solution to
	/// the output assembly the running application was actually built from.
	/// </summary>
	/// <remarks>
	/// The workspace evaluates the head without a <c>RuntimeIdentifier</c>, so
	/// <see cref="Project.CompilationOutputInfo"/>.AssemblyPath is the RID-less intermediate
	/// path (<c>obj/Debug/netX.0-android/App.dll</c>). A device deployment however builds
	/// RID-specific (<c>obj/Debug/netX.0-android/android-arm64/App.dll</c>): the RID-less file
	/// either does not exist — Roslyn's EnC then treats the project as "not built" and silently
	/// emits no update — or is a stale twin from an earlier RID-less build whose MVID does not
	/// match the deployed module, so emitted deltas would be dropped by the application. Only
	/// the head flavor needs re-pointing: project references are built RID-less by MSBuild's
	/// project-to-project protocol, their evaluated paths are correct.
	///
	/// The trigger is declarative — the <c>RuntimeIdentifier</c> captured from the running
	/// application's build — and a candidate path is only accepted when its module is readable
	/// (a valid PE with an MVID), the same primitive Roslyn's EnC uses to decide a project is
	/// "built". When nothing readable is found the situation is reported as a warning: without
	/// it, the EnC "project not built" outcome is invisible outside Roslyn's own session log.
	/// </remarks>
	/// <param name="solution">The (already flavor-filtered) solution.</param>
	/// <param name="headProjectPath">Full path of the head project (<c>.csproj</c>) the application runs.</param>
	/// <param name="runtimeIdentifier">The runtime identifier captured from the running application's build, when any.</param>
	/// <param name="reporter">Reporter used to surface the alignment decisions.</param>
	public static Solution AlignHeadProjectCompilationOutputs(
		this Solution solution,
		string headProjectPath,
		string? runtimeIdentifier,
		IReporter reporter)
	{
		foreach (var projectId in solution.Projects
			.Where(p => PathComparer.PathEquals(p.FilePath, headProjectPath))
			.Select(p => p.Id)
			.ToList())
		{
			var project = solution.GetProject(projectId)!;
			var evaluatedPath = project.CompilationOutputInfo.AssemblyPath;
			if (string.IsNullOrEmpty(evaluatedPath))
			{
				reporter.Warn(
					$"The hot-reload workspace has no output assembly path for '{project.Name}' — Roslyn will consider the " +
					"project not built and hot reload will silently produce no updates.");
				continue;
			}

			if (string.IsNullOrEmpty(runtimeIdentifier))
			{
				// No RID captured from the running build: the evaluated (RID-less) path is the built
				// one. Only surface the anti-silence warning when it isn't readable.
				if (!TryReadMvid(evaluatedPath, out _))
				{
					reporter.Warn(
						$"The output assembly of '{project.Name}' was not found at '{evaluatedPath}' — Roslyn will consider " +
						"the project not built and hot reload will silently produce no updates. Build this target framework first.");
				}

				continue;
			}

			// The running application was built WITH a runtime identifier: prefer the RID-specific
			// output — it is the binary that was actually deployed. Even when the RID-less file
			// exists it can be a stale twin (an earlier RID-less build) with a mismatching MVID.
			var fileName = Path.GetFileName(evaluatedPath);
			var directory = Path.GetDirectoryName(evaluatedPath)!;
			var ridSpecificPath = Path.Combine(directory, runtimeIdentifier, fileName);

			if (TryReadMvid(ridSpecificPath, out _))
			{
				solution = Remap(project, ridSpecificPath);
			}
			else if (FindNewestReadableCandidate(directory, fileName, evaluatedPath) is { } candidate)
			{
				// Custom output layouts (artifacts output path, intermediate-path overrides, …):
				// probe the evaluated directory subtree for the assembly instead.
				solution = Remap(project, candidate);
			}
			else if (TryReadMvid(evaluatedPath, out _))
			{
				reporter.Output(
					$"The application was built with runtime identifier '{runtimeIdentifier}' but no RID-specific output was " +
					$"found for '{project.Name}'; keeping the evaluated '{evaluatedPath}'.");
			}
			else
			{
				reporter.Warn(
					$"The output assembly of '{project.Name}' was not found (probed '{ridSpecificPath}' and '{evaluatedPath}') — " +
					"Roslyn will consider the project not built and hot reload will silently produce no updates. " +
					"Deploy this target framework first.");
			}
		}

		return solution;

		Solution Remap(Project project, string path)
		{
			reporter.Output($"Hot-reload baseline of '{project.Name}' re-pointed to '{path}' (runtime identifier '{runtimeIdentifier}').");
			return solution.WithProjectCompilationOutputInfo(project.Id, project.CompilationOutputInfo.WithAssemblyPath(path));
		}
	}

	/// <summary>
	/// Probes the evaluated output directory subtree for a readable build of the assembly,
	/// excluding reference-assembly folders (<c>ref</c>/<c>refint</c> — readable PEs, but not an
	/// EnC baseline) and preferring the most recently written candidate.
	/// </summary>
	private static string? FindNewestReadableCandidate(string directory, string fileName, string evaluatedPath)
	{
		try
		{
			if (!Directory.Exists(directory))
			{
				return null;
			}

			return Directory
				.EnumerateFiles(directory, fileName, SearchOption.AllDirectories)
				.Where(path => !PathComparer.PathEquals(path, evaluatedPath))
				.Where(path => Path.GetDirectoryName(path) is { } dir
					&& Path.GetFileName(dir) is { } dirName
					&& !dirName.Equals("ref", StringComparison.OrdinalIgnoreCase)
					&& !dirName.Equals("refint", StringComparison.OrdinalIgnoreCase))
				.OrderByDescending(File.GetLastWriteTimeUtc)
				.FirstOrDefault(path => TryReadMvid(path, out _));
		}
		catch (Exception)
		{
			// IO race with a build in progress — treat as "no candidate" rather than failing the
			// workspace initialization.
			return null;
		}
	}

	/// <summary>
	/// Reads the module version id the way Roslyn's EnC decides a project is "built" (see
	/// Roslyn's <c>DebuggingSession.GetProjectModuleIdAsync</c>): a readable PE with metadata.
	/// </summary>
	private static bool TryReadMvid(string path, out Guid mvid)
	{
		mvid = default;
		try
		{
			using var stream = File.OpenRead(path);
			using var peReader = new PEReader(stream);
			var metadataReader = peReader.GetMetadataReader();
			mvid = metadataReader.GetGuid(metadataReader.GetModuleDefinition().Mvid);
			return mvid != Guid.Empty;
		}
		catch (Exception)
		{
			return false;
		}
	}
}
