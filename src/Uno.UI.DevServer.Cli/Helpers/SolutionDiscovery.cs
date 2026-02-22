using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Uno.UI.DevServer.Cli.Helpers;

/// <summary>
/// Provides methods to discover solution files in the current directory and subdirectories.
/// This allows the DevServer to be started from a parent directory (e.g., repository root)
/// when solution files are located in subdirectories like 'src/'.
/// </summary>
internal static class SolutionDiscovery
{
	/// <summary>
	/// Prioritized subdirectory names to search first. These are common source directory names.
	/// </summary>
	private static readonly string[] PrioritizedSubdirectories = ["src", "source", "app"];

	/// <summary>
	/// Directory names to skip during search. These typically contain build artifacts or dependencies.
	/// </summary>
	private static readonly HashSet<string> SkippedDirectories = new(StringComparer.OrdinalIgnoreCase)
	{
		"bin", "obj", "node_modules", "packages", ".git", ".vs", ".idea"
	};

	/// <summary>
	/// Maximum depth to search for solution files in subdirectories.
	/// </summary>
	private const int MaxSearchDepth = 2;

	/// <summary>
	/// Discovers all solution files starting from the specified directory.
	/// First searches the current directory, then subdirectories up to MaxSearchDepth.
	/// Results are ordered by depth (shallower first), then alphabetically.
	/// </summary>
	/// <param name="startDirectory">The directory to start searching from.</param>
	/// <param name="logger">Optional logger for diagnostic output.</param>
	/// <returns>Array of full paths to discovered solution files, or empty array if none found.</returns>
	public static string[] DiscoverSolutions(string startDirectory, ILogger? logger = null)
	{
		var solutions = new List<(string path, int depth)>();

		// First, check current directory (depth 0)
		var currentDirSolutions = GetSolutionFilesInDirectory(startDirectory);
		if (currentDirSolutions.Length > 0)
		{
			logger?.LogDebug("Found {Count} solution(s) in current directory: {Directory}", currentDirSolutions.Length, startDirectory);
			return currentDirSolutions.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToArray();
		}

		// Search subdirectories up to MaxSearchDepth
		logger?.LogDebug("No solutions found in current directory, searching subdirectories...");
		SearchSubdirectories(startDirectory, 1, solutions, logger);

		if (solutions.Count == 0)
		{
			logger?.LogDebug("No solution files found in {Directory} or its subdirectories", startDirectory);
			return [];
		}

		// Order by depth first, then alphabetically
		var result = solutions
			.OrderBy(s => s.depth)
			.ThenBy(s => s.path, StringComparer.OrdinalIgnoreCase)
			.Select(s => s.path)
			.ToArray();

		logger?.LogDebug("Found {Count} solution(s) in subdirectories", result.Length);
		return result;
	}

	/// <summary>
	/// Discovers the first solution file starting from the specified directory.
	/// Convenience method that returns the first result from DiscoverSolutions.
	/// </summary>
	/// <param name="startDirectory">The directory to start searching from.</param>
	/// <param name="logger">Optional logger for diagnostic output.</param>
	/// <returns>Full path to the first discovered solution file, or null if none found.</returns>
	public static string? DiscoverFirstSolution(string startDirectory, ILogger? logger = null)
	{
		return DiscoverSolutions(startDirectory, logger).FirstOrDefault();
	}

	private static void SearchSubdirectories(string directory, int currentDepth, List<(string path, int depth)> results, ILogger? logger)
	{
		if (currentDepth > MaxSearchDepth)
		{
			return;
		}

		IEnumerable<string> subdirectories;
		try
		{
			subdirectories = Directory.EnumerateDirectories(directory);
		}
		catch (UnauthorizedAccessException)
		{
			return;
		}
		catch (DirectoryNotFoundException)
		{
			return;
		}

		// Sort subdirectories: prioritized ones first, then alphabetically
		var sortedSubdirs = subdirectories
			.Select(d => new { Path = d, Name = Path.GetFileName(d) })
			.Where(d => !ShouldSkipDirectory(d.Name))
			.OrderBy(d => GetDirectoryPriority(d.Name))
			.ThenBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
			.Select(d => d.Path)
			.ToList();

		foreach (var subdir in sortedSubdirs)
		{
			var solutions = GetSolutionFilesInDirectory(subdir);
			foreach (var solution in solutions)
			{
				results.Add((solution, currentDepth));
				logger?.LogDebug("Found solution at depth {Depth}: {Path}", currentDepth, solution);
			}

			// Recurse into subdirectories
			SearchSubdirectories(subdir, currentDepth + 1, results, logger);
		}
	}

	private static string[] GetSolutionFilesInDirectory(string directory)
	{
		try
		{
			return Directory
				.EnumerateFiles(directory, "*.sln")
				.Concat(Directory.EnumerateFiles(directory, "*.slnx"))
				.ToArray();
		}
		catch (UnauthorizedAccessException)
		{
			return [];
		}
		catch (DirectoryNotFoundException)
		{
			return [];
		}
	}

	private static bool ShouldSkipDirectory(string directoryName)
	{
		// Skip hidden directories (starting with . or _)
		if (directoryName.StartsWith('.') || directoryName.StartsWith('_'))
		{
			return true;
		}

		return SkippedDirectories.Contains(directoryName);
	}

	private static int GetDirectoryPriority(string directoryName)
	{
		var index = Array.FindIndex(PrioritizedSubdirectories, p => p.Equals(directoryName, StringComparison.OrdinalIgnoreCase));
		return index >= 0 ? index : int.MaxValue;
	}
}
