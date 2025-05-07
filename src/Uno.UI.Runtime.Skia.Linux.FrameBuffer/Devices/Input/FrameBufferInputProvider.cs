#nullable enable

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
using Uno.UI.Runtime.Skia;

namespace Uno.WinUI.Runtime.Skia.Linux.FrameBuffer;

unsafe internal class FrameBufferInputProvider : IDisposable
{
	private readonly CancellationTokenSource _cts = new CancellationTokenSource();

	private bool _initialized;
	private IntPtr _libInputContext;
	private Thread? _inputThread;
	private FrameBufferPointerInputSource? _pointers;
	private int _libDevFd;

	private FrameBufferInputProvider()
	{
	}

	public static FrameBufferInputProvider Instance { get; } = new FrameBufferInputProvider();

	internal void Initialize()
	{
		if (_initialized)
		{
			return;
		}

		_initialized = true;
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
					FrameBufferKeyboardInputSource.Instance.ProcessKeyboardEvent(rawEvent, type);
				}

				libinput_event_destroy(rawEvent);
				libinput_dispatch(_libInputContext);
			}

			var pfd = new pollfd { fd = _libDevFd, events = 1 };
#pragma warning disable CA1806 // Do not ignore method results
			Libc.poll(&pfd, (IntPtr)1, -1);
#pragma warning restore CA1806 // Do not ignore method results
		}
	}

	public void Dispose()
	{
		if (_libDevFd != 0)
		{
#pragma warning disable CA1806 // Do not ignore method results
			Libc.close(_libDevFd);
#pragma warning restore CA1806 // Do not ignore method results
			_libDevFd = 0;

			_cts.Cancel();
		}
	}


	private bool TryGetPointers(/*[NotNullWhen(true)] */out FrameBufferPointerInputSource? pointers)
	{
		if (_pointers is null)
		{
			_pointers = FrameBufferPointerInputSource.Instance;
			_pointers.Configure(FrameBufferKeyboardInputSource.Instance.GetCurrentModifiersState);
		}

		pointers = _pointers;
		return true;
	}
}
