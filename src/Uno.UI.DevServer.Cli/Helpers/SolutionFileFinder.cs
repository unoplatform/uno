using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Uno.UI.DevServer.Cli.Helpers;

// Directory filtering uses a two-layer strategy: a hardcoded skip list is
// always applied during traversal, and `git check-ignore` is applied only to
// discovered solution files when available. This keeps discovery cheap while
// still honoring `.gitignore` for the solutions we might select.
//
// This ensures robust behavior across all environments:
//
// | # | `.git` exists | `git` installed | `.gitignore` | Result                                                                  |
// |---|---------------|-----------------|--------------|-------------------------------------------------------------------------|
// | 1 | no            | no              | no           | Hardcoded skip list only                                                |
// | 2 | no            | no              | yes          | Hardcoded skip list only (`.gitignore` ignored without a repo)          |
// | 3 | no            | yes             | no           | Hardcoded skip list only (not in a repo)                                |
// | 4 | no            | yes             | yes          | Hardcoded skip list only (`.gitignore` ignored without `.git`)          |
// | 5 | yes           | no              | no           | Hardcoded skip list only (`Process.Start` throws, caught gracefully)    |
// | 6 | yes           | no              | yes          | Hardcoded skip list only (same as #5)                                   |
// | 7 | yes (worktree)| no              | yes          | Hardcoded skip list only (worktree detected, but git missing)           |
// | 8 | fake/corrupt  | yes             | no           | Hardcoded skip list only (`git check-ignore` exits 128 → null fallback) |
// | 9 | yes           | yes             | no           | Hardcoded skip list only (`git check-ignore` returns empty set)         |
// |10 | yes           | yes             | yes          | **Hardcoded skip list + `.gitignore` rules (union)**                    |
// |11 | yes           | yes (hangs)     | yes          | Hardcoded skip list only (2s timeout → kill → null fallback)            |
// |12 | yes (worktree)| yes             | yes          | Same as #10 — worktree detected correctly                              |
//
// No scenario can result in accidentally empty filtering.

/// <summary>
/// Searches for .sln/.slnx files recursively, respecting .gitignore rules
/// for discovered solutions when running inside a git repository.
/// </summary>
internal static class SolutionFileFinder
{
	/// <summary>
	/// Default directories to skip when not inside a git repository.
	/// </summary>
	private static readonly string[] HardcodedSkipDirs =
		["node_modules", "bin", "obj", ".vs", ".idea", "packages"];

