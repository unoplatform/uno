using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.HotReload.Info;
using Uno.HotReload.Tracking;
using Uno.HotReload.Utils;

namespace Uno.HotReload.IO;

/// <summary>
/// Orchestrates file update requests: edits files, writes hot-reload info, and triggers hot-reload.
/// </summary>
/// <remarks>
/// The edit application and the post-edit completion are overridable so IDE-driven variants can
/// batch all edits — and the hot-reload trigger — into a single IDE interaction (spec 052),
/// while sharing the validation / tracking / buffering pipeline.
/// </remarks>
public class FileUpdater(
	IFileEditor editor,
	BufferGate gate,
	HotReloadTracker tracker,
	HotReloadInfoFile hotReloadInfoFile,
	Func<ValueTask> requestHotReload) : IFileUpdater
{
	public async Task<IUpdateFileResponse> UpdateAsync(IUpdateFileRequest request, CancellationToken ct)
	{
		if (request.Edits.IsDefaultOrEmpty)
		{
			return new FileUpdateResponse(request.RequestId, "No edit to process", []);
		}
		else if (request.Edits.DistinctBy(edit => Path.GetFullPath(edit.FilePath), PathComparer.Comparer).Count() != request.Edits.Length)
		{
			return new FileUpdateResponse(request.RequestId, "Detected multiple updates on the same file", []);
		}

		var hotReload = await tracker.StartHotReload([.. request.Edits.Select(edit => Path.GetFullPath(edit.FilePath))]);
		var results = ImmutableArray<FileEditResult>.Empty;
		try
		{
			using var _ = gate.Acquire(); // Makes sure to batch all file changes in a single solution update

			results = await ApplyEditsAsync(hotReload, request, ct);

			await FinalizeAsync(hotReload, request, results, ct);

			return new FileUpdateResponse(request.RequestId, null, results, hotReload.Id);
		}
		catch (Exception ex)
		{
			await hotReload.Complete(HotReloadOperationResult.InternalError, ex);
			return new FileUpdateResponse(request.RequestId, ex.Message, results, hotReload.Id);
		}
	}

	/// <summary>
	/// Applies the edits of the request and reports the per-edit results, in request order.
	/// Base behavior: apply every edit through the configured <see cref="IFileEditor"/>.
	/// </summary>
	protected virtual async Task<ImmutableArray<FileEditResult>> ApplyEditsAsync(HotReloadOperation hotReload, IUpdateFileRequest request, CancellationToken ct)
		=> [.. await Task.WhenAll(request.Edits.Select(edit => EditFileAsync(edit, request.ForceSaveOnDisk, ct)))];

	/// <summary>
	/// Completes the request processing once the edits have been applied.
	/// Base behavior: persist the hot-reload info file, then forcefully request a hot-reload
	/// (only if the request allows it and at least one edit succeeded).
	/// </summary>
	protected virtual async Task FinalizeAsync(HotReloadOperation hotReload, IUpdateFileRequest request, ImmutableArray<FileEditResult> results, CancellationToken ct)
	{
		// Update the hot-reload info so the application will be able to determine the request has been applied
		await PersistHotReloadInfoAsync(hotReload, request);

		// Forcefully request a hot-reload after the file edits have been applied (only if at least one edit succeeded).
		if (request.IsForceHotReloadDisabled is false && HasAnySuccessfulEdit(results))
		{
			if ((request.ForceHotReloadDelay ?? HotReloadOperation.DefaultAutoRetryIfNoChangesDelay) is { TotalMilliseconds: > 0 } delay)
			{
				await Task.Delay(delay, ct);
			}

			hotReload.EnableAutoRetryIfNoChanges(request.ForceHotReloadAttempts, request.ForceHotReloadDelay);

			// Even if IDE does not support hot-reload manual requests, we still invoke this to report the HR processingFiles state as soon as possible.
			await RequestHotReloadAsync();
		}
	}

	/// <summary>Applies a single edit through the configured <see cref="IFileEditor"/>.</summary>
	protected async Task<FileEditResult> EditFileAsync(FileEdit edit, bool? forceSaveOnDisk, CancellationToken ct)
	{
		try
		{
			if (edit.FilePath is null or { Length: 0 })
			{
				return new(edit.FilePath!, FileUpdateResult.BadRequest, "Invalid request (file path is empty)");
			}

			var (result, error) = await editor.EditAsync(edit, forceSaveOnDisk, ct);
			return new(edit.FilePath, result, error);
		}
		catch (Exception ex)
		{
			return new(edit.FilePath, FileUpdateResult.Failed, ex.Message);
		}
	}

	/// <summary>Persists the hot-reload info file for the given operation.</summary>
	protected ValueTask PersistHotReloadInfoAsync(HotReloadOperation hotReload, IUpdateFileRequest request)
		=> hotReloadInfoFile.SetAsync(hotReload, request);

	/// <summary>Requests a hot-reload through the configured callback.</summary>
	protected ValueTask RequestHotReloadAsync()
		=> requestHotReload();

	/// <summary>Indicates whether at least one edit succeeded.</summary>
	protected static bool HasAnySuccessfulEdit(ImmutableArray<FileEditResult> results)
		=> results.Any(result => (int)result.Result < 300);

	private sealed record FileUpdateResponse(
		string RequestId,
		string? GlobalError,
		ImmutableArray<FileEditResult> Results,
		long? HotReloadCorrelationId = null) : IUpdateFileResponse;
}
