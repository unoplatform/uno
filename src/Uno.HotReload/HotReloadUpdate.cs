using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Uno.HotReload.Diffing;

namespace Uno.HotReload;

/// <summary>
/// Payload handed to <see cref="IHotReloadHandler.OnHotReloadAsync"/> on every terminal
/// hot-reload cycle outcome. Carries every piece of state the manager has at the point the
/// outcome is decided. <see cref="Deltas"/> is non-empty only on a <c>Success</c> cycle;
/// <see cref="SolutionUpdate"/> is populated on every path reached after the solution update.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="SolutionUpdate"/> property carries the *full* result returned
/// by <see cref="ISolutionUpdater.UpdateAsync"/> — implementations that return
/// a richer subtype (e.g. one carrying resolved NuGet packages) keep that
/// information visible to the handler without growing the manager's surface.
/// Handlers downcast via pattern matching.
/// </para>
/// <para>
/// <see cref="PinnedReferences"/> lists the metadata references the emit pinned back to their
/// session-baseline identity (see <see cref="PinnedReference"/>): <see cref="Deltas"/> bind the
/// baseline files, NOT the conflicting ones listed there. A handler staging assembly files
/// should skip any file whose path matches a <see cref="PinnedReference.ConflictingPath"/> —
/// writing it over the baseline file would diverge the on-disk application from what the
/// deltas bind against.
/// </para>
/// </remarks>
public sealed record HotReloadUpdate(
	ImmutableHashSet<string> Files,
	ChangeSet ChangeSet,
	SolutionUpdateResult SolutionUpdate,
	ImmutableArray<Diagnostic> Diagnostics,
	ImmutableArray<Update> Deltas,
	ImmutableArray<PinnedReference> PinnedReferences);
