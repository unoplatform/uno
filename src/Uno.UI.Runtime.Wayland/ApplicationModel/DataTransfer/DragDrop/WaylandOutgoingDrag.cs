#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.UI.Xaml;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Wayland.Protocols;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;

namespace Uno.UI.Runtime.Wayland;

// One drag handed to the compositor through wl_data_device.start_drag, from the moment Uno's drag started
// window until the data source reports its outcome. Started on the UI thread; the source lives on the dispatch thread.
internal sealed class WaylandOutgoingDrag
{
	private readonly WaylandDataDevice _device;
	private readonly WaylandXamlRootHost _host;
	private readonly Action<DataPackageOperation> _onCompleted;
	private readonly WaylandDragSourceState _state = new();
	// The implicit grab the drag belongs to; a press that replaced it must not be converted into this drag.
	private readonly uint _grabSerial;
	// Dispatch thread.
	private WlDataSource? _source;
	private WaylandDataSourceContent? _content;
	private WaylandDragIcon? _icon;
	private bool _cancelled;
	private int _completed;

	internal WaylandOutgoingDrag(WaylandDataDevice device, WaylandXamlRootHost host, XamlRoot xamlRoot, CoreDragInfo info, uint grabSerial, Action<DataPackageOperation> onCompleted)
	{
		_device = device;
		_host = host;
		_grabSerial = grabSerial;
		XamlRoot = xamlRoot;
		Info = info;
		_onCompleted = onCompleted;
	}

	internal XamlRoot XamlRoot { get; }
	internal CoreDragInfo Info { get; }
	internal bool IsCompleted => Volatile.Read(ref _completed) != 0;

	// UI thread. The data is read while the view is current; the compositor handshake follows on the dispatch thread.
	internal async void Start()
	{
		try
		{
			var view = Info.Data;
			var mimeTypes = WaylandDataFormats.GetDragMimeTypes(view);
			// Link has no Wayland action; a drag with nothing else to offer is still a copy for the target.
			var actions = WaylandDataFormats.ToDndActions(Info.AllowedOperations);
			if (actions == 0)
			{
				actions = WaylandDataFormats.DndActionCopy;
			}
			var scale = XamlRoot.RasterizationScale;
			var image = await WaylandDragIcon.CaptureAsync(Info);
			_device.Display.Send(() => StartOnDispatch(view, mimeTypes, actions, image, scale));
		}
		catch (Exception error)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("Failed to start the native drag.", error);
			}
			_device.Display.Send(() =>
			{
				if (_state.Cancelled())
				{
					Complete();
				}
			});
		}
	}

	private void StartOnDispatch(DataPackageView view, List<string> mimeTypes, uint actions, WaylandDragIconImage? image, double scale)
	{
		if (_cancelled)
		{
			return;
		}
		try
		{
			if (_host.Input?.GrabSerial != _grabSerial)
			{
				// The press ended (or another began) before the request could be sent; Uno's internal drag goes on.
				WaylandDiagnostics.Trace("dnd: grab ended before start_drag, the drag stays internal");
				Abandon();
				return;
			}
			var serial = _grabSerial;
			var source = _device.CreateSource();
			if (source.Version < 3)
			{
				// Before v3 a source learns nothing after the drop (no dnd_finished), so the drag could never complete.
				if (WaylandDiagnostics.IsEnabled)
				{
					WaylandDiagnostics.Trace($"dnd: wl_data_source v{source.Version} cannot report an outcome, the drag stays internal");
				}
				source.Dispose();
				Abandon();
				return;
			}
			_source = source;
			foreach (var mimeType in mimeTypes)
			{
				source.Offer(mimeType);
			}
			source.SetActions(actions);
			var content = new WaylandDataSourceContent(_device.PostToUI, view, "drag");
			_content = content;
			source.Target += (_, e) =>
			{
				_state.Target(e.MimeType);
				if (WaylandDiagnostics.IsEnabled)
				{
					WaylandDiagnostics.Trace($"dnd: source target {e.MimeType ?? "(none)"}");
				}
			};
			source.Action += (_, e) =>
			{
				_state.SetAction(e.DndAction);
				if (WaylandDiagnostics.IsEnabled)
				{
					WaylandDiagnostics.Trace($"dnd: source action {e.DndAction}");
				}
			};
			source.Send += (_, e) =>
			{
				_state.Sent();
				content.Send(e.MimeType, e.Fd);
			};
			source.DndDropPerformed += (_, _) =>
			{
				_state.DropPerformed();
				WaylandDiagnostics.Trace("dnd: source drop performed");
			};
			source.DndFinished += (_, _) =>
			{
				if (_state.Finished())
				{
					Complete();
				}
			};
			source.Cancelled += (_, _) =>
			{
				if (_state.Cancelled())
				{
					Complete();
				}
			};
			try
			{
				_icon = WaylandDragIcon.Create(_device.Connection, image, scale);
			}
			catch (Exception error)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn("The drag icon could not be created; dragging without one.", error);
				}
			}
			_host.PointerSource.BeginNativeDrag();
			_device.StartDrag(source, _host.Surface, _icon?.Surface, serial);
			_icon?.Present();
			if (WaylandDiagnostics.IsEnabled)
			{
				WaylandDiagnostics.Trace($"dnd: start_drag serial={serial} actions={actions} icon={_icon is not null} formats={string.Join(",", mimeTypes)}");
			}
		}
		catch (Exception error)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("Failed to start the native drag.", error);
			}
			if (_state.Cancelled())
			{
				Complete();
			}
		}
	}

	// UI thread: a newer drag or the closing of the originating window ends this one as cancelled.
	internal void Cancel() => _device.Display.Send(() =>
	{
		_cancelled = true;
		if (_state.Cancelled())
		{
			Complete();
		}
	});

	// Dispatch thread. The internal drag keeps running; only the registration is dropped.
	private void Abandon()
	{
		if (Interlocked.Exchange(ref _completed, 1) != 0)
		{
			return;
		}
		_device.PostToUI(() =>
		{
			if (_device.OutgoingDrag == this)
			{
				_device.OutgoingDrag = null;
			}
		});
	}

	// Dispatch thread, once per drag.
	private void Complete()
	{
		if (Interlocked.Exchange(ref _completed, 1) != 0)
		{
			return;
		}
		var result = WaylandDataFormats.ToOperations(_state.Result);
		if (WaylandDiagnostics.IsEnabled)
		{
			WaylandDiagnostics.Trace($"dnd: source {_state.Phase} action={_state.Result} ({result}) targetAccepts={_state.TargetAccepts} sent={_state.DataSent}");
		}
		_icon?.Dispose();
		_icon = null;
		_content?.Dispose();
		_content = null;
		_source?.Dispose();
		_source = null;
		_device.PostToUI(() =>
		{
			if (_device.OutgoingDrag == this)
			{
				_device.OutgoingDrag = null;
			}
			_onCompleted(result);
		});
		// Queued after the completion so the drop result precedes the capture loss.
		_host.PointerSource.EndNativeDrag();
	}
}
