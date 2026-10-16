#nullable enable

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.HotReload;

/// <summary>
/// Outcome of one Edit-and-Continue emit, mirroring Roslyn's <c>ModuleUpdateStatus</c>.
/// </summary>
internal enum HotReloadEmitStatus
{
	/// <summary>Nothing differed from the session baseline, so there was nothing to emit.</summary>
	NoChanges,

	/// <summary>The emit ran. On Roslyn 5.x this does not mean every project could be updated: see
	/// <see cref="HotReloadEmitResult.ProjectsToRebuild"/>.</summary>
	Ready,

	/// <summary>The emit itself could not be produced.</summary>
	Blocked,

	/// <summary>An edit was not applicable and the application has to be restarted (Roslyn 4.x).</summary>
	RestartRequired,
}

/// <summary>
/// What Roslyn's Edit-and-Continue engine reports about one emit.
/// </summary>
internal sealed record HotReloadEmitResult
{
	public static HotReloadEmitResult Empty { get; } = new();

	public HotReloadEmitStatus Status { get; init; } = HotReloadEmitStatus.NoChanges;

	/// <summary>
	/// The metadata deltas to apply. Empty whenever the emit carries an error or names a project that
	/// cannot be updated: an emit that is not fully applicable hands out nothing.
	/// </summary>
	public ImmutableArray<Update> Deltas { get; init; } = ImmutableArray<Update>.Empty;

	/// <summary>All diagnostics produced by the emit, rude edits included.</summary>
	public ImmutableArray<Diagnostic> Diagnostics { get; init; } = ImmutableArray<Diagnostic>.Empty;

	/// <summary>Names of the projects that only a rebuild can bring back in line with their sources.</summary>
	public ImmutableArray<string> ProjectsToRebuild { get; init; } = ImmutableArray<string>.Empty;

	/// <summary>Names of the projects whose running process has to be restarted.</summary>
	public ImmutableArray<string> ProjectsToRestart { get; init; } = ImmutableArray<string>.Empty;

	/// <summary>
	/// Whether the application can no longer be brought in line with its sources by hot reload alone.
	/// Roslyn 5.x says so by naming projects while staying <see cref="HotReloadEmitStatus.Ready"/>;
	/// 4.x names none and answers <see cref="HotReloadEmitStatus.RestartRequired"/>.
	/// </summary>
	public bool RequiresRebuildOrRestart
		=> !ProjectsToRebuild.IsEmpty
			|| !ProjectsToRestart.IsEmpty
			|| Status is HotReloadEmitStatus.RestartRequired;

	/// <summary>The projects behind <see cref="RequiresRebuildOrRestart"/>, deduplicated.</summary>
	public ImmutableArray<string> ProjectsRequiringRebuildOrRestart
		=> ProjectsToRebuild.Concat(ProjectsToRestart).Distinct().ToImmutableArray();
}
