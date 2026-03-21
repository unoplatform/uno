using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Uno.UI.DevServer.Cli.Helpers;

internal sealed class WorkspaceResolver(ILogger<WorkspaceResolver> logger, ISolutionFileFinder? solutionFileFinder = null) : IWorkspaceResolver
{
	private readonly ILogger<WorkspaceResolver> _logger = logger;
	private readonly ISolutionFileFinder _solutionFileFinder = solutionFileFinder ?? new FileSystemSolutionFileFinder();

	public async Task<WorkspaceResolution> ResolveAsync(string requestedDirectory)
	{
		var normalizedRequestedDirectory = Path.GetFullPath(requestedDirectory);
		var solutionFiles = _solutionFileFinder.FindSolutionFiles(normalizedRequestedDirectory);
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
		// Per-resolution cache: every new resolve call rebuilds this dictionary.
		var globalJsonCache = new Dictionary<string, (string? sdkPackage, string? sdkVersion)>(StringComparer.Ordinal);

		foreach (var solutionPath in solutionFiles.OrderBy(PathComparison.Normalize, StringComparer.Ordinal))
		{
			var solutionDirectory = Path.GetDirectoryName(solutionPath);
			if (string.IsNullOrWhiteSpace(solutionDirectory))
			{
				continue;
			}

			string? currentDirectory = Path.GetFullPath(solutionDirectory);
			while (currentDirectory is not null)
			{
				var globalJsonPath = Path.Combine(currentDirectory, "global.json");
				if (File.Exists(globalJsonPath))
				{
					var parsed = await GetCachedGlobalJsonResultAsync(globalJsonPath, globalJsonCache);
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
			.OrderBy(candidate => PathComparison.Normalize(candidate.SolutionPath), StringComparer.Ordinal)
			.ToList();

		if (bestCandidates.Count > 1)
		{
			return new WorkspaceResolution
			{
				RequestedWorkingDirectory = normalizedRequestedDirectory,
				ResolutionKind = WorkspaceResolutionKind.Ambiguous,
				CandidateSolutions = [.. solutionFiles],
			};
		}

		var selected = bestCandidates[0];
		var resolutionKind = PathComparison.PathsEqual(selected.WorkspaceDirectory, normalizedRequestedDirectory)
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
			CandidateSolutions = [.. solutionFiles],
		};
	}

	public async Task<WorkspaceResolution> ResolveExplicitWorkspaceAsync(string requestedDirectory)
	{
		var normalizedRequestedDirectory = Path.GetFullPath(requestedDirectory);
		var resolved = await ResolveAsync(normalizedRequestedDirectory);
		if (resolved.IsResolved || resolved.ResolutionKind != WorkspaceResolutionKind.NoCandidates)
		{
			return resolved with { SelectionSource = WorkspaceSelectionSource.UserSelected };
		}

		var globalJsonPath = Path.Combine(normalizedRequestedDirectory, "global.json");
		if (!File.Exists(globalJsonPath))
		{
			return resolved;
		}

		var parsed = await GlobalJsonLocator.ParseGlobalJsonFileForUnoSdkAsync(globalJsonPath, _logger);
		if (string.IsNullOrWhiteSpace(parsed.sdkPackage) || string.IsNullOrWhiteSpace(parsed.sdkVersion))
		{
			return new WorkspaceResolution
			{
				RequestedWorkingDirectory = normalizedRequestedDirectory,
				SelectedGlobalJsonPath = globalJsonPath,
				ResolutionKind = WorkspaceResolutionKind.NoValidWorkspace,
				SelectionSource = WorkspaceSelectionSource.UserSelected,
				CandidateSolutions = [],
			};
		}

		return new WorkspaceResolution
		{
			RequestedWorkingDirectory = normalizedRequestedDirectory,
			EffectiveWorkspaceDirectory = normalizedRequestedDirectory,
			SelectedGlobalJsonPath = globalJsonPath,
			ResolutionKind = WorkspaceResolutionKind.CurrentDirectory,
			SelectionSource = WorkspaceSelectionSource.UserSelected,
			CandidateSolutions = [],
		};
	}

	public async Task<WorkspaceResolution> ResolveSolutionAsync(string requestedDirectory, string solutionPath, WorkspaceSelectionSource selectionSource = WorkspaceSelectionSource.UserSelected)
	{
		var normalizedRequestedDirectory = Path.GetFullPath(requestedDirectory);
		var candidateSolutions = _solutionFileFinder.FindSolutionFiles(normalizedRequestedDirectory);
		return await ResolveSolutionAsync(normalizedRequestedDirectory, solutionPath, candidateSolutions, selectionSource);
	}

	internal async Task<WorkspaceResolution> ResolveSolutionAsync(
		string requestedDirectory,
		string solutionPath,
		IReadOnlyList<string> candidateSolutions,
		WorkspaceSelectionSource selectionSource = WorkspaceSelectionSource.UserSelected)
	{
		var normalizedRequestedDirectory = Path.GetFullPath(requestedDirectory);
		var normalizedSolutionPath = Path.GetFullPath(solutionPath);
		var globalJsonCache = new Dictionary<string, (string? sdkPackage, string? sdkVersion)>(StringComparer.Ordinal);

		if (!File.Exists(normalizedSolutionPath)
			|| !candidateSolutions.Any(candidate => PathComparison.PathsEqual(candidate, normalizedSolutionPath)))
		{
			return new WorkspaceResolution
			{
				RequestedWorkingDirectory = normalizedRequestedDirectory,
				ResolutionKind = candidateSolutions.Count == 0
					? WorkspaceResolutionKind.NoCandidates
					: WorkspaceResolutionKind.NoValidWorkspace,
				CandidateSolutions = candidateSolutions,
				SelectionSource = selectionSource,
			};
		}

		var resolved = await ResolveSolutionCoreAsync(
			normalizedRequestedDirectory,
			normalizedSolutionPath,
			candidateSolutions,
			selectionSource,
			globalJsonCache);
		return resolved ?? new WorkspaceResolution
		{
			RequestedWorkingDirectory = normalizedRequestedDirectory,
			ResolutionKind = WorkspaceResolutionKind.NoValidWorkspace,
			CandidateSolutions = candidateSolutions,
			SelectionSource = selectionSource,
		};
	}

	private async Task<WorkspaceResolution?> ResolveSolutionCoreAsync(
		string requestedDirectory,
		string solutionPath,
		IReadOnlyList<string> candidateSolutions,
		WorkspaceSelectionSource selectionSource,
		Dictionary<string, (string? sdkPackage, string? sdkVersion)> globalJsonCache)
	{
		var solutionDirectory = Path.GetDirectoryName(solutionPath);
		if (string.IsNullOrWhiteSpace(solutionDirectory))
		{
			return null;
		}

		string? currentDirectory = Path.GetFullPath(solutionDirectory);
		while (currentDirectory is not null)
		{
			var globalJsonPath = Path.Combine(currentDirectory, "global.json");
			if (File.Exists(globalJsonPath))
			{
				var parsed = await GetCachedGlobalJsonResultAsync(globalJsonPath, globalJsonCache);
				if (!string.IsNullOrWhiteSpace(parsed.sdkPackage) && !string.IsNullOrWhiteSpace(parsed.sdkVersion))
				{
					var resolutionKind = PathComparison.PathsEqual(currentDirectory, requestedDirectory)
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
						CandidateSolutions = [.. candidateSolutions],
					};
				}

				break;
			}

			var parent = Directory.GetParent(currentDirectory);
			currentDirectory = parent?.FullName;
		}

		return null;
	}

	private async Task<(string? sdkPackage, string? sdkVersion)> GetCachedGlobalJsonResultAsync(
		string globalJsonPath,
		Dictionary<string, (string? sdkPackage, string? sdkVersion)> globalJsonCache)
	{
		var normalizedGlobalJsonPath = PathComparison.Normalize(globalJsonPath);
		if (globalJsonCache.TryGetValue(normalizedGlobalJsonPath, out var cached))
		{
			return cached;
		}

		var parsed = await GlobalJsonLocator.ParseGlobalJsonFileForUnoSdkAsync(globalJsonPath, _logger);
		var result = (parsed.sdkPackage, parsed.sdkVersion);
		globalJsonCache[normalizedGlobalJsonPath] = result;
		return result;
	}

	private static int GetDirectoryDistance(string workspaceDirectory, string solutionDirectory)
	{
		var distance = 0;
		var current = Path.GetFullPath(solutionDirectory);
		var target = Path.GetFullPath(workspaceDirectory);

		while (!PathComparison.PathsEqual(current, target))
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
