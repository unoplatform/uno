using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.HotReload.Info;
using Uno.HotReload.IO;
using Uno.HotReload.Tracking;
using Uno.HotReload.Utils;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using IdeFileEdit = Uno.UI.RemoteControl.Messaging.IdeChannel.HotReload.FileEdit;

namespace Uno.UI.RemoteControl.Host.HotReload;

/// <summary>
/// <see cref="FileUpdater"/> variant for IDE-driven hot reload (Visual Studio): all content
/// edits of a request — and the hot-reload trigger — are forwarded to the IDE as a single
/// <see cref="UpdateFileRequestIdeMessage"/>, so the IDE can apply them in order and wait for its
/// workspace to be able to compile the change-set before triggering EnC (spec 052).
/// Deletes are applied on disk by the base editor, like the legacy <see cref="IDEFileEditor"/> did.
/// </summary>
internal sealed class IdeFileUpdater(
	IFileEditor onDiskEditor,
	BufferGate gate,
	HotReloadTracker tracker,
	HotReloadInfoFile hotReloadInfoFile,
	Func<ValueTask> requestHotReload,
	Func<UpdateFileRequestIdeMessage, ValueTask<(bool IsSuccess, string? Error)>> sendToIde,
	Func<long> nextIdeCorrelationId)
	: FileUpdater(onDiskEditor, gate, tracker, hotReloadInfoFile, requestHotReload)
{
	protected override async Task<ImmutableArray<FileEditResult>> ApplyEditsAsync(HotReloadOperation hotReload, IUpdateFileRequest request, CancellationToken ct)
	{
		// The batch below carries the hot-reload trigger: the info file must be persisted
		// before the IDE can possibly act on that trigger (spec 052, R6).
		await PersistHotReloadInfoAsync(hotReload, request);

		// Non-nullable by construction: the delete loop below fills every NewText==null slot,
		// the write fan-out fills every other one — each index is covered exactly once.
		var results = new FileEditResult[request.Edits.Length];

		// Deletes are not forwarded to the IDE — apply them on disk through the base editor.
		for (var i = 0; i < request.Edits.Length; i++)
		{
			if (request.Edits[i].NewText is null)
			{
				results[i] = await EditFileAsync(request.Edits[i], request.ForceSaveOnDisk, ct);
			}
		}

		var writes = request.Edits
			.Select((edit, index) => (edit, index))
			.Where(x => x.edit.NewText is not null)
			.ToImmutableArray();

		if (writes.Length > 0)
		{
			var message = new UpdateFileRequestIdeMessage(
				nextIdeCorrelationId(),
				request.RequestId,
				[.. writes.Select(x => new IdeFileEdit(x.edit.FilePath, x.edit.OldText, x.edit.NewText, x.edit.IsCreateDeleteAllowed))],
				request.ForceSaveOnDisk,
				request.IsForceHotReloadDisabled,
				request.ForceHotReloadDelay,
				request.ForceHotReloadAttempts);

			if (this.Log().IsEnabled(LogLevel.Information))
			{
				this.Log().LogInformation($"Forwarding batched update #{message.CorrelationId} to the IDE: {writes.Length} edit(s) of request {request.RequestId}.");
			}

			var (isSuccess, error) = await sendToIde(message);

			if (!isSuccess && this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning($"IDE failed to apply batched update #{message.CorrelationId}: {error ?? "(no error detail)"}.");
			}

			foreach (var (edit, index) in writes)
			{
				results[index] = new(edit.FilePath, isSuccess ? FileUpdateResult.Success : FileUpdateResult.Failed, error);
			}
		}

		return [.. results];
	}

	protected override Task FinalizeAsync(HotReloadOperation hotReload, IUpdateFileRequest request, ImmutableArray<FileEditResult> results, CancellationToken ct)
	{
		// The info file is already persisted (ApplyEditsAsync). For write batches the trigger
		// traveled with the UpdateFileRequestIdeMessage (no blind pre-delay); for delete-only
		// batches no IDE message is sent and the NoChanges auto-retry below provides the
		// trigger. Either way the retry must stay armed: retries go through the standalone
		// ForceHotReloadIdeMessage and never re-send file contents.
		if (request.IsForceHotReloadDisabled is false && HasAnySuccessfulEdit(results))
		{
			hotReload.EnableAutoRetryIfNoChanges(request.ForceHotReloadAttempts, request.ForceHotReloadDelay);
		}

		return Task.CompletedTask;
	}
}
