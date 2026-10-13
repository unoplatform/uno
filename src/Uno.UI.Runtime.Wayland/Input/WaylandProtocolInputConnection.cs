#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Windows.UI.Core;
using Uno.UI.Runtime.Wayland.Protocols;

namespace Uno.UI.Runtime.Wayland.Input;

internal sealed class WaylandProtocolInputConnection : IWaylandInputConnection
{
	private readonly WlSeat _seat;
	private readonly Func<WlSurface?, bool> _ownsSurface;
	private readonly Action<Action> _post;
	private readonly Action<uint, CoreCursor?> _cursor;

	public WaylandProtocolInputConnection(WlSeat seat, Func<WlSurface?, bool> ownsSurface, Action<Action> post, Action<uint, CoreCursor?> cursor)
	{
		_seat = seat;
		_ownsSurface = ownsSurface;
		_post = post;
		_cursor = cursor;
	}
	public void Post(Action action) => _post(action);
	public void SetCursor(uint serial, CoreCursor? cursor) => _cursor(serial, cursor);

	// The live wl_pointer, for cursor surfaces and cursor-shape devices. Dispatch thread only.
	public WlPointer? Pointer { get; private set; }
	// Raised on the dispatch thread before the wl_pointer is released, so cursor objects can go first.
	public event EventHandler? PointerReleasing;
	// Raised on the dispatch thread with the serial of each keyboard enter, key and pointer button event.
	public event EventHandler<uint>? InputSerial;
	// Raised on the dispatch thread for buttons on this window's surface, before the pointer source posts them
	// to the UI thread.
	public event EventHandler<WlPointer.ButtonEventArgs>? PointerButton;

