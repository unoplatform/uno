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
	/// returns the metadata deltas, the diagnostics produced by the emit, and the
	/// names of the projects the engine reports as un-updatable — a non-empty set
	/// means the application must be rebuilt or restarted before it matches its
	/// sources again, and no delta can bring it back in line.
	/// </summary>
	Task<(ImmutableArray<Update> updates, ImmutableArray<Diagnostic> diagnostics, ImmutableArray<string> projectsRequiringRebuild)> EmitSolutionUpdateAsync(Solution solution, CancellationToken cancellationToken);

	/// <summary>
	/// Ends the underlying edit-and-continue session.
	/// </summary>
	void EndSession();
}
