#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uno.ApplicationModel.DataTransfer;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Wayland.Native;
using Uno.UI.Runtime.Wayland.Protocols;
using Windows.ApplicationModel.DataTransfer;

namespace Uno.UI.Runtime.Wayland;

// Local copies remain readable even without a seat or selection ownership. Foreign selections use wl_data_device.
internal sealed class WaylandClipboardExtension : IClipboardExtension
{
	// Data production and writing each have a bounded deadline.
	private static readonly TimeSpan _writeTimeout = TimeSpan.FromSeconds(5);

	private readonly IWaylandClipboardDevice? _device;
	private readonly Action<Action> _postToUI;
	private readonly object _gate = new();
	// Guarded by _gate: local generations survive announcements while pending and echoed offers while owned.
	private DataPackageView? _content;
	private int _generation;
	private int? _pendingGeneration;
	// Dispatch thread.
	private WlDataSource? _source;
	private CancellationTokenSource? _sourceCancellation;
	private volatile bool _isMonitoringEnabled;

	internal WaylandClipboardExtension(IWaylandClipboardDevice? device, Action<Action> postToUI)
	{
		_device = device;
		_postToUI = postToUI;
		if (device is not null)
		{
			device.SelectionChanged += (_, _) =>
			{
				// On refocus the existing offer precedes keyboard enter. It must not overwrite a pending copy or clear.
				if (_source is null)
				{
					lock (_gate)
					{
						if (_pendingGeneration is null)
						{
							_content = null;
							_generation++;
						}
					}
				}
				NotifyContentChanged();
			};
		}
	}

	private void NotifyContentChanged()
	{
		if (_isMonitoringEnabled)
		{
			_postToUI(() => ContentChanged?.Invoke(this, EventArgs.Empty));
		}
	}

	public event EventHandler<object>? ContentChanged;

	public void StartContentChanged() => _isMonitoringEnabled = true;

	public void StopContentChanged() => _isMonitoringEnabled = false;

	// Nothing to persist: the compositor keeps no copy, and clipboard managers fetch the data themselves.
	public void Flush() { }

	public void Clear() => SetSelection(null);

	public void SetContent(DataPackage content) => SetSelection(content.GetView());

	private void SetSelection(DataPackageView? view)
	{
		int generation;
		lock (_gate)
		{
			// Readable locally right away, as on WinUI, even while publishing waits for keyboard focus.
			_content = view ?? new DataPackage().GetView();
			generation = ++_generation;
			_pendingGeneration = generation;
		}
		if (_device is not { } device)
		{
			NotifyContentChanged();
			return;
		}
		var mimeTypes = view is null ? [] : WaylandDataFormats.GetMimeTypes(view);
		// Wayland only accepts a selection from the focused client with a recent input serial.
		device.Post(() =>
		{
			if (IsCurrentSelection(generation))
			{
				device.RequestSelection(Publish);
			}
		});
		NotifyContentChanged();

		void Publish()
		{
			if (!IsCurrentSelection(generation))
			{
				return;
			}
			var previous = _source;
			_sourceCancellation?.Cancel();
			_sourceCancellation?.Dispose();
			_sourceCancellation = null;
			_source = null;
			if (view is not null)
			{
				var source = device.CreateSource();
				foreach (var mimeType in mimeTypes)
				{
					source.Offer(mimeType);
				}
				// Each format is produced once per content, however many clients paste it.
				var cancellation = new CancellationTokenSource();
				_sourceCancellation = cancellation;
				var token = cancellation.Token;
				var data = new Dictionary<string, Task<byte[]?>>();
				source.Send += (_, e) => Send(view, data, e.MimeType, e.Fd, token);
				source.Cancelled += (_, _) =>
				{
					cancellation.Cancel();
					if (_source == source)
					{
						_source = null;
						_sourceCancellation = null;
						cancellation.Dispose();
						lock (_gate)
						{
							// Ownership loss invalidates this source, never a newer deferred copy or clear.
							if (_generation == generation)
							{
								_content = null;
								_generation++;
							}
						}
						NotifyContentChanged();
					}
					source.Destroy();
				};
				_source = source;
			}
			device.SetSelection(_source);
			lock (_gate)
			{
				if (_pendingGeneration == generation)
				{
					_pendingGeneration = null;
				}
			}
			previous?.Destroy();
		}
	}

	private bool IsCurrentSelection(int generation)
	{
		lock (_gate)
		{
			return _generation == generation;
		}
	}

	// Providers run on the UI dispatcher; pipe ownership stays with the worker.
	private void Send(DataPackageView view, Dictionary<string, Task<byte[]?>> cache, string mimeType, int fd, CancellationToken cancellationToken)
	{
		if (!cache.TryGetValue(mimeType, out var data))
		{
			var completion = new TaskCompletionSource<byte[]?>(TaskCreationOptions.RunContinuationsAsynchronously);
			cache[mimeType] = data = completion.Task;
			try
			{
				// Not an async lambda: a failure must reach the completion rather than the UI dispatcher.
				_postToUI(() => _ = ProduceAsync(view, mimeType, completion, cancellationToken));
			}
			catch (Exception error)
			{
				completion.TrySetException(error);
			}
		}
		_ = SendAsync(fd, data, mimeType, cancellationToken);
	}

	private async Task ProduceAsync(DataPackageView view, string mimeType, TaskCompletionSource<byte[]?> completion, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			completion.TrySetCanceled(cancellationToken);
			return;
		}
		try
		{
			completion.TrySetResult(await GetBytesAsync(view, mimeType));
		}
		catch (Exception error)
		{
			completion.TrySetException(error);
		}
	}

	private async Task SendAsync(int fd, Task<byte[]?> data, string mimeType, CancellationToken cancellationToken)
	{
		try
		{
			if (!await WaylandDataTransfer.SendAsync(fd, data, _writeTimeout, cancellationToken)
				&& this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"The clipboard reader stopped receiving {mimeType}.");
			}
		}
		catch (OperationCanceledException)
		{
			// A newer selection replaced this one.
		}
		catch (Exception error)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"Clipboard transfer failed for {mimeType}.", error);
			}
		}
	}

	private async Task<byte[]?> GetBytesAsync(DataPackageView view, string mimeType)
	{
		try
		{
			return await WaylandDataFormats.GetBytesAsync(view, mimeType);
		}
		catch (Exception error)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Failed to produce clipboard data for {mimeType}.", error);
			}
			return null;
		}
	}

	public DataPackageView GetContent()
	{
		lock (_gate)
		{
			if (_content is { } content)
			{
				return content;
			}
		}

		var package = new DataPackage();
		_device?.FillContent(package);
		return package.GetView();
	}
}
