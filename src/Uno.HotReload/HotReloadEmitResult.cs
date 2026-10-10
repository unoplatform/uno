#nullable enable

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.HotReload;

/// <summary>
/// Outcome of one Edit-and-Continue emit, mirroring Roslyn's <c>ModuleUpdateStatus</c>.
/// </summary>
public enum HotReloadEmitStatus
{
	/// <summary>A status this version of the shim does not know about — treat as inconclusive.</summary>
	Unknown,

	/// <summary>Nothing differed from the session baseline, so there was nothing to emit.</summary>
	NoChanges,

	/// <summary>Deltas were produced. Note this says nothing about whether EVERY project could be
	/// updated: on the Roslyn 5.x line a project that cannot be is reported through
	/// <see cref="HotReloadEmitResult.ProjectsToRebuild"/> while the status stays
	/// <see cref="Ready"/> — the change that
	/// <see cref="Microsoft.WatchHotReloadService"/>'s commit policy exists to handle.</summary>
	Ready,

	/// <summary>The emit itself could not be produced.</summary>
	Blocked,

	/// <summary>An edit was not applicable and the application has to be restarted. Roslyn 4.x only:
	/// the 5.x line reports the same situation as <see cref="Ready"/> plus a non-empty
	/// <see cref="HotReloadEmitResult.ProjectsToRebuild"/>.</summary>
	RestartRequired,
}

/// <summary>
/// Everything Roslyn's Edit-and-Continue engine reports about one emit.
/// </summary>
/// <remarks>
/// <para>Shaped after Roslyn's own <c>ExternalAccess.HotReload.HotReloadService.Updates</c> — the API
/// that replaces the <c>UnitTesting</c> twin this shim has to use until it ships in a released
/// package. Keeping the same information means adopting it later is a mapping change, not a redesign
/// of every consumer.</para>
/// <para>Some members are only populated on one Roslyn line, because the engine only reports them
/// there. Each says which; none of them ever lies by omission — a member that the running Roslyn does
/// not report stays empty rather than being guessed at.</para>
/// </remarks>
public sealed record HotReloadEmitResult
{
	public static HotReloadEmitResult Empty { get; } = new();

	/// <summary>Status of the emit.</summary>
	public HotReloadEmitStatus Status { get; init; } = HotReloadEmitStatus.NoChanges;

	/// <summary>
	/// The metadata deltas to apply to the running application. Empty whenever
	/// <see cref="Diagnostics"/> carries an error: a failed emit hands out nothing.
	/// </summary>
	public ImmutableArray<Update> Deltas { get; init; } = ImmutableArray<Update>.Empty;

	/// <summary>All diagnostics produced by the emit, rude edits included.</summary>
	public ImmutableArray<Diagnostic> Diagnostics { get; init; } = ImmutableArray<Diagnostic>.Empty;

	/// <summary>
	/// The diagnostics that stay true until the code changes again — as opposed to
	/// <see cref="TransientDiagnostics"/>, which the engine reports once. Roslyn 5.x only; empty on
	/// 4.x, where only the combined <see cref="Diagnostics"/> is available.
	/// </summary>
	public ImmutableArray<Diagnostic> PersistentDiagnostics { get; init; } = ImmutableArray<Diagnostic>.Empty;

	/// <summary>
	/// The diagnostics the engine reports only for this emit. Roslyn 5.x only; empty on 4.x.
	/// </summary>
	public ImmutableArray<Diagnostic> TransientDiagnostics { get; init; } = ImmutableArray<Diagnostic>.Empty;

	/// <summary>The syntax error that stopped the emit before any analysis, if any.</summary>
	public Diagnostic? SyntaxError { get; init; }

	/// <summary>
	/// Names of the projects that cannot be updated in place: no later delta will bring them back in
	/// line with their sources, only a rebuild will.
	/// </summary>
	public ImmutableArray<string> ProjectsToRebuild { get; init; } = ImmutableArray<string>.Empty;

	/// <summary>
	/// Names of the projects whose running process has to be restarted.
	/// </summary>
	public ImmutableArray<string> ProjectsToRestart { get; init; } = ImmutableArray<string>.Empty;

	/// <summary>
	/// Names of the projects whose outputs have to be redeployed. Roslyn 5.x only; empty on 4.x,
	/// which does not report it.
	/// </summary>
	public ImmutableArray<string> ProjectsToRedeploy { get; init; } = ImmutableArray<string>.Empty;

	/// <summary>
	/// Whether the application can no longer be brought in line with its sources by hot reload alone.
	/// </summary>
	/// <remarks>
	/// The two Roslyn lines say this differently and both are honoured here: 5.x keeps the status at
	/// <see cref="HotReloadEmitStatus.Ready"/> and names the projects, while 4.x names none and
	/// answers <see cref="HotReloadEmitStatus.RestartRequired"/>. Consumers ask this rather than
	/// inspecting either.
	/// </remarks>
	public bool RequiresRebuildOrRestart
		=> !ProjectsToRebuild.IsEmpty
			|| !ProjectsToRestart.IsEmpty
			|| Status is HotReloadEmitStatus.RestartRequired;

	/// <summary>
	/// The projects behind <see cref="RequiresRebuildOrRestart"/>, deduplicated — a project can be
	/// reported as needing both.
	/// </summary>
	public ImmutableArray<string> ProjectsRequiringRebuildOrRestart
		=> ProjectsToRebuild.Concat(ProjectsToRestart).Distinct().ToImmutableArray();
}
