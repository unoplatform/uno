#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Uno.UI.Runtime.Wayland.Protocols;
using Uno.UI.Runtime.Wayland.Native;
using Windows.ApplicationModel.DataTransfer;

namespace Uno.UI.Runtime.Wayland;

// The seat's wl_data_device, shared by the clipboard and every window's drop target.
// Protocol events arrive on the dispatch thread; drag-and-drop events are forwarded to the UI thread in order.
internal sealed class WaylandDataDevice : IWaylandClipboardDevice
{
	private readonly WaylandConnection _connection;
	private readonly WlDataDeviceManager _manager;
	private readonly WlDataDevice _device;
	private readonly WaylandSelectionSerial _selectionSerial = new();
	private WlKeyboard? _keyboard;
	private WlPointer? _pointer;

	// Dispatch thread: offers announced by data_offer and not yet claimed by selection or enter.
	private readonly Dictionary<WlDataOffer, WaylandDataOffer> _newOffers = new();
	// Written on the dispatch thread, read under SyncRoot.
	private WaylandDataOffer? _selection;

	// UI thread.
	private readonly List<WaylandDragDropExtension> _dropTargets = new();
	private WaylandDragDropExtension? _dropTarget;
	// UI thread: the drag this client started and that has not completed, so its own windows recognise the offer.
	internal WaylandOutgoingDrag? OutgoingDrag { get; set; }

	private WaylandDataDevice(WaylandConnection connection, WlDataDeviceManager manager, WlSeat seat)
	{
		_connection = connection;
		_manager = manager;
		_device = manager.GetDataDevice(seat);
		seat.Capabilities += (_, e) => UpdateSeat(seat, e.Capabilities);
		UpdateSeat(seat, connection.SeatCapabilities);
		_device.DataOffer += (_, e) => _newOffers[e.Id] = new WaylandDataOffer(e.Id);
		_device.Selection += OnSelection;
		_device.Enter += OnEnter;
		_device.Motion += (_, e) =>
		{
			var position = new Point(e.X.ToDouble(), e.Y.ToDouble());
			_connection.PostToUI(() => _dropTarget?.Motion(position));
		};
		_device.Leave += (_, _) => _connection.PostToUI(() =>
		{
			_dropTarget?.Leave();
			_dropTarget = null;
		});
		_device.Drop += (_, _) => _connection.PostToUI(() =>
		{
			_dropTarget?.Drop();
			_dropTarget = null;
		});
	}

	private void UpdateSeat(WlSeat seat, uint capabilities)
	{
		if ((capabilities & (uint)WlSeat.CapabilityValues.Keyboard) == 0)
		{
			_selectionSerial.Leave();
			_keyboard?.Dispose();
			_keyboard = null;
		}
		else if (_keyboard is null)
		{
			_keyboard = seat.GetKeyboard();
			_keyboard.Keymap += (_, e) =>
			{
				_ = Posix.close(e.Fd);
			};
			_keyboard.Enter += (_, e) =>
			{
				_selectionSerial.Enter(e.Serial);
			};
			_keyboard.Key += (_, e) =>
			{
				_selectionSerial.Update(e.Serial);
			};
			_keyboard.Leave += (_, _) => _selectionSerial.Leave();
		}
		if ((capabilities & (uint)WlSeat.CapabilityValues.Pointer) == 0)
		{
			_pointer?.Dispose();
			_pointer = null;
		}
		else if (_pointer is null)
		{
			_pointer = seat.GetPointer();
			_pointer.Button += (_, e) => _selectionSerial.Update(e.Serial);
		}
	}

	/// <summary>Creates the data device when the compositor has a seat and wl_data_device_manager.</summary>
	internal static WaylandDataDevice? Create(WaylandConnection connection)
	{
		var display = connection.Display;
		lock (display.SyncRoot)
		{
			if (connection.Seat is not { } seat || display.GetGlobal(WlDataDeviceManager.ProtocolName) is null)
			{
				return null;
			}
			// v3 adds drag-and-drop actions and wl_data_offer.finish.
			var manager = (WlDataDeviceManager)display.Bind(WlDataDeviceManager.ProtocolName, 3);
			return new WaylandDataDevice(connection, manager, seat);
		}
	}

	internal WaylandDisplay Display => _connection.Display;

	internal WaylandConnection Connection => _connection;

	internal void PostToUI(Action action) => _connection.PostToUI(action);

	/// <summary>Raised on the dispatch thread when the selection changes while one of our surfaces has keyboard focus.</summary>
	internal event EventHandler? SelectionChanged;

	internal WaylandDataOffer? Selection
	{
		get
		{
			lock (Display.SyncRoot)
			{
				return _selection;
			}
		}
	}

	// Sources before wl_data_source v3 never learn a drag's outcome, so drags are only handed to the compositor from v3.
	internal bool SupportsDragOutcome => _manager.Version >= 3;

	// Dispatch thread.
	internal WlDataSource CreateSource() => _manager.CreateDataSource();

	// Dispatch thread. A null source clears the selection.
	internal void SetSelection(WlDataSource? source) => _device.SetSelection(source, _selectionSerial.Serial);

	// Dispatch thread. The serial must be the button press or touch down of the implicit grab being converted.
	internal void StartDrag(WlDataSource source, WlSurface origin, WlSurface? icon, uint serial) => _device.StartDrag(source, origin, icon, serial);

	event EventHandler? IWaylandClipboardDevice.SelectionChanged
	{
		add => SelectionChanged += value;
		remove => SelectionChanged -= value;
	}

	bool IWaylandClipboardDevice.HasKeyboardFocus => _selectionSerial.HasFocus;
	void IWaylandClipboardDevice.Post(Action action) => Display.Post(action);
	WlDataSource IWaylandClipboardDevice.CreateSource() => CreateSource();
	void IWaylandClipboardDevice.SetSelection(WlDataSource? source) => SetSelection(source);

	void IWaylandClipboardDevice.FillContent(DataPackage package)
	{
		if (Selection is { } offer)
		{
			WaylandDataFormats.Fill(package, offer);
		}
	}

	// UI thread.
	internal void AddDropTarget(WaylandDragDropExtension target) => _dropTargets.Add(target);

	// UI thread.
	internal void RemoveDropTarget(WaylandDragDropExtension target)
	{
		_dropTargets.Remove(target);
		if (_dropTarget == target)
		{
			_dropTarget = null;
		}
		target.Leave();
	}

	private void OnSelection(object? sender, WlDataDevice.SelectionEventArgs e)
	{
		_selection?.Destroy();
		_selection = e.Id is { } id ? Claim(id) : null;
		SelectionChanged?.Invoke(this, EventArgs.Empty);
	}

	private void OnEnter(object? sender, WlDataDevice.EnterEventArgs e)
	{
		// A drag without a source (a client-local drag of another client) carries no offer and no data.
		var offer = e.Id is { } id ? Claim(id) : null;
		var surface = e.Surface;
		var serial = e.Serial;
		var position = new Point(e.X.ToDouble(), e.Y.ToDouble());
		_connection.PostToUI(() =>
		{
			_dropTarget = offer is null ? null : _dropTargets.Find(target => target.Surface == surface);
			if (_dropTarget is { } target)
			{
				target.Enter(offer!, serial, position);
			}
			else
			{
				offer?.Destroy();
			}
		});
	}

	private WaylandDataOffer? Claim(WlDataOffer proxy)
		=> _newOffers.Remove(proxy, out var offer) ? offer : null;
}
