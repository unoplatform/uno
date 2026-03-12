using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Uno.UI.DevServer.Cli.Helpers;

internal sealed class WorkspaceResolver(ILogger<WorkspaceResolver> logger)
{
	private readonly ILogger<WorkspaceResolver> _logger = logger;

	public async Task<WorkspaceResolution> ResolveAsync(string requestedDirectory)
	{
		var normalizedRequestedDirectory = Path.GetFullPath(requestedDirectory);
		var solutionFiles = SolutionFileFinder.FindSolutionFiles(normalizedRequestedDirectory);
		if (solutionFiles.Length == 0)
		{
			return new WorkspaceResolution
			{
				RequestedWorkingDirectory = normalizedRequestedDirectory,
				ResolutionKind = WorkspaceResolutionKind.NoCandidates,
				CandidateSolutions = [],
			};
		}

		var candidates = new List<WorkspaceCandidate>();

		foreach (var solutionPath in solutionFiles.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
		{
			var solutionDirectory = Path.GetDirectoryName(solutionPath);
			if (string.IsNullOrWhiteSpace(solutionDirectory))
			{
				continue;
			}

			var currentDirectory = Path.GetFullPath(solutionDirectory);
			while (currentDirectory is not null)
			{
				var globalJsonPath = Path.Combine(currentDirectory, "global.json");
				if (File.Exists(globalJsonPath))
				{
					var parsed = await GlobalJsonLocator.ParseGlobalJsonFileForUnoSdkAsync(globalJsonPath, _logger);
					if (!string.IsNullOrWhiteSpace(parsed.sdkPackage) && !string.IsNullOrWhiteSpace(parsed.sdkVersion))
					{
						candidates.Add(new WorkspaceCandidate(
							solutionPath,
							currentDirectory,
							globalJsonPath,
							GetDirectoryDistance(currentDirectory, solutionDirectory)));
					}

					break;
				}

				var parent = Directory.GetParent(currentDirectory);
				currentDirectory = parent?.FullName;
			}
		}

		if (candidates.Count == 0)
		{
			return new WorkspaceResolution
			{
				RequestedWorkingDirectory = normalizedRequestedDirectory,
				ResolutionKind = WorkspaceResolutionKind.NoValidWorkspace,
				CandidateSolutions = [.. solutionFiles],
			};
		}

		var bestDistance = candidates.Min(candidate => candidate.GlobalJsonDistance);
		var bestCandidates = candidates
			.Where(candidate => candidate.GlobalJsonDistance == bestDistance)
			.OrderBy(candidate => candidate.SolutionPath, StringComparer.OrdinalIgnoreCase)
			.ToList();

		if (bestCandidates.Count > 1)
		{
			return new WorkspaceResolution
			{
				RequestedWorkingDirectory = normalizedRequestedDirectory,
				ResolutionKind = WorkspaceResolutionKind.Ambiguous,
				CandidateSolutions = [.. bestCandidates.Select(candidate => candidate.SolutionPath)],
			};
		}

		var selected = bestCandidates[0];
		var resolutionKind = string.Equals(selected.WorkspaceDirectory, normalizedRequestedDirectory, StringComparison.OrdinalIgnoreCase)
			? WorkspaceResolutionKind.CurrentDirectory
			: WorkspaceResolutionKind.AutoDiscovered;

		return new WorkspaceResolution
		{
			RequestedWorkingDirectory = normalizedRequestedDirectory,
			EffectiveWorkspaceDirectory = selected.WorkspaceDirectory,
			SelectedSolutionPath = selected.SolutionPath,
			SelectedGlobalJsonPath = selected.GlobalJsonPath,
			ResolutionKind = resolutionKind,
			SelectionSource = WorkspaceSelectionSource.Automatic,
			CandidateSolutions = [.. candidates.Select(candidate => candidate.SolutionPath).Distinct(StringComparer.OrdinalIgnoreCase)],
		};
	}

	public async Task<WorkspaceResolution> ResolveSolutionAsync(string requestedDirectory, string solutionPath, WorkspaceSelectionSource selectionSource = WorkspaceSelectionSource.UserSelected)
	{
		var normalizedRequestedDirectory = Path.GetFullPath(requestedDirectory);
		var normalizedSolutionPath = Path.GetFullPath(solutionPath);
		var candidateSolutions = SolutionFileFinder.FindSolutionFiles(normalizedRequestedDirectory);

		if (!File.Exists(normalizedSolutionPath)
			|| !candidateSolutions.Contains(normalizedSolutionPath, StringComparer.OrdinalIgnoreCase))
		{
			return new WorkspaceResolution
			{
				RequestedWorkingDirectory = normalizedRequestedDirectory,
				ResolutionKind = candidateSolutions.Length == 0
					? WorkspaceResolutionKind.NoCandidates
					: WorkspaceResolutionKind.NoValidWorkspace,
				CandidateSolutions = candidateSolutions,
				SelectionSource = selectionSource,
			};
		}

		var resolved = await ResolveSolutionCoreAsync(normalizedRequestedDirectory, normalizedSolutionPath, selectionSource);
		return resolved ?? new WorkspaceResolution
		{
			RequestedWorkingDirectory = normalizedRequestedDirectory,
			ResolutionKind = WorkspaceResolutionKind.NoValidWorkspace,
			CandidateSolutions = candidateSolutions,
			SelectionSource = selectionSource,
		};
	}

	private async Task<WorkspaceResolution?> ResolveSolutionCoreAsync(string requestedDirectory, string solutionPath, WorkspaceSelectionSource selectionSource)
	{
		var solutionDirectory = Path.GetDirectoryName(solutionPath);
		if (string.IsNullOrWhiteSpace(solutionDirectory))
		{
			return null;
		}

		var currentDirectory = Path.GetFullPath(solutionDirectory);
		while (currentDirectory is not null)
		{
			var globalJsonPath = Path.Combine(currentDirectory, "global.json");
			if (File.Exists(globalJsonPath))
			{
				var parsed = await GlobalJsonLocator.ParseGlobalJsonFileForUnoSdkAsync(globalJsonPath, _logger);
				if (!string.IsNullOrWhiteSpace(parsed.sdkPackage) && !string.IsNullOrWhiteSpace(parsed.sdkVersion))
				{
					var resolutionKind = string.Equals(currentDirectory, requestedDirectory, StringComparison.OrdinalIgnoreCase)
						? WorkspaceResolutionKind.CurrentDirectory
						: WorkspaceResolutionKind.AutoDiscovered;

					return new WorkspaceResolution
					{
						RequestedWorkingDirectory = requestedDirectory,
						EffectiveWorkspaceDirectory = currentDirectory,
						SelectedSolutionPath = solutionPath,
						SelectedGlobalJsonPath = globalJsonPath,
						ResolutionKind = resolutionKind,
						SelectionSource = selectionSource,
						CandidateSolutions = SolutionFileFinder.FindSolutionFiles(requestedDirectory),
					};
				}

				break;
			}

			var parent = Directory.GetParent(currentDirectory);
			currentDirectory = parent?.FullName;
		}

		return null;
	}

	private static int GetDirectoryDistance(string workspaceDirectory, string solutionDirectory)
	{
		var distance = 0;
		var current = Path.GetFullPath(solutionDirectory);
		var target = Path.GetFullPath(workspaceDirectory);

		while (!string.Equals(current, target, StringComparison.OrdinalIgnoreCase))
		{
			var parent = Directory.GetParent(current);
			if (parent is null)
			{
				return int.MaxValue;
			}

			distance++;
			current = parent.FullName;
		}

		return distance;
	}

	private sealed record WorkspaceCandidate(
		string SolutionPath,
		string WorkspaceDirectory,
		string GlobalJsonPath,
		int GlobalJsonDistance);
}

internal sealed record WorkspaceResolution
{
	public required string RequestedWorkingDirectory { get; init; }
	public string? EffectiveWorkspaceDirectory { get; init; }
	public string? SelectedSolutionPath { get; init; }
	public string? SelectedGlobalJsonPath { get; init; }
	public required WorkspaceResolutionKind ResolutionKind { get; init; }
	public WorkspaceSelectionSource SelectionSource { get; init; } = WorkspaceSelectionSource.Automatic;
	public IReadOnlyList<string> CandidateSolutions { get; init; } = [];

	public bool IsResolved => !string.IsNullOrWhiteSpace(EffectiveWorkspaceDirectory);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WorkspaceResolutionKind
{
	CurrentDirectory,
	AutoDiscovered,
	Ambiguous,
	NoValidWorkspace,
	NoCandidates,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WorkspaceSelectionSource
{
	Automatic,
	RootsConfirmed,
	UserSelected,
}
