#nullable enable

using System;
using Uno.ApplicationModel.DataTransfer;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Wayland.Protocols;
using Windows.ApplicationModel.DataTransfer;

namespace Uno.UI.Runtime.Wayland;

// Local copies remain readable even without a seat or selection ownership. Foreign selections use wl_data_device.
internal sealed class WaylandClipboardExtension : IClipboardExtension
{
	private readonly IWaylandClipboardDevice? _device;
	private readonly Action<Action> _postToUI;
	private readonly object _gate = new();
	// Guarded by _gate: local generations survive announcements while in flight and echoed offers while owned.
	private DataPackageView? _content;
	private int _generation;
	private int? _pendingGeneration;
	// Dispatch thread.
	private WlDataSource? _source;
	private WaylandDataSourceContent? _sourceContent;
	private volatile bool _isMonitoringEnabled;

	internal WaylandClipboardExtension(IWaylandClipboardDevice? device, Action<Action> postToUI)
	{
		_device = device;
		_postToUI = postToUI;
		if (device is not null)
		{
			device.SelectionChanged += (_, _) =>
			{
				// An announcement must not overwrite a copy or clear still on its way to the dispatch thread.
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
		if (_device is { HasKeyboardFocus: false })
		{
			// Queuing until focus returns would overwrite whatever the user copied elsewhere in the meantime.
			WarnUnfocused();
			return;
		}
		int generation;
		lock (_gate)
		{
			// Readable locally right away, as on WinUI.
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
		device.Send(Publish);
		NotifyContentChanged();

		void Publish()
		{
			if (!IsCurrentSelection(generation))
			{
				return;
			}
			if (!device.HasKeyboardFocus)
			{
				// Focus left before the request reached the dispatch thread: nothing was published.
				lock (_gate)
				{
					if (_generation == generation)
					{
						_content = null;
						_pendingGeneration = null;
						_generation++;
					}
				}
				WarnUnfocused();
				NotifyContentChanged();
				return;
			}
			var previous = _source;
			_sourceContent?.Dispose();
			_sourceContent = null;
			_source = null;
			if (view is not null)
			{
				var source = device.CreateSource();
				foreach (var mimeType in mimeTypes)
				{
					source.Offer(mimeType);
				}
				var content = new WaylandDataSourceContent(_postToUI, view, "clipboard");
				_sourceContent = content;
				source.Send += (_, e) => content.Send(e.MimeType, e.Fd);
				source.Cancelled += (_, _) =>
				{
					content.Dispose();
					if (_source == source)
					{
						_source = null;
						_sourceContent = null;
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

	private void WarnUnfocused()
	{
		if (this.Log().IsEnabled(LogLevel.Warning))
		{
			this.Log().Warn("The clipboard was not set: Wayland only lets the window with keyboard focus set it.");
		}
	}

	private bool IsCurrentSelection(int generation)
	{
		lock (_gate)
		{
			return _generation == generation;
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