	public IDisposable GetPointer(WaylandPointerInputSource sink)
	{
		var proxy = _seat.GetPointer();
		var binding = new Binding(proxy);
		Pointer = proxy;
		if (WaylandDiagnostics.IsEnabled)
		{
			WaylandDiagnostics.Trace($"input: wl_pointer v{proxy.Version} bound");
		}
		bool focused = false;
		EventHandler<WlPointer.EnterEventArgs> enter = (_, e) =>
		{
			focused = _ownsSurface(e.Surface);
			Trace($"input: pointer enter serial={e.Serial} at {e.SurfaceX.ToDouble():0.##},{e.SurfaceY.ToDouble():0.##} owned={focused}");
			if (focused) { sink.Enter(e.Serial, e.SurfaceX.ToDouble(), e.SurfaceY.ToDouble()); }
		};
		EventHandler<WlPointer.LeaveEventArgs> leave = (_, e) => { if (focused) { Trace($"input: pointer leave"); sink.Leave(); } focused = false; };
		EventHandler<WlPointer.MotionEventArgs> motion = (_, e) => { if (focused) { Trace($"input: pointer motion {e.SurfaceX.ToDouble():0.##},{e.SurfaceY.ToDouble():0.##}"); sink.Motion(e.Time, e.SurfaceX.ToDouble(), e.SurfaceY.ToDouble()); } };
		EventHandler<WlPointer.ButtonEventArgs> button = (_, e) => { if (focused) { Trace($"input: pointer button 0x{e.Button:x} state={e.State}"); PointerButton?.Invoke(this, e); sink.Button(e.Time, e.Button, e.State); } };
		EventHandler<WlPointer.AxisEventArgs> axis = (_, e) => { if (focused) { Trace($"input: pointer axis {e.Axis} value={e.Value.ToDouble():0.##}"); sink.Axis(e.Time, e.Axis, e.Value.ToDouble()); if (proxy.Version < 5) { sink.Frame(); } } };
		EventHandler<WlPointer.AxisDiscreteEventArgs> discrete = (_, e) => { if (focused) { sink.AxisDiscrete(e.Axis, e.Discrete); } };
		EventHandler<WlPointer.AxisValue120EventArgs> value120 = (_, e) => { if (focused) { Trace($"input: pointer axis {e.Axis} value120={e.Value120}"); sink.AxisValue120(e.Axis, e.Value120); } };
		EventHandler<WlPointer.AxisStopEventArgs> stop = (_, e) => { if (focused) { sink.AxisStop(e.Axis); } };
		EventHandler<WlPointer.FrameEventArgs> frame = (_, _) => sink.Frame();
		proxy.Enter += enter;
		proxy.Leave += leave;
		proxy.Motion += motion;
		proxy.Button += button;
		proxy.Axis += axis;
		proxy.AxisDiscrete += discrete;
		proxy.AxisValue120 += value120;
		proxy.AxisStop += stop;
		proxy.Frame += frame;
		EventHandler<WlPointer.ButtonEventArgs> buttonSerial = (_, e) => InputSerial?.Invoke(this, e.Serial);
		proxy.Button += buttonSerial;
		binding.Remove.Add(() => proxy.Button -= buttonSerial);
		binding.Remove.Add(() =>
		{
			PointerReleasing?.Invoke(this, EventArgs.Empty);
			Pointer = null;
			proxy.Enter -= enter;
			proxy.Leave -= leave;
			proxy.Motion -= motion;
			proxy.Button -= button;
			proxy.Axis -= axis;
			proxy.AxisDiscrete -= discrete;
			proxy.AxisValue120 -= value120;
			proxy.AxisStop -= stop;
			proxy.Frame -= frame;
		});
		return binding;
	}
	public IDisposable GetKeyboard(WaylandKeyboardInputSource sink)
	{
		var proxy = _seat.GetKeyboard();
		var binding = new Binding(proxy);
		if (WaylandDiagnostics.IsEnabled)
		{
			WaylandDiagnostics.Trace($"input: wl_keyboard v{proxy.Version} bound");
		}
		EventHandler<WlKeyboard.KeymapEventArgs> keymap = (_, e) =>
		{
			if (WaylandDiagnostics.IsEnabled)
			{
				WaylandDiagnostics.Trace($"input: keymap format={e.Format} size={e.Size}");
			}
			sink.Keymap(e.Format, e.Fd, e.Size);
		};
		EventHandler<WlKeyboard.EnterEventArgs> enter = (_, e) => { if (_ownsSurface(e.Surface)) { Trace($"input: keyboard enter"); sink.Enter(MemoryMarshal.Cast<byte, uint>(e.Keys.AsSpan())); } };
		EventHandler<WlKeyboard.LeaveEventArgs> leave = (_, e) => { if (_ownsSurface(e.Surface)) { Trace($"input: keyboard leave"); sink.Leave(); } };
		EventHandler<WlKeyboard.KeyEventArgs> key = (_, e) => { if (WaylandDiagnostics.IsVerbose) { WaylandDiagnostics.TraceVerbose(FormattableString.Invariant($"input: key {e.Key} state={e.State}")); } sink.Key(e.Key, e.State); };
		EventHandler<WlKeyboard.ModifiersEventArgs> modifiers = (_, e) => { Trace($"input: modifiers depressed=0x{e.ModsDepressed:x} locked=0x{e.ModsLocked:x} group={e.Group}"); sink.UpdateModifiers(e.ModsDepressed, e.ModsLatched, e.ModsLocked, e.Group); };
		EventHandler<WlKeyboard.RepeatInfoEventArgs> repeat = (_, e) => { Trace($"input: repeat rate={e.Rate} delay={e.Delay}"); sink.RepeatInfo(e.Rate, e.Delay); };
		proxy.Keymap += keymap;
		proxy.Enter += enter;
		proxy.Leave += leave;
		proxy.Key += key;
		proxy.Modifiers += modifiers;
		proxy.RepeatInfo += repeat;
		EventHandler<WlKeyboard.EnterEventArgs> enterSerial = (_, e) => InputSerial?.Invoke(this, e.Serial);
		EventHandler<WlKeyboard.KeyEventArgs> keySerial = (_, e) => InputSerial?.Invoke(this, e.Serial);
		proxy.Enter += enterSerial;
		proxy.Key += keySerial;
		binding.Remove.Add(() =>
		{
			proxy.Enter -= enterSerial;
			proxy.Key -= keySerial;
		});
		binding.Remove.Add(() =>
		{
			proxy.Keymap -= keymap;
			proxy.Enter -= enter;
			proxy.Leave -= leave;
			proxy.Key -= key;
			proxy.Modifiers -= modifiers;
			proxy.RepeatInfo -= repeat;
		});
		return binding;
	}
	public IDisposable GetTouch(WaylandPointerInputSource sink)
	{
		var proxy = _seat.GetTouch();
		var binding = new Binding(proxy);
		var ids = new HashSet<int>();
		EventHandler<WlTouch.DownEventArgs> down = (_, e) => { if (_ownsSurface(e.Surface)) { ids.Add(e.Id); InputSerial?.Invoke(this, e.Serial); sink.TouchDown(e.Time, e.Id, e.X.ToDouble(), e.Y.ToDouble()); } };
		EventHandler<WlTouch.UpEventArgs> up = (_, e) => { if (ids.Remove(e.Id)) { InputSerial?.Invoke(this, e.Serial); sink.TouchUp(e.Time, e.Id); } };
		EventHandler<WlTouch.MotionEventArgs> motion = (_, e) => { if (ids.Contains(e.Id)) { sink.TouchMotion(e.Time, e.Id, e.X.ToDouble(), e.Y.ToDouble()); } };
		EventHandler<WlTouch.FrameEventArgs> frame = (_, _) => sink.TouchFrame();
		EventHandler<WlTouch.CancelEventArgs> cancel = (_, _) => { ids.Clear(); sink.TouchCancel(); };
		proxy.Down += down;
		proxy.Up += up;
		proxy.Motion += motion;
		proxy.Frame += frame;
		proxy.Cancel += cancel;
		binding.Remove.Add(() => { proxy.Down -= down; proxy.Up -= up; proxy.Motion -= motion; proxy.Frame -= frame; proxy.Cancel -= cancel; });
		return binding;
	}
	private static void Trace(FormattableString message)
	{
		if (WaylandDiagnostics.IsEnabled)
		{
			WaylandDiagnostics.Trace(FormattableString.Invariant(message));
		}
	}
	private sealed class Binding : IDisposable
	{
		private IDisposable? _proxy;
		public List<Action> Remove { get; } = new();
		public Binding(IDisposable proxy) => _proxy = proxy;
		public void Dispose()
		{
			if (_proxy is not { } proxy)
			{
				return;
			}
			_proxy = null;
			foreach (var remove in Remove)
			{
				remove();
			}
			Remove.Clear();
			proxy.Dispose();
		}
	}
}
