using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Uno.HotReload.Tracking;

/// <summary>
/// Tracks and report hot-reload operations.
/// </summary>
internal interface IHotReloadTracker : IReporter
{
	ValueTask<HotReloadOperation> StartOrContinueHotReload(ImmutableHashSet<string>? files = null);

	ValueTask<HotReloadOperation> StartHotReload(ImmutableHashSet<string>? files);
}
