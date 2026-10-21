#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.UI.Input;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Uno.UI.Hosting;
using PointerDeviceType = global::Windows.Devices.Input.PointerDeviceType;
using PointerEventArgs = global::Windows.UI.Core.PointerEventArgs;

namespace Uno.UI.Runtime.Wayland.Input;

internal sealed class WaylandPointerInputSource : IUnoCorePointerInputSource
{
	public event TypedEventHandler<object, PointerEventArgs>? PointerCaptureLost;
	public event TypedEventHandler<object, PointerEventArgs>? PointerEntered;
	public event TypedEventHandler<object, PointerEventArgs>? PointerExited;
	public event TypedEventHandler<object, PointerEventArgs>? PointerMoved;
	public event TypedEventHandler<object, PointerEventArgs>? PointerPressed;
	public event TypedEventHandler<object, PointerEventArgs>? PointerReleased;
	public event TypedEventHandler<object, PointerEventArgs>? PointerWheelChanged;
	public event TypedEventHandler<object, PointerEventArgs>? PointerCancelled;
	private readonly Action<Action> _dispatch;
	private readonly Func<VirtualKeyModifiers> _modifiers;
	private readonly IWaylandInputConnection _connection;
	private readonly Dictionary<int, Point> _touches = new();
	private readonly List<Action> _touchFrame = new();
	private readonly double[] _axis = new double[2], _remainder = new double[2];
	private readonly int[] _discrete = new int[2], _value120 = new int[2];
	private readonly bool[] _hasDiscrete = new bool[2], _hasValue120 = new bool[2];
	private uint _buttons, _serial, _time, _frame;
	private int? _primaryTouch;
	private readonly bool[] _stopped = new bool[2];
	private uint _lastTimestamp;
	private ulong _timestampEpoch;
	private bool _entered;
	private bool _nativeDrag, _nativeDragCaptureLost;
	private CoreCursor _cursor = new(CoreCursorType.Arrow, 0);

	public WaylandPointerInputSource(IWaylandInputConnection connection, Action<Action> dispatch, Func<VirtualKeyModifiers> modifiers)
	{
		_connection = connection;
		_dispatch = dispatch;
		_modifiers = modifiers;
	}

	public bool HasCapture => false;
	public Point PointerPosition
	{
		get; private set;
	}
	public CoreCursor PointerCursor
	{
		get => _cursor;
		set
		{
			_cursor = value;
			_connection.Post(() => { if (_entered) { _connection.SetCursor(_serial, value); } });
		}
	}

	// Core Wayland supplies only an implicit grab while buttons are pressed.
	public void SetPointerCapture()
	{
	}
	public void SetPointerCapture(PointerIdentifier pointer)
	{
	}
	public void ReleasePointerCapture()
	{
	}
	public void ReleasePointerCapture(PointerIdentifier pointer)
	{
	}

	public void Enter(uint serial, double x, double y)
	{
		_serial = serial;
		_entered = true;
		PointerPosition = new(x, y);
		_connection.SetCursor(serial, _cursor);
		Emit(PointerEntered);
	}

	public void Leave()
	{
		if (!_entered)
		{
			return;
		}
		Emit(PointerExited);
		ResetPointer();
	}

	public void Motion(uint time, double x, double y)
	{
		_time = time;
		PointerPosition = new(x, y);
		if (_entered)
		{
			Emit(PointerMoved);
		}
	}

	public void Button(uint time, uint button, uint state)
	{
		if (!_entered)
		{
			return;
		}
		_time = time;
		var index = button switch
		{
			0x110 => 0,
			0x111 => 1,
			0x112 => 2,
			0x113 or 0x116 => 3,
			0x114 or 0x115 => 4,
			_ => -1
		};
		if (index < 0)
		{
			return;
		}
		var down = state == 1;
		if (down)
		{
			_buttons |= 1u << index;
		}
		else
		{
			_buttons &= ~(1u << index);
		}
		var kind = index switch
		{
			0 => down ? PointerUpdateKind.LeftButtonPressed : PointerUpdateKind.LeftButtonReleased,
			1 => down ? PointerUpdateKind.RightButtonPressed : PointerUpdateKind.RightButtonReleased,
			2 => down ? PointerUpdateKind.MiddleButtonPressed : PointerUpdateKind.MiddleButtonReleased,
			3 => down ? PointerUpdateKind.XButton1Pressed : PointerUpdateKind.XButton1Released,
			_ => down ? PointerUpdateKind.XButton2Pressed : PointerUpdateKind.XButton2Released
		};
		Emit(down ? PointerPressed : PointerReleased, kind);
	}

	public void Axis(uint time, uint axis, double value)
	{
		_time = time;
		if (axis < 2)
		{
			_axis[axis] += value;
		}
	}
	public void AxisDiscrete(uint axis, int steps)
	{
		if (axis < 2)
		{
			_hasDiscrete[axis] = true;
			_discrete[axis] += steps;
		}
	}
	public void AxisValue120(uint axis, int value)
	{
		if (axis < 2)
		{
			_hasValue120[axis] = true;
			_value120[axis] += value;
		}
	}
	public void AxisStop(uint axis)
	{
		if (axis < 2)
		{
			_stopped[axis] = true;
		}
	}

