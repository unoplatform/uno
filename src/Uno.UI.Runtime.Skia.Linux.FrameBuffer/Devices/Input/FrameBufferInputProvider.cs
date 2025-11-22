#nullable enable

using System;
using System.IO;
using System.Threading;
using Uno.UI.Runtime.Skia.Native;
using static Uno.UI.Runtime.Skia.Native.LibInput;
using static Uno.UI.Runtime.Skia.Native.libinput_event_type;
using System.Runtime.InteropServices;
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

		if (Directory.Exists("/dev/input"))
		{
			foreach (var f in Directory.GetFiles("/dev/input", "event*"))
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Opening input device {f}");
				}

				var ret = libinput_path_add_device(_libInputContext, f);
				if (ret == IntPtr.Zero)
				{
					this.LogError()?.Error($"{nameof(libinput_path_add_device)} failed on device {f}");
				}
			}
		}

		while (!_cts.IsCancellationRequested)
		{
			IntPtr rawEvent;
			var ret = libinput_dispatch(_libInputContext);
			if (ret < 0)
			{
				throw new InvalidOperationException($"{nameof(libinput_dispatch)} failed with error {ret}");
			}
			while ((rawEvent = libinput_get_event(_libInputContext)) != IntPtr.Zero)
			{
				var type = libinput_event_get_type(rawEvent);

				this.LogTrace()?.Trace($"Got event type (0x{rawEvent:X16}) {type}");

				if (type is >= LIBINPUT_EVENT_TOUCH_DOWN and <= LIBINPUT_EVENT_TOUCH_CANCEL
					&& TryGetPointers(out var pointers))
				{
					pointers!.ProcessTouchEvent(rawEvent, type);
				}

				if (type is >= LIBINPUT_EVENT_POINTER_MOTION and <= LIBINPUT_EVENT_POINTER_AXIS
					&& TryGetPointers(out pointers))
				{
					pointers!.ProcessMouseEvent(rawEvent, type);
				}

				if (type == LIBINPUT_EVENT_KEYBOARD_KEY)
				{
					FrameBufferKeyboardInputSource.Instance.ProcessKeyboardEvent(rawEvent, type);
				}

				libinput_event_destroy(rawEvent);
				var ret2 = libinput_dispatch(_libInputContext);
				if (ret2 != 0)
				{
					this.LogError()?.Error($"{nameof(libinput_dispatch)} failed with error {ret2}");
				}
			}

			var pfd = new pollfd { fd = _libDevFd, events = 1 };
			var ret3 = Libc.poll(&pfd, Libc.PROT_READ, -1);
			if (ret3 < 0 && this.Log().IsEnabled(LogLevel.Error))
			{
				var errno = Marshal.GetLastWin32Error();
				var errnoStringPtr = Libc.strerror(errno);
				var errorString = Marshal.PtrToStringAnsi(errnoStringPtr);
				this.Log().Error($"{nameof(Libc.poll)} failed ({errno}) : {errorString}");
			}
		}
	}

	public void Dispose()
	{
		if (_libDevFd != 0)
		{
			var ret = Libc.close(_libDevFd);
			if (ret < 0 && this.Log().IsEnabled(LogLevel.Error))
			{
				var errno = Marshal.GetLastWin32Error();
				var errnoStringPtr = Libc.strerror(errno);
				var errorString = Marshal.PtrToStringAnsi(errnoStringPtr);
				this.Log().Error($"{nameof(Libc.close)} failed ({errno}) : {errorString}");
			}
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
