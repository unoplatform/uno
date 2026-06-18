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
	/// Compiles <paramref name="solution"/> against the session baseline and
	/// returns the metadata deltas plus the diagnostics produced by the emit.
	/// </summary>
	Task<(ImmutableArray<Update> updates, ImmutableArray<Diagnostic> diagnostics)> EmitSolutionUpdateAsync(Solution solution, CancellationToken cancellationToken);

	/// <summary>
	/// Ends the underlying edit-and-continue session.
	/// </summary>
	void EndSession();
}
