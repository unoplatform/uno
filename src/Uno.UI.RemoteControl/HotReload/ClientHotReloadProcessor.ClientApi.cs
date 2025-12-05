#if HAS_UNO_WINUI

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.RemoteControl.HotReload.Messages;
using Windows.UI.Notifications;

namespace Uno.UI.RemoteControl.HotReload;

public partial class ClientHotReloadProcessor
{
	private static int _reqId;

	/// <summary>
	/// Result details of a file update
	/// </summary>
	/// <param name="FilesUpdated">Number of files that are known to have been updated on server-side.</param>
	/// <param name="ApplicationUpdated">Indicates if the change had an impact on the compilation of the application (might be a success-full build or an error).</param>
	/// <param name="Error">Gets the error if any happened during the update.</param>
	public record struct UpdateResult(
		int FilesUpdated,
		bool? ApplicationUpdated,
		Exception? Error = null)
	{
		/// <summary>
		/// Indicates if at leats one file is known to have been updated on server-side.
		/// </summary>
		public bool FileUpdated => FilesUpdated > 0; // For backward compat purposes!
	}

	/// <summary>
	/// Request details of a file update
	/// </summary>
	/// <param name="FilePath">Path of the file to update, relative to the solution root dir.</param>
	/// <param name="OldText">Current text to replace in the file.</param>
	/// <param name="NewText">Replacement text.</param>
	/// <param name="WaitForHotReload">Indicates if we should also wait for the change to be applied in the application before completing the resulting task.</param>
	public record struct UpdateRequest(
		ImmutableArray<FileEdit> Edits,
		bool WaitForHotReload = true)
	{
		/// <summary>
		/// Request details of a single file update
		/// </summary>
		/// <param name="FilePath">Path of the file to update, relative to the solution root dir.</param>
		/// <param name="OldText">Current text to replace in the file.</param>
		/// <param name="NewText">Replacement text.</param>
		/// <param name="WaitForHotReload">Indicates if we should also wait for the change to be applied in the application before completing the resulting task.</param>
		public UpdateRequest(
			string FilePath,
			string? OldText,
			string? NewText,
			bool WaitForHotReload = true)
			: this([new(FilePath, OldText, NewText)], WaitForHotReload)
		{
		}

		/// <summary>
		/// Indicates if the file should be saved to disk.
		/// </summary>
		/// <remarks>
		/// Some IDE supports the ability to update the file in memory without saving it to disk.
		/// Null means that the default behavior of the IDE should be used.
		/// </remarks>
		public bool? ForceSaveToDisk { get; init; }

		/// <summary>
		/// The max delay to wait for the server to process a file update request.
		/// </summary>
		/// <remarks>This includes the time to send the request to the server, the server to process it and send a reply.</remarks>
		public TimeSpan ServerUpdateTimeout { get; set; } = TimeSpan.FromSeconds(10);

		/// <summary>
		/// The max delay to wait for the server to process a hot-reload and send completion messages after a file has been updated.
		/// </summary>
		/// <remarks>
		/// Once a file has been updated on the server, this includes the time for the IDE/dev-server to detect the file update,
		/// roslyn to generate delta (or error), send it to the app, and then the dev-server to send notification of HR completion.
		/// </remarks>
		public TimeSpan ServerHotReloadTimeout { get; set; } = TimeSpan.FromSeconds(10);

		/// <summary>
		/// The max delay to wait for the local application to process a hot-reload delta.
		/// </summary>
		/// <remarks>This includes the time to apply the delta locally and then to run all local handlers.</remarks>
		public TimeSpan LocalHotReloadTimeout { get; set; } = TimeSpan.FromSeconds(5);

		/// <summary>
		/// When <see cref="WaitForHotReload"/> the delay to wait before retrying a hot-reload in Visual Studio if no changes are detected.
		/// </summary>
		public TimeSpan? HotReloadNoChangesRetryDelay { get; set; }

		/// <summary>
		/// When <see cref="WaitForHotReload"/> the number of times to retry the hot reload in Visual Studio if no changes are detected.
		/// </summary>
		public int? HotReloadNoChangesRetryAttempts { get; set; }

		public UpdateRequest WithExtendedTimeouts(float? factor = null)
		{
			factor ??= Debugger.IsAttached ? 10 : 30;

			return this with
			{
				ServerUpdateTimeout = ServerUpdateTimeout * factor.Value,
				ServerHotReloadTimeout = ServerHotReloadTimeout * factor.Value,
				LocalHotReloadTimeout = LocalHotReloadTimeout * factor.Value
			};
		}

		public UpdateRequest Undo()
			=> this with { Edits = [.. Edits.Select(edit => edit with { OldText = edit.NewText, NewText = edit.OldText })] };

		public UpdateRequest Undo(bool waitForHotReload)
			=> this with { Edits = [.. Edits.Select(edit => edit with { OldText = edit.NewText, NewText = edit.OldText })], WaitForHotReload = waitForHotReload };
	}

