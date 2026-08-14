using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Uno.HotReload.Microsoft;

/// <summary>
/// Abstraction over the Roslyn watch hot-reload service consumed by
/// <see cref="HotReloadManager"/>. Exists so the manager's orchestration
/// (operation lifecycle, merge/completion ordering) can be unit-tested with a
/// stub emitter; production code always goes through
/// <see cref="WatchHotReloadService"/>.
/// </summary>
internal interface IWatchHotReloadService
{
	/// <summary>
	/// Compiles <paramref name="solution"/> against the session baseline and returns everything the
	/// Edit-and-Continue engine reports about the emit.
	/// </summary>
	Task<HotReloadEmitResult> EmitSolutionUpdateAsync(Solution solution, CancellationToken cancellationToken);

	/// <summary>
	/// Members of Roslyn's Edit-and-Continue engine that are present but whose shape could not be
	/// read, so the corresponding <see cref="HotReloadEmitResult"/> values will always be empty.
	/// Reported once, at session start: a member the running Roslyn line simply does not have is NOT
	/// listed here (that is expected and documented on the result), only one that changed shape.
	/// </summary>
	ImmutableArray<string> EngineShapeWarnings { get; }

	/// <summary>
	/// Ends the underlying edit-and-continue session.
	/// </summary>
	void EndSession();
}
