#nullable enable

using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Controls;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Uno.UI.NativeElementHosting;
using Uno.UI.Xaml.Controls.Extensions;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// X11 IME implementation of <see cref="IImeTextBoxExtension"/>.
/// When a D-Bus IME (IBus/Fcitx) is active, delegates cursor and focus management to it.
/// Falls back to XIM (XOpenIM/XCreateIC) when no D-Bus IME is available.
/// </summary>
internal sealed class X11ImeTextBoxExtension : IImeTextBoxExtension
{
	internal static X11ImeTextBoxExtension Instance { get; } = new();

	private static IntPtr _xim;
	private static readonly ConcurrentDictionary<IntPtr, IntPtr> _windowToXic = new();

	// The host for dispatching UI-thread actions.
	private X11XamlRootHost? _currentHost;

	// Pending spot location to be applied from the event thread (XIM only).
	private volatile bool _spotLocationPending;
	private short _pendingSpotX;
	private short _pendingSpotY;

	private IntPtr _currentDisplay;
	private IntPtr _currentWindow;
	private IntPtr _currentXic;
	private bool _isComposing;

	// D-Bus IME reference (set during StartImeSession)
	private IX11InputMethod? _dbusIme;

	private X11ImeTextBoxExtension()
	{
	}

	public bool IsComposing => _isComposing;

	public event EventHandler? CompositionStarted;
	public event EventHandler<ImeCompositionEventArgs>? CompositionUpdated;
	public event EventHandler<ImeCompositionEventArgs>? CompositionCompleted;
	public event EventHandler? CompositionEnded;

	/// <summary>
	/// Gets the XIC for the given window, or IntPtr.Zero if none exists.
	/// Called from <see cref="X11KeyboardInputSource"/> to use Xutf8LookupString.
	/// </summary>
	internal static IntPtr GetXicForWindow(IntPtr window)
	{
		_windowToXic.TryGetValue(window, out var xic);
		return xic;
	}

	public void StartImeSession(TextBox textBox)
	{
		_currentDisplay = IntPtr.Zero;
		_currentWindow = IntPtr.Zero;
		_currentXic = IntPtr.Zero;
		_currentHost = null;
		_dbusIme = null;

		if (textBox.XamlRoot is not { } xamlRoot)
		{
			return;
		}

		if (XamlRootMap.GetHostForRoot(xamlRoot) is not X11XamlRootHost host)
		{
			return;
		}

		_currentHost = host;
		var rootWindow = host.RootX11Window;
		_currentDisplay = rootWindow.Display;
		_currentWindow = rootWindow.Window;

		// Check if D-Bus IME is active from the keyboard source
		var keyboardSource = host.GetKeyboardSource();
		_dbusIme = keyboardSource?.GetDBusIme();

		if (_dbusIme?.IsEnabled == true)
		{
			// D-Bus IME is active — notify it of focus and send initial cursor location
			_dbusIme.SetFocus(true);
			UpdateSpotLocationFromTextBox(textBox);

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug("IME session started with D-Bus IME backend.");
			}
			return;
		}