	public Task UpdateFileAsync(string filePath, string? oldText, string newText, bool waitForHotReload, CancellationToken ct)
		=> UpdateFileAsync(new UpdateRequest(filePath, oldText, newText, waitForHotReload), ct);

	public Task UpdateFileAsync(string filePath, string? oldText, string newText, bool waitForHotReload, bool forceSaveToDisk, CancellationToken ct)
		=> UpdateFileAsync(new UpdateRequest(filePath, oldText, newText, waitForHotReload) { ForceSaveToDisk = forceSaveToDisk }, ct);

	public async Task UpdateFileAsync(UpdateRequest req, CancellationToken ct)
	{
		if (await TryUpdateFileAsync(req, ct) is { Error: { } error })
		{
			ExceptionDispatchInfo.Throw(error);
		}
	}

	public Task TryUpdateFileAsync(string filePath, string? oldText, string newText, bool waitForHotReload, CancellationToken ct)
		=> TryUpdateFileAsync(new UpdateRequest(filePath, oldText, newText, waitForHotReload), ct);

	public Task<UpdateResult> TryUpdateFileAsync(UpdateRequest req, CancellationToken ct)
		=> TryUpdateFilesAsync(req, ct);

	public async Task<UpdateResult> TryUpdateFilesAsync(UpdateRequest req, CancellationToken ct)
	{
		var result = default(UpdateResult);
		try
		{
			if (req.Edits.IsDefaultOrEmpty || req.Edits.Any(edit => string.IsNullOrWhiteSpace(edit.FilePath)))
			{
				return result with { Error = new ArgumentOutOfRangeException(nameof(req.Edits), "Invalid edits (no edits or edit.FilePath is null or empty).") };
			}

			var log = this.Log();
			var trace = log.IsTraceEnabled() ? log : default;
			var debug = log.IsDebugEnabled() ? log : default;
			var tag = $"[{Interlocked.Increment(ref _reqId):D2}-{Path.GetFileName(req.Edits.First().FilePath)}]";

			debug?.Debug($"{tag} Updating {req.Edits.Length} file(s).");
			if (debug?.IsTraceEnabled() ?? false)
			{
				foreach (var edit in req.Edits)
				{
					debug?.Debug($"{tag} '{edit.FilePath}' (from: {edit.OldText?[..100]} | to: {edit.NewText?[..100]}).");
				}
			}

			// As the local HR is not really ID trackable (trigger by VS without any ID), we capture the current ID here to make sure that if HR completes locally before we get info from the server, we won't miss it.
			var currentLocalHrId = GetCurrentLocalHotReloadId();

			var request = new UpdateFileRequest
			{
				Edits = req.Edits,
				ForceHotReloadDelay = req.HotReloadNoChangesRetryDelay,
				ForceHotReloadAttempts = req.HotReloadNoChangesRetryAttempts,
				ForceSaveOnDisk = req.ForceSaveToDisk,
			};
			var response = await UpdateFileCoreAsync(request, req.ServerUpdateTimeout, ct);

			if (response.Results.All(static r => r.Result is FileUpdateResult.NoChanges))
			{
				debug?.Debug($"{tag} Changes requested has no effect on server, completing.");
				return result;
			}

			// Collect errors
			var fileErrors = response
				.Results
				.Where(edit => ((int)edit.Result) > 300)
				.Select(edit => new InvalidOperationException($"Failed to update file '{edit.FilePath}': {edit.Result} (see inner exception for more details)", new InvalidOperationException(edit.Error)))
				.ToList();

			result.FilesUpdated = response.Results.Length - fileErrors.Count;

			// Append the global errors
			if (response.GlobalError is not null)
			{
				fileErrors.Insert(0, new InvalidOperationException($"Failed to update files: {response.GlobalError}"));
			}
			result.Error = fileErrors.Count switch
			{
				0 => null,
				1 => fileErrors[0],
				_ => new AggregateException("Multiple errors occurred while updating files.", fileErrors),
			};

			if (response.GlobalError is not null || result.FilesUpdated is 0)
			{
				debug?.Debug($"{tag} Server failed to update files (srv error: {response.GlobalError} | updated files: {result.FilesUpdated}).");
				return result;
			}

			if (!req.WaitForHotReload)
			{
				trace?.Trace($"{tag} File updated successfully and do not wait for HR, completing.");
				return result;
			}

			if (response.HotReloadCorrelationId is null)
			{
				debug?.Debug($"{tag} File updated successfully, but didn't get any HR id from server to wait for.");
				return result with { Error = new InvalidOperationException("Cannot wait for Hot reload for this file.") };
			}

			trace?.Trace($"{tag} Successfully updated {result.FilesUpdated} file(s) on server, waiting for server HR id {response.HotReloadCorrelationId}.");

			var serverHr = await WaitForServerHotReloadAsync(response.HotReloadCorrelationId.Value, req.ServerHotReloadTimeout, ct);
			if (serverHr.Result is HotReloadServerResult.NoChanges)
			{
				trace?.Trace($"{tag} Server didn't detected any changes in code, do not wait for local HR.");
				return result with { ApplicationUpdated = false };
			}

			result.ApplicationUpdated = true;

			if (serverHr.Result is not HotReloadServerResult.Success)
			{
				debug?.Debug($"{tag} Server failed to applied changes in code: {serverHr.Result}.");
				return result with { Error = new InvalidOperationException($"Failed to update {req.Edits.Length} file(s), hot-reload failed on server: {serverHr.Result}.") };
			}

			trace?.Trace($"{tag} Successfully got HR from server ({serverHr.Result}), waiting for local HR to complete.");

			var localHr = await WaitForLocalHotReloadAsync(currentLocalHrId + 1, req.LocalHotReloadTimeout, ct);
			if (localHr.Result is HotReloadClientResult.Failed)
			{
				debug?.Debug($"{tag} Failed to apply HR locally: {localHr.Result}.");
				return result with { Error = new InvalidOperationException($"Failed to update {req.Edits.Length} file(s), hot-reload failed locally: {localHr.Result}.") };
			}

			await Task.Delay(100, ct); // Wait a bit to make sure to let the dispatcher to resume, this is just for safety.

			trace?.Trace($"{tag} Successfully updated file and completed HR.");

			return result;
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			return result with { Error = new OperationCanceledException("Update file operation has been cancelled.") };
		}
		catch (Exception error)
		{
			return result with { Error = error };
		}
	}

