using System;

namespace Uno.WinUI.Runtime.Skia.X11;

internal static partial class X11Helper
{
	// Xlib's default error handler terminates the whole process when an X protocol error is
	// delivered (for example a BadValue from X_GLXCreateNewContext on some drivers). That makes
	// it impossible to fall back to another rendering backend, since the process dies before any
	// managed catch block runs.
	//
	// Rather than replacing the handler process-wide (which would change behavior for every other
	// X call), we install our handler only for the duration of a TrapErrors scope and restore the
	// previously-active handler afterwards. Inside the scope errors are recorded for the caller to
	// inspect; outside it Xlib's default behavior is unchanged.
	// cf. https://github.com/unoplatform/uno/issues/23338

	// Kept alive for the process lifetime so Xlib can safely hold a native function pointer to it
	// while a trap is active.
	private static readonly XErrorHandler _trapHandler = HandleTrappedError;

	// The error handler is invoked synchronously on the thread that triggers the round-trip
	// (typically an XSync), so the trap state is tracked per-thread.
	[ThreadStatic] private static int _errorTrapDepth;
	[ThreadStatic] private static bool _trappedErrorOccurred;
	[ThreadStatic] private static byte _trappedErrorCode;
	[ThreadStatic] private static XRequest _trappedErrorRequest;
	[ThreadStatic] private static byte _trappedErrorMinorCode;

	private static int HandleTrappedError(IntPtr display, ref XErrorEvent error)
	{
		if (_errorTrapDepth > 0)
		{
			_trappedErrorOccurred = true;
			_trappedErrorCode = error.error_code;
			_trappedErrorRequest = error.request_code;
			_trappedErrorMinorCode = error.minor_code;
		}

		// The return value is ignored by Xlib. The important part is that we don't chain to the
		// default handler, which would abort the process.
		return 0;
	}

	/// <summary>
	/// Begins a scope during which X protocol errors raised on the current thread are recorded
	/// instead of aborting the process, allowing the caller to detect failures (such as GLX context
	/// creation) that Xlib reports asynchronously. The previously-active error handler is restored
	/// when the returned value is disposed.
	/// </summary>
	public static X11ErrorTrap TrapErrors() => new X11ErrorTrap();

	internal ref struct X11ErrorTrap
	{
		private readonly IntPtr _previousHandler;
		private bool _disposed;

		public X11ErrorTrap()
		{
			if (_errorTrapDepth++ == 0)
			{
				_trappedErrorOccurred = false;
			}
			_previousHandler = XLib.XSetErrorHandler(_trapHandler);
		}

		/// <summary>Whether an X protocol error has been recorded since this trap began.</summary>
		public readonly bool HasError => _trappedErrorOccurred;

		/// <summary>A human-readable description of the last trapped error.</summary>
		public readonly string ErrorText =>
			$"error_code={_trappedErrorCode}, request_code={_trappedErrorRequest}, minor_code={_trappedErrorMinorCode}";

		/// <summary>
		/// Flushes pending requests with <see cref="XLib.XSync"/> so that any asynchronous protocol
		/// errors are delivered to the handler, then returns whether an error was trapped.
		/// </summary>
		public readonly bool SyncAndHasError(IntPtr display)
		{
			_ = XLib.XSync(display, false);
			return _trappedErrorOccurred;
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;
				_errorTrapDepth--;
				// Restore whatever handler was active before this trap (Xlib's default, or an outer trap).
				_ = XLib.XSetErrorHandlerRaw(_previousHandler);
			}
		}
	}
}
