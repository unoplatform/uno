using System.Threading;
using System.Threading.Tasks;

namespace Uno.HotReload;

/// <summary>
/// Consumer end of the hot-reload pipeline. Receives the full
/// <see cref="HotReloadUpdate"/> after a successful cycle and is responsible
/// for shipping the deltas (and any subtype-specific side-effects) downstream.
/// </summary>
/// <remarks>
/// Implementations may chain (decorator pattern): a wrapping handler can
/// inspect the update, perform side-effects (e.g. stage resolved NuGet
/// packages onto disk), then delegate to an inner handler for the actual
/// delta transport.
/// </remarks>
public interface IHotReloadHandler
{
	/// <summary>
	/// Process the post-validated <paramref name="update"/>. Called by
	/// <see cref="HotReloadManager"/> on the success path only — handlers
	/// never see <c>NoChanges</c>, <c>RudeEdit</c>, or <c>Failed</c> cycles.
	/// </summary>
	ValueTask SendAsync(HotReloadUpdate update, CancellationToken ct);
}
