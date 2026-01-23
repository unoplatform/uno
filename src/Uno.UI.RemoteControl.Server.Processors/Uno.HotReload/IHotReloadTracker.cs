using System.Collections.Immutable;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.Host.HotReload;

namespace Uno.HotReload;

/// <summary>
/// Tracks and report hot-reload operations.
/// </summary>
internal interface IHotReloadTracker : IReporter
{
	ValueTask<HotReloadTracker.HotReloadServerOperation> StartOrContinueHotReload(ImmutableHashSet<string>? files = null);

	ValueTask<HotReloadTracker.HotReloadServerOperation> StartHotReload(ImmutableHashSet<string>? files);
}