	public void Frame()
	{
		for (var axis = 0; axis < 2; axis++)
		{
			// Continuous scrolling uses a policy of 10 logical units per wheel detent.
			var delta = _hasValue120[axis] ? _value120[axis] : _hasDiscrete[axis] ? _discrete[axis] * 120.0 : _axis[axis] * 12;
			_remainder[axis] -= delta;
			var whole = (int)_remainder[axis];
			_remainder[axis] -= whole;
			if (_entered && whole != 0)
			{
				Emit(PointerWheelChanged, wheel: whole, horizontal: axis == 1);
			}
			_axis[axis] = _discrete[axis] = _value120[axis] = 0;
			_hasDiscrete[axis] = _hasValue120[axis] = false;
			if (_stopped[axis])
			{
				_remainder[axis] = 0;
				_stopped[axis] = false;
			}
		}
	}

	// The id of the latest pointer frame handed to Uno; later synthetic drag frames must count past it.
	public uint LastFrameId => Volatile.Read(ref _frame);

	// Dispatch thread, before wl_data_device.start_drag: the compositor then ends the implicit grab with a leave,
	// which must not cancel the drag Uno keeps running until the drag source completes.
	public void BeginNativeDrag() => _nativeDrag = true;

	// Dispatch thread, once the drag source completed: the suppressed cancellation is delivered now, after the
	// drop result was reported, so the dragged element ends its gesture and releases its pointer capture.
	public void EndNativeDrag()
	{
		_nativeDrag = false;
		if (_nativeDragCaptureLost)
		{
			_nativeDragCaptureLost = false;
			Emit(PointerCancelled);
			Emit(PointerCaptureLost);
		}
	}

	public void ResetPointer()
	{
		if (_buttons != 0)
		{
			_buttons = 0;
			if (_nativeDrag)
			{
				_nativeDragCaptureLost = true;
			}
			else
			{
				Emit(PointerCancelled);
				Emit(PointerCaptureLost);
			}
		}
		_entered = false;
		Array.Clear(_axis);
		Array.Clear(_discrete);
		Array.Clear(_value120);
		Array.Clear(_hasDiscrete);
		Array.Clear(_hasValue120);
		Array.Clear(_remainder);
		Array.Clear(_stopped);
	}

	public void TouchDown(uint time, int id, double x, double y)
	{
		if (_touches.ContainsKey(id))
		{
			return;
		}
		_primaryTouch ??= id;
		_touches[id] = new(x, y);
		TouchEmit(PointerEntered, time, id, new(x, y), true);
		TouchEmit(PointerPressed, time, id, new(x, y), true);
	}
	public void TouchMotion(uint time, int id, double x, double y)
	{
		if (!_touches.ContainsKey(id))
		{
			return;
		}
		_touches[id] = new(x, y);
		TouchEmit(PointerMoved, time, id, new(x, y), true);
	}
	public void TouchUp(uint time, int id)
	{
		if (!_touches.Remove(id, out var point))
		{
			return;
		}
		TouchEmit(PointerReleased, time, id, point, false);
		TouchEmit(PointerExited, time, id, point, false);
		if (_touches.Count == 0)
		{
			_primaryTouch = null;
		}
	}
	public void TouchFrame()
	{
		foreach (var action in _touchFrame)
		{
			_dispatch(action);
		}
		_touchFrame.Clear();
	}
	public void TouchCancel()
	{
		TouchFrame();
		foreach (var touch in _touches)
		{
			TouchEmit(PointerCancelled, _time, touch.Key, touch.Value, false);
		}
		_touches.Clear();
		_primaryTouch = null;
		TouchFrame();
	}
	private void TouchEmit(TypedEventHandler<object, PointerEventArgs>? handler, uint time, int id, Point point, bool contact)
	{
		_time = time;
		var props = new PointerPointProperties { IsLeftButtonPressed = contact, IsPrimary = id == _primaryTouch, IsInRange = contact, TouchConfidence = true, IsCanceled = handler == PointerCancelled };
		var args = Create(time, unchecked((uint)id + 1), point, PointerDeviceType.Touch, props);
		_touchFrame.Add(() => handler?.Invoke(this, args));
	}
	private void Emit(TypedEventHandler<object, PointerEventArgs>? handler, PointerUpdateKind kind = PointerUpdateKind.Other, int wheel = 0, bool horizontal = false)
	{
		var props = new PointerPointProperties
		{
			IsPrimary = true,
			IsInRange = _entered,
			IsCanceled = handler == PointerCancelled,
			IsLeftButtonPressed = (_buttons & 1) != 0,
			IsRightButtonPressed = (_buttons & 2) != 0,
			IsMiddleButtonPressed = (_buttons & 4) != 0,
			IsXButton1Pressed = (_buttons & 8) != 0,
			IsXButton2Pressed = (_buttons & 16) != 0,
			PointerUpdateKind = kind,
			MouseWheelDelta = wheel,
			IsHorizontalMouseWheel = horizontal
		};
		var args = Create(_time, 0, PointerPosition, PointerDeviceType.Mouse, props);
		_dispatch(() => handler?.Invoke(this, args));
	}
	private PointerEventArgs Create(uint time, uint id, Point point, PointerDeviceType type, PointerPointProperties properties)
	{
		if (time < _lastTimestamp && _lastTimestamp - time > int.MaxValue)
		{
			_timestampEpoch += 1UL << 32;
		}
		_lastTimestamp = time;
		return new(new PointerPoint(++_frame, (_timestampEpoch + time) * 1000, PointerDevice.For(type), id, point, point, properties.HasPressedButton, properties), _modifiers());
	}
}
