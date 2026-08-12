using System.Threading;
using System.Threading.Tasks;

namespace Uno.HotReload.IO;

/// <summary>
/// Abstracts file editing operations (on disk, via IDE, etc.).
/// </summary>
/// <remarks>
/// LEGACY SEAM — do not build new behavior on this interface. Its per-file granularity cannot
/// express request-level semantics (ordering, batching, the hot-reload trigger), which is what
/// caused the IDE to evaluate EnC on intermediate states (spec 052). New variants must
/// implement or derive <see cref="IFileUpdater"/>/<see cref="FileUpdater"/> instead (see
/// <c>IdeFileUpdater</c>). The only intended remaining implementation is the on-disk editor
/// used by the base <see cref="FileUpdater"/>.
/// </remarks>
public interface IFileEditor
{
	/// <summary>
	/// Edits a file based on the given <see cref="FileEdit"/> specification.
	/// </summary>
	/// <param name="edit">The edit to apply.</param>
	/// <param name="forceSaveOnDisk">If set, overrides the default save-to-disk behavior.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>The result of the edit operation and an optional error message.</returns>
	ValueTask<(FileUpdateResult Result, string? Error)> EditAsync(FileEdit edit, bool? forceSaveOnDisk, CancellationToken ct);
}
