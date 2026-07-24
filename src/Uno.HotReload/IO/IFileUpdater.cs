using System.Threading;
using System.Threading.Tasks;

namespace Uno.HotReload.IO;

/// <summary>
/// Contract of the file-update orchestrator consumed by hot-reload server processors.
/// </summary>
/// <remarks>
/// Extracted from <see cref="FileUpdater"/> so behavior can be composed by decoration
/// (e.g. <see cref="WorkspaceGatedFileUpdater"/> queueing requests until the hot-reload
/// workspace has captured its baseline).
/// </remarks>
public interface IFileUpdater
{
	/// <summary>
	/// Applies the given file-update request and reports the per-edit results.
	/// </summary>
	Task<IUpdateFileResponse> UpdateAsync(IUpdateFileRequest request, CancellationToken ct);
}