	#region File updates messaging
	private EventHandler<UpdateFileResponse>? _updateResponse;

	private async ValueTask<UpdateFileResponse> UpdateFileCoreAsync(UpdateFileRequest request, TimeSpan timeout, CancellationToken ct)
	{
		var timeoutTask = Task.Delay(timeout, ct);
		var responseAsync = new TaskCompletionSource<UpdateFileResponse>();

		try
		{
			_updateResponse += OnFileUpdated;

			await _rcClient.SendMessage(request);

			if (await Task.WhenAny(responseAsync.Task, timeoutTask) == timeoutTask)
			{
				throw new TimeoutException($"Failed to get response from the server in the given delay ({timeout:g}).");
			}

			return await responseAsync.Task;
		}
		finally
		{
			_updateResponse -= OnFileUpdated;
		}

		void OnFileUpdated(object? _, UpdateFileResponse response)
		{
			if (response.RequestId == request.RequestId)
			{
				responseAsync.TrySetResult(response);
			}
		}
	}

	partial void ProcessUpdateFileResponse(UpdateFileResponse response)
		=> _updateResponse?.Invoke(this, response);
	#endregion

	private async ValueTask<HotReloadServerOperationData> WaitForServerHotReloadAsync(long hotReloadId, TimeSpan timeout, CancellationToken ct)
	{
		var timeoutTask = Task.Delay(timeout, ct);
		var operationAsync = new TaskCompletionSource<HotReloadServerOperationData>();

		try
		{
			StatusChanged += OnStatusChanged;
			CheckIfCompleted(CurrentStatus);

			if (await Task.WhenAny(operationAsync.Task, timeoutTask) == timeoutTask)
			{
				throw new TimeoutException($"Failed to get hot-reload (id: {hotReloadId}) from the server in the given delay ({timeout:g}).");
			}

			return await operationAsync.Task;
		}
		finally
		{
			StatusChanged -= OnStatusChanged;
		}

		void OnStatusChanged(object? _, Status status)
			=> CheckIfCompleted(status);

		void CheckIfCompleted(Status status)
		{
			var operation = status.Server.Operations.FirstOrDefault(op => op.Id >= hotReloadId && op.Result is not (null or HotReloadServerResult.Aborted));
			if (operation is not null)
			{
				operationAsync.TrySetResult(operation);
			}
		}
	}

	private int GetCurrentLocalHotReloadId()
		=> CurrentStatus.Local.Operations is { Count: > 0 } ops ? ops.Max(op => op.Id) : -1;

	private async ValueTask<HotReloadClientOperation> WaitForLocalHotReloadAsync(int hotReloadId, TimeSpan timeout, CancellationToken ct)
	{
		var timeoutTask = Task.Delay(timeout, ct);
		var operationAsync = new TaskCompletionSource<HotReloadClientOperation>();

		try
		{
			StatusChanged += OnStatusChanged;
			CheckIfCompleted(CurrentStatus);

			if (await Task.WhenAny(operationAsync.Task, timeoutTask) == timeoutTask)
			{
				throw new TimeoutException($"Failed to get a local hot-reload (id: {hotReloadId}) in the given delay ({timeout:g}).");
			}

			return await operationAsync.Task;
		}
		finally
		{
			StatusChanged -= OnStatusChanged;
		}

		void OnStatusChanged(object? _, Status status)
			=> CheckIfCompleted(status);

		void CheckIfCompleted(Status status)
		{
			var operation = status.Local.Operations.FirstOrDefault(op => op.Id >= hotReloadId && op.Result is not null);
			if (operation is not null)
			{
				operationAsync.TrySetResult(operation);
			}
		}
	}
}
#endif