	private static bool ShouldAlwaysSkipDirectory(string directoryName)
	{
		if (HardcodedSkipDirs.Contains(directoryName, StringComparer.OrdinalIgnoreCase))
		{
			return true;
		}

		// Codex homes/sessions can be created next to the workspace during tests
		// and should never influence solution discovery.
		return directoryName.StartsWith(".codex", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Finds solution files (.sln, .slnx) recursively up to <paramref name="maxDepth"/> levels.
	/// When inside a git repo, uses <c>git check-ignore</c> to skip ignored directories.
	/// Otherwise, uses a hardcoded skip list.
	/// </summary>
	public static string[] FindSolutionFiles(string directory, int maxDepth = 3)
	{
		var results = new List<string>();
		var gitRoot = FindGitRoot(directory);
		SearchDirectory(directory, 0, maxDepth, results);

		if (gitRoot is null || results.Count == 0)
		{
			return results.ToArray();
		}

		var gitIgnoredSolutions = GetGitIgnoredPaths(results, gitRoot);
		if (gitIgnoredSolutions is null || gitIgnoredSolutions.Count == 0)
		{
			return results.ToArray();
		}

		return
		[
			.. results.Where(solution => !gitIgnoredSolutions.Contains(solution))
		];
	}

	/// <summary>
	/// Finds the root of the git repository containing <paramref name="directory"/>,
	/// or returns <c>null</c> if the directory is not inside a git repo.
	/// </summary>
	public static string? FindGitRoot(string directory)
	{
		var current = directory;
		while (current is not null)
		{
			var gitPath = Path.Combine(current, ".git");
			// .git can be a directory (normal repo) or a file (worktree)
			if (Directory.Exists(gitPath) || File.Exists(gitPath))
			{
				return current;
			}
			current = Path.GetDirectoryName(current);
		}
		return null;
	}

	/// <summary>
	/// Uses <c>git check-ignore</c> to determine which of the given paths
	/// are ignored by .gitignore rules. Returns the set of ignored paths,
	/// or <c>null</c> if git is not available or fails (so the caller can
	/// fall back to the hardcoded skip list).
	/// </summary>
	public static HashSet<string>? GetGitIgnoredPaths(List<string> paths, string gitRoot)
	{
		if (paths.Count == 0)
		{
			return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		}

		try
		{
			// Build relative paths for git check-ignore (absolute Windows paths don't work)
			var gitRootFull = Path.GetFullPath(gitRoot);
			var relativeToAbsolute = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			var relativePaths = new List<string>();
			foreach (var path in paths)
			{
				var relativePath = Path.GetRelativePath(gitRootFull, path).Replace('\\', '/');
				relativeToAbsolute[relativePath] = path;
				relativePaths.Add(relativePath);
			}

			// Pass paths as arguments (--stdin has issues on Windows via ProcessStartInfo)
			var psi = new ProcessStartInfo("git")
			{
				WorkingDirectory = gitRoot,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			};
			psi.ArgumentList.Add("check-ignore");
			foreach (var relativePath in relativePaths)
			{
				psi.ArgumentList.Add(relativePath);
			}

			using var process = Process.Start(psi);
			if (process is null)
			{
				return null; // git not available
			}

			// Kill the process after a timeout to prevent hanging. This
			// ensures that the subsequent synchronous ReadToEnd() will
			// return promptly because killing closes the stdout pipe.
			if (!process.WaitForExit(2000))
			{
				// git hung — kill it and fall back
				try { process.Kill(); } catch { /* best effort */ }
				return null;
			}

			// Exit code 128 = not a git repo, other non-0/1 = unexpected error
			// Exit code 0 = at least one path is ignored, 1 = none are ignored (both valid)
			if (process.ExitCode is not (0 or 1))
			{
				return null;
			}

			// Safe to read synchronously: the process has exited, so stdout is closed.
			var output = process.StandardOutput.ReadToEnd();
			var ignored = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
			{
				var trimmed = line.Trim();
				if (trimmed.Length > 0 && relativeToAbsolute.TryGetValue(trimmed, out var absolutePath))
				{
					ignored.Add(absolutePath);
				}
			}

			return ignored;
		}
		catch
		{
			// git not found or other error — caller will use hardcoded skip list
			return null;
		}
	}

	private static void SearchDirectory(string directory, int currentDepth, int maxDepth,
		List<string> results)
	{
		try
		{
			foreach (var file in Directory.EnumerateFiles(directory, "*.sln"))
			{
				results.Add(file);
			}
			foreach (var file in Directory.EnumerateFiles(directory, "*.slnx"))
			{
				results.Add(file);
			}
		}
		catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException or IOException)
		{
			// Skip directories we can't access or that disappeared during scanning
		}

		if (currentDepth >= maxDepth)
		{
			return;
		}

		try
		{
			var subDirs = Directory.EnumerateDirectories(directory).ToList();

			// Always skip .git directory (contains git objects, never solutions)
			subDirs.RemoveAll(d => string.Equals(Path.GetFileName(d), ".git", StringComparison.OrdinalIgnoreCase));

			// Only the hardcoded skip list participates in traversal. Git-based
			// filtering is applied once at the end on the discovered solution set,
			// which avoids paying for `git check-ignore` on every directory.
			var ignored = subDirs
				.Where(d => ShouldAlwaysSkipDirectory(Path.GetFileName(d)))
				.ToHashSet(StringComparer.OrdinalIgnoreCase);

			foreach (var subDir in subDirs)
			{
				if (ignored.Contains(subDir))
				{
					continue;
				}
				SearchDirectory(subDir, currentDepth + 1, maxDepth, results);
			}
		}
		catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException or IOException)
		{
			// Skip directories we can't access or that disappeared during scanning
		}
	}
}
