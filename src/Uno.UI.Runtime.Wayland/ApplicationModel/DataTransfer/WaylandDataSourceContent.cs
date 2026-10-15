#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Wayland.Native;
using Windows.ApplicationModel.DataTransfer;

namespace Uno.UI.Runtime.Wayland;

// Serves wl_data_source.send for one DataPackageView, for the selection and for drags. Each format is
// produced once on the UI dispatcher however many clients read it; pipe ownership stays with the writer.
// Send runs on the dispatch thread.
internal sealed class WaylandDataSourceContent : IDisposable
{
	// Data production and writing each have a bounded deadline.
	private static readonly TimeSpan _writeTimeout = TimeSpan.FromSeconds(5);

	private readonly Action<Action> _postToUI;
	private readonly DataPackageView _view;
	private readonly string _purpose;
	private readonly Dictionary<string, Task<byte[]?>> _cache = new();
	private readonly CancellationTokenSource _cancellation = new();
	private readonly CancellationToken _token;
	private bool _disposed;

	internal WaylandDataSourceContent(Action<Action> postToUI, DataPackageView view, string purpose)
	{
		_postToUI = postToUI;
		_view = view;
		_purpose = purpose;
		_token = _cancellation.Token;
	}

	internal void Send(string mimeType, int fd)
	{
		if (!_cache.TryGetValue(mimeType, out var data))
		{
			var completion = new TaskCompletionSource<byte[]?>(TaskCreationOptions.RunContinuationsAsynchronously);
			_cache[mimeType] = data = completion.Task;
			try
			{
				// Not an async lambda: a failure must reach the completion rather than the UI dispatcher.
				_postToUI(() => _ = ProduceAsync(mimeType, completion));
			}
			catch (Exception error)
			{
				completion.TrySetException(error);
			}
		}
		_ = SendAsync(fd, data, mimeType);
	}

	private async Task ProduceAsync(string mimeType, TaskCompletionSource<byte[]?> completion)
	{
		if (_token.IsCancellationRequested)
		{
			completion.TrySetCanceled(_token);
			return;
		}
		try
		{
			completion.TrySetResult(await GetBytesAsync(mimeType));
		}
		catch (Exception error)
		{
			completion.TrySetException(error);
		}
	}

	private async Task SendAsync(int fd, Task<byte[]?> data, string mimeType)
	{
		try
		{
			if (!await WaylandDataTransfer.SendAsync(fd, data, _writeTimeout, _token)
				&& this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"The {_purpose} reader stopped receiving {mimeType}.");
			}
		}
		catch (OperationCanceledException)
		{
			// The source was replaced or abandoned.
		}
		catch (Exception error)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"The {_purpose} transfer failed for {mimeType}.", error);
			}
		}
	}

	private async Task<byte[]?> GetBytesAsync(string mimeType)
	{
		try
		{
			return await WaylandDataFormats.GetBytesAsync(_view, mimeType);
		}
		catch (Exception error)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Failed to produce {_purpose} data for {mimeType}.", error);
			}
			return null;
		}
	}

	// Stops producing and writing; readers of an abandoned source get a closed pipe.
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		_cancellation.Cancel();
		_cancellation.Dispose();
	}
}
