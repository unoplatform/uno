using System.Collections.Immutable;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.Host.HotReload;
using Uno.UI.RemoteControl.Host.HotReload.MetadataUpdates;

namespace Uno.HotReload;

/// <summary>
/// Tracks and report hot-reload operations.
/// </summary>
internal interface IHotReloadTracker : IReporter
{
	ValueTask<ServerHotReloadProcessor.HotReloadServerOperation> StartOrContinueHotReload(ImmutableHashSet<string>? files = null);

	ValueTask<ServerHotReloadProcessor.HotReloadServerOperation> StartHotReload(ImmutableHashSet<string>? files);
}
