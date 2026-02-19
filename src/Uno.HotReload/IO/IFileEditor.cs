using System.Threading;
using System.Threading.Tasks;

namespace Uno.HotReload.IO;

/// <summary>
/// Abstracts file editing operations (on disk, via IDE, etc.).
/// </summary>
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