		// XIM fallback path
		using (X11Helper.XLock(_currentDisplay))
		{
			if (_xim == IntPtr.Zero)
			{
				_xim = XLib.XOpenIM(_currentDisplay, IntPtr.Zero, null, null);
				if (_xim == IntPtr.Zero)
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().Debug("XOpenIM returned null — no input method available. IME will be disabled.");
					}
					return;
				}
			}

			if (!_windowToXic.TryGetValue(_currentWindow, out _currentXic))
			{
				_currentXic = XLib.XCreateIC(_xim,
					XLib.XNInputStyle, (IntPtr)(XLib.XIMPreeditNothing | XLib.XIMStatusNothing),
					XLib.XNClientWindow, _currentWindow,
					XLib.XNFocusWindow, _currentWindow,
					IntPtr.Zero);

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"XCreateIC with XIMPreeditNothing: {(_currentXic != IntPtr.Zero ? "succeeded" : "failed")}");
				}

				if (_currentXic == IntPtr.Zero)
				{
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().Warn("XCreateIC failed — IME will be disabled for this window.");
					}
					return;
				}

				_windowToXic[_currentWindow] = _currentXic;
			}

			XLib.XSetICFocus(_currentXic);
		}
	}

	public void EndImeSession()
	{
		if (_isComposing)
		{
			// Implicitly commit the current preedit text (matches WinUI behavior
			// where losing focus commits the active composition).
			_isComposing = false;
			CompositionEnded?.Invoke(this, EventArgs.Empty);
		}

		if (_dbusIme?.IsEnabled == true)
		{
			_dbusIme.SetFocus(false);
			_dbusIme.Reset();
		}
		else if (_currentXic != IntPtr.Zero && _currentDisplay != IntPtr.Zero)
		{
			using (X11Helper.XLock(_currentDisplay))
			{
				XLib.XUnsetICFocus(_currentXic);
			}
		}

		_dbusIme = null;
		_currentDisplay = IntPtr.Zero;
		_currentWindow = IntPtr.Zero;
		_currentXic = IntPtr.Zero;
		_currentHost = null;
	}

	/// <summary>
	/// Called from <see cref="X11KeyboardInputSource"/> when Xutf8LookupString
	/// returns committed text (XLookupChars or XLookupBoth).
	/// </summary>
	internal void OnCommittedText(string text)
	{
		if (!_isComposing)
		{
			// Direct commit without prior composition (e.g., single-key IME commit)
			CompositionStarted?.Invoke(this, EventArgs.Empty);
		}

		CompositionCompleted?.Invoke(this, new ImeCompositionEventArgs(text));
		_isComposing = false;
		CompositionEnded?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>
	/// Called from the event loop when XFilterEvent consumes a KeyPress,
	/// indicating the IME is composing.
	/// </summary>
	internal void OnComposing()
	{
		if (!_isComposing)
		{
			_isComposing = true;
			CompositionStarted?.Invoke(this, EventArgs.Empty);
		}
	}

	/// <summary>
	/// Called from the keyboard source when the D-Bus IME reports preedit text changes.
	/// The preedit text contains the current composition preview — for CJK IMEs this may be
	/// candidate characters (e.g. "阿波"), raw pinyin with separators (e.g. "a b|c"), or a
	/// mix of resolved characters and remaining input (e.g. "阿伯丁c" after partial commit).
	/// </summary>
	internal void OnPreeditChanged(string? preeditText, int cursorPos)
	{
		if (!string.IsNullOrEmpty(preeditText))
		{
			if (!_isComposing)
			{
				_isComposing = true;
				CompositionStarted?.Invoke(this, EventArgs.Empty);
			}
			CompositionUpdated?.Invoke(this, new ImeCompositionEventArgs(preeditText, cursorPos));
		}
		else if (_isComposing)
		{
			// Preedit cleared without an explicit CommitText signal (e.g., IBus cancelled
			// the composition via Escape or backspace). Fire CompositionCompleted with empty
			// text so the TextBox removes the preedit region, then end the composition.
			CompositionCompleted?.Invoke(this, new ImeCompositionEventArgs(string.Empty));
			_isComposing = false;
			CompositionEnded?.Invoke(this, EventArgs.Empty);
		}
	}

	/// <summary>
	/// Computes and sends the cursor location from the given TextBox to the D-Bus IME.
	/// Called during <see cref="StartImeSession"/> to ensure the candidate window
	/// is positioned correctly before the first keystroke.
	/// </summary>
	private void UpdateSpotLocationFromTextBox(TextBox textBox)
	{
		var textBoxView = textBox.TextBoxView;
		if (textBoxView?.DisplayBlock?.ParsedText is null || textBox.XamlRoot is null)
		{
			return;
		}

		var index = textBox.IsBackwardSelection ? textBox.SelectionStart : textBox.SelectionStart + textBox.SelectionLength;
		var rect = textBoxView.DisplayBlock.ParsedText.GetRectForIndex(index);
		var transform = textBoxView.DisplayBlock.TransformToVisual(null);
		var point = transform.TransformPoint(new Windows.Foundation.Point(rect.Left, rect.Top + rect.Height));
		var scale = textBox.XamlRoot.RasterizationScale;

		UpdateSpotLocation((int)(point.X * scale), (int)(point.Y * scale));
	}

	/// <summary>
	/// Stores the desired spot location. When D-Bus IME is active, updates the cursor
	/// location directly. For XIM fallback, the actual XSetICValues call is deferred
	/// to the event thread via <see cref="FlushPendingSpotLocation"/>.
	/// </summary>
	internal void UpdateSpotLocation(int x, int y)
	{
		if (_dbusIme?.IsEnabled == true)
		{
			// D-Bus IME: update cursor location directly (thread-safe D-Bus call)
			_dbusIme.SetCursorLocation(x, y, 1, 20);
			return;
		}

		// XIM fallback: defer to event thread. Clamp to short range for XPoint struct.
		_pendingSpotX = (short)Math.Clamp(x, short.MinValue, short.MaxValue);
		_pendingSpotY = (short)Math.Clamp(y, short.MinValue, short.MaxValue);
		_spotLocationPending = true;
	}

	/// <summary>
	/// Applies any pending spot location update. Must be called from the X11 event thread
	/// (e.g., during <see cref="X11KeyboardInputSource.ProcessKeyboardEvent"/>).
	/// </summary>
	internal void FlushPendingSpotLocation()
	{
		if (!_spotLocationPending || _currentXic == IntPtr.Zero)
		{
			return;
		}

		_spotLocationPending = false;

		// Allocate XPoint on unmanaged heap — XVaCreateNestedList is varargs
		// and ref parameters don't work reliably with varargs P/Invoke.
		var pointPtr = Marshal.AllocHGlobal(Marshal.SizeOf<XPoint>());
		try
		{
			Marshal.StructureToPtr(new XPoint { X = _pendingSpotX, Y = _pendingSpotY }, pointPtr, false);

			using var lockDisposable = X11Helper.XLock(_currentDisplay);

			var preeditAttr = XLib.XVaCreateNestedList(0,
				XLib.XNSpotLocation, pointPtr,
				IntPtr.Zero);

			if (preeditAttr != IntPtr.Zero)
			{
				XLib.XSetICValues(_currentXic,
					XLib.XNPreeditAttributes, preeditAttr,
					IntPtr.Zero);
				_ = XLib.XFree(preeditAttr);
			}
		}
		finally
		{
			Marshal.FreeHGlobal(pointPtr);
		}
	}
}
