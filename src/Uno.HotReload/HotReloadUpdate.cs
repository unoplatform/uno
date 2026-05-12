using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Uno.HotReload.Diffing;

namespace Uno.HotReload;

/// <summary>
/// Payload handed to <see cref="IHotReloadHandler.SendAsync"/> after a
/// successful hot-reload cycle. Carries every piece of state the manager has
/// at the point where deltas are ready to be shipped to consumers.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="SolutionUpdate"/> property carries the *full* result returned
/// by <see cref="ISolutionUpdater.UpdateAsync"/> — implementations that return
/// a richer subtype (e.g. one carrying resolved NuGet packages) keep that
/// information visible to the handler without growing the manager's surface.
/// Handlers downcast via pattern matching.
/// </para>
/// </remarks>
public sealed record HotReloadUpdate(
	ImmutableHashSet<string> Files,
	ChangeSet ChangeSet,
	SolutionUpdateResult SolutionUpdate,
	ImmutableArray<Diagnostic> Diagnostics,
	ImmutableArray<Update> Deltas);
