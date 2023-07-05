using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using Uno.Extensions;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;
using Uno.UI.Runtime.Skia.Native;
using static Uno.UI.Runtime.Skia.Native.LibInput;
using static Windows.UI.Input.PointerUpdateKind;
using static Uno.UI.Runtime.Skia.Native.libinput_event_type;
using System.Runtime.CompilerServices;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia;

unsafe internal partial class CoreWindowExtension : ICoreWindowExtension, IDisposable
{
	private readonly CoreWindow _owner;
	private readonly IntPtr _libInputContext;
	private readonly Thread? _inputThread;
	private FrameBufferPointerInputSource? _pointers;
	private int _libDevFd;
	private readonly CancellationTokenSource _cts = new CancellationTokenSource();

	public CoreWindowExtension(object owner)
	{
		_owner = (CoreWindow)owner;

		try
		{
			_libInputContext = libinput_path_create_context();

			_inputThread = new Thread(Run)
			{
				Name = "Uno libdev Input",
				IsBackground = true
			};

			_inputThread.Start();
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning($"Failed to initialize LibInput, continuing without pointer and keyboard support ({ex.Message})");
			}
		}
	}

	private void Run()
	{
		_libDevFd = libinput_get_fd(_libInputContext);

		var timeval = stackalloc IntPtr[2];

		if (Directory.Exists("/dev/input"))
		{
			foreach (var f in Directory.GetFiles("/dev/input", "event*"))
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Opening input device {f}");
				}

				libinput_path_add_device(_libInputContext, f);
			}
		}

		while (!_cts.IsCancellationRequested)
		{
			IntPtr rawEvent;
			libinput_dispatch(_libInputContext);
			while ((rawEvent = libinput_get_event(_libInputContext)) != IntPtr.Zero)
			{
				var type = libinput_event_get_type(rawEvent);

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"Got event type (0x{rawEvent:X16}) {type}");
				}

				if (type >= LIBINPUT_EVENT_TOUCH_DOWN
					&& type <= LIBINPUT_EVENT_TOUCH_CANCEL
					&& TryGetPointers(out var pointers))
				{
					pointers!.ProcessTouchEvent(rawEvent, type);
				}

				if (type >= LIBINPUT_EVENT_POINTER_MOTION
					&& type <= LIBINPUT_EVENT_POINTER_AXIS
					&& TryGetPointers(out pointers))
				{
					pointers!.ProcessMouseEvent(rawEvent, type);
				}

				if (type == LIBINPUT_EVENT_KEYBOARD_KEY)
				{
					ProcessKeyboardEvent(rawEvent, type);
				}

				libinput_event_destroy(rawEvent);
				libinput_dispatch(_libInputContext);
			}

			var pfd = new pollfd { fd = _libDevFd, events = 1 };
			Libc.poll(&pfd, (IntPtr)1, -1);
		}
	}

	public void Dispose()
	{
		if (_libDevFd != 0)
		{
			Libc.close(_libDevFd);
			_libDevFd = 0;

			_cts.Cancel();
		}
	}

	public bool IsNativeElement(object content)
		=> false;
	public void AttachNativeElement(object owner, object content) { }
	public void DetachNativeElement(object owner, object content) { }
	public void ArrangeNativeElement(object owner, object content, Rect arrangeRect) { }
	public Size MeasureNativeElement(object owner, object content, Size size)
		=> size;

	private bool TryGetPointers(/*[NotNullWhen(true)] */out FrameBufferPointerInputSource? pointers)
	{
		if (_pointers is null)
		{
			_pointers = _owner.PointersSource as FrameBufferPointerInputSource;
			if (_pointers is null)
			{
				pointers = null;
				return false;
			}

			_pointers.Configure(_owner, GetCurrentModifiersState);
		}

		pointers = _pointers;
		return true;
	}
}
