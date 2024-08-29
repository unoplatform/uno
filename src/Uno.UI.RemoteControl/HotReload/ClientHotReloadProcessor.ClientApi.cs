#if __SKIA__

using System;
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
	/// <param name="FileUpdated">Indicates if is known to have been updated on server-side.</param>
	/// <param name="ApplicationUpdated">Indicates if the change had an impact on the compilation of the application (might be a success-full build or an error).</param>
	/// <param name="Error">Gets the error if any happened during the update.</param>
	public record struct UpdateResult(
		bool FileUpdated,
		bool? ApplicationUpdated,
		Exception? Error = null);

	public async Task UpdateFileAsync(string filePath, string oldText, string newText, bool waitForHotReload, CancellationToken ct)
	{
		if (await TryUpdateFileAsync(filePath, oldText, newText, waitForHotReload, ct) is { Error: { } error })
		{
			ExceptionDispatchInfo.Throw(error);
		}
	}

	public async Task<UpdateResult> TryUpdateFileAsync(string filePath, string oldText, string newText, bool waitForHotReload, CancellationToken ct)
	{
		var result = default(UpdateResult);
		try
		{
			if (string.IsNullOrWhiteSpace(filePath))
			{
				return result with { Error = new ArgumentOutOfRangeException(nameof(filePath), "File path is invalid (null or empty).") };
			}

			var log = this.Log();
			var trace = log.IsTraceEnabled(LogLevel.Trace) ? log : default;
			var debug = log.IsDebugEnabled(LogLevel.Debug) ? log : default;
			var tag = $"[{Interlocked.Increment(ref _reqId):D2}-{Path.GetFileName(filePath)}]";

			debug?.Debug($"{tag} Updating file {filePath} (from: {oldText[..100]} | to: {newText[..100]}.");

			var request = new UpdateFile { FilePath = filePath, OldText = oldText, NewText = newText };
			var response = await UpdateFileCoreAsync(request, ct);

			if (response.Result is FileUpdateResult.NoChanges)
			{
				debug?.Debug($"{tag} Changes requested has no effect on server, completing.");
				return result;
			}

			if (response.Result is not FileUpdateResult.Success)
			{
				debug?.Debug($"{tag} Server failed to update file: {response.Result} (srv error: {response.Error}).");
				return result with { Error = new InvalidOperationException($"Failed to update file {filePath}: {response.Result} (see inner exception for more details)", new InvalidOperationException(response.Error)) };
			}

			result.FileUpdated = true;

			if (!waitForHotReload)
			{
				trace?.Trace($"{tag} File updated successfully and do not wait for HR, completing.");
				return result;
			}

			if (response.HotReloadCorrelationId is null)
			{
				debug?.Debug($"{tag} File updated successfully, but didn't get any HR id from server to wait for.");
				return result with { Error = new InvalidOperationException("Cannot wait for Hot reload for this file.") };
			}

			trace?.Trace($"{tag} Successfully updated file on server ({response.Result}), waiting for server HR id {response.HotReloadCorrelationId}.");

			var localHrTask = WaitForNextLocalHotReload(ct);
			var serverHr = await WaitForServerHotReloadAsync(response.HotReloadCorrelationId.Value, ct);
			if (serverHr.Result is HotReloadServerResult.NoChanges)
			{
				trace?.Trace($"{tag} Server didn't detected any changes in code, do not wait for local HR.");
				return result with { ApplicationUpdated = false };
			}

			result.ApplicationUpdated = true;

			if (serverHr.Result is not HotReloadServerResult.Success)
			{
				debug?.Debug($"{tag} Server failed to applied changes in code: {serverHr.Result}.");
				return result with { Error = new InvalidOperationException($"Failed to update file {filePath}, hot-reload failed on server: {serverHr.Result}.") };
			}

			trace?.Trace($"{tag} Successfully got HR from server ({serverHr.Result}), waiting for local HR to complete.");

			var localHr = await localHrTask;
			if (localHr.Result is HotReloadClientResult.Failed)
			{
				debug?.Debug($"{tag} Failed to apply HR locally: {localHr.Result}.");
				return result with { Error = new InvalidOperationException($"Failed to update file {filePath}, hot-reload failed locally: {localHr.Result}.") };
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

	private async ValueTask<UpdateFileResponse> UpdateFileCoreAsync(UpdateFile request, CancellationToken ct)
	{
		var timeout = Task.Delay(10_000, ct);
		var responseAsync = new TaskCompletionSource<UpdateFileResponse>();

		try
		{
			_updateResponse += OnFileUpdated;

			await _rcClient.SendMessage(request);

			if (await Task.WhenAny(responseAsync.Task, timeout) == timeout)
			{
				throw new TimeoutException("Failed to get response from the server in the given delay.");
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

	private async ValueTask<HotReloadServerOperationData> WaitForServerHotReloadAsync(long hotReloadId, CancellationToken ct)
	{
		var timeout = Task.Delay(10_000, ct);
		var operationAsync = new TaskCompletionSource<HotReloadServerOperationData>();

		try
		{
			StatusChanged += OnStatusChanged;
			CheckIfCompleted(CurrentStatus);

			if (await Task.WhenAny(operationAsync.Task, timeout) == timeout)
			{
				throw new TimeoutException($"Failed to get hot-reload (id: {hotReloadId}) from the server in the given delay.");
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

	private async ValueTask<HotReloadClientOperation> WaitForNextLocalHotReload(CancellationToken ct)
	{
		var timeout = Task.Delay(10_000, ct);
		var operationAsync = new TaskCompletionSource<HotReloadClientOperation>();
		var previousId = CurrentStatus.Local.Operations is { Count: > 0 } ops ? ops.Max(op => op.Id) : -1;

		try
		{
			StatusChanged += OnStatusChanged;

			if (await Task.WhenAny(operationAsync.Task, timeout) == timeout)
			{
				throw new TimeoutException($"Failed to get a local hot-reload (id: {previousId}+) in the given delay.");
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
			var operation = status.Local.Operations.FirstOrDefault(op => op.Id > previousId && op.Result is not null);
			if (operation is not null)
			{
				operationAsync.TrySetResult(operation);
			}
		}
	}
}
#endif
