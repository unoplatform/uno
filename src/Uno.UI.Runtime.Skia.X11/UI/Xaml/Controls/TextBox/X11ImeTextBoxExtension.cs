#nullable enable

using System;
using Microsoft.UI.Xaml.Controls;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls.Extensions;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// X11 IME implementation of <see cref="IImeTextBoxExtension"/>.
/// Delegates cursor and focus management to a D-Bus IME (IBus/Fcitx) when one is active.
/// When no D-Bus IME is available, IME is a no-op — basic key input still works via
/// <see cref="X11KeyboardInputSource"/>'s XLookupString path.
/// </summary>
internal sealed class X11ImeTextBoxExtension : IImeTextBoxExtension
{
	internal static X11ImeTextBoxExtension Instance { get; } = new();

	private IntPtr _currentDisplay;
	private IntPtr _currentWindow;
	private bool _isComposing;
	private IX11InputMethod? _dbusIme;

	private X11ImeTextBoxExtension()
	{
	}

	public bool IsComposing => _isComposing;

	public event EventHandler? CompositionStarted;
	public event EventHandler<ImeCompositionEventArgs>? CompositionUpdated;
	public event EventHandler<ImeCompositionEventArgs>? CompositionCompleted;
	public event EventHandler? CompositionEnded;

	public void StartImeSession(TextBox textBox)
	{
		_currentDisplay = IntPtr.Zero;
		_currentWindow = IntPtr.Zero;
		_dbusIme = null;

		if (textBox.XamlRoot is not { } xamlRoot)
		{
			return;
		}

		if (XamlRootMap.GetHostForRoot(xamlRoot) is not X11XamlRootHost host)
		{
			return;
		}

		var rootWindow = host.RootX11Window;
		_currentDisplay = rootWindow.Display;
		_currentWindow = rootWindow.Window;

		_dbusIme = host.GetKeyboardSource()?.GetDBusIme();

		if (_dbusIme?.IsEnabled == true)
		{
			_dbusIme.SetFocus(true);
			UpdateSpotLocationFromTextBox(textBox);

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug("IME session started with D-Bus IME backend.");
			}
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

		_currentDisplay = IntPtr.Zero;
		_currentWindow = IntPtr.Zero;
		_dbusIme = null;
	}

	/// <summary>
	/// Called from <see cref="X11KeyboardInputSource"/> when the D-Bus IME emits Commit.
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
	/// Computes and sends the caret location to the active D-Bus IME so its candidate
	/// window tracks the caret.
	/// </summary>
	internal void UpdateSpotLocationFromTextBox(TextBox textBox)
	{
		if (_dbusIme?.IsEnabled != true)
		{
			return;
		}

		var textBoxView = textBox.TextBoxView;
		if (textBoxView?.DisplayBlock?.ParsedText is null || textBox.XamlRoot is null)
		{
			return;
		}

		var index = textBox.IsBackwardSelection ? textBox.SelectionStart : textBox.SelectionStart + textBox.SelectionLength;
		var rect = textBoxView.DisplayBlock.ParsedText.GetRectForIndex(index);
		var transform = textBoxView.DisplayBlock.TransformToVisual(null);
		var topLeft = transform.TransformPoint(new Windows.Foundation.Point(rect.Left, rect.Top));
		var scale = textBox.XamlRoot.RasterizationScale;

		var x = (int)(topLeft.X * scale);
		var y = (int)(topLeft.Y * scale);
		var height = (int)(rect.Height * scale);

		// IBus/Fcitx SetCursorLocation expects coordinates in absolute root-window
		// (screen) pixels, while the values above are window-local. Translate through
		// the X server so the popup lines up with the actual content area (not the WM
		// decoration frame).
		var screenX = x;
		var screenY = y;
		if (_currentDisplay != IntPtr.Zero && _currentWindow != IntPtr.Zero)
		{
			using (X11Helper.XLock(_currentDisplay))
			{
				var root = XLib.XDefaultRootWindow(_currentDisplay);
				_ = XLib.XTranslateCoordinates(
					_currentDisplay, _currentWindow, root,
					x, y,
					out screenX, out screenY,
					out _);
			}
		}

		// IBus/Fcitx treat (x, y, w, h) as the caret rect and place the candidate
		// popup at (x, y + h). Passing the caret top + line height anchors the popup
		// at the line's bottom — flush below the text the user is composing.
		_dbusIme.SetCursorLocation(screenX, screenY, 1, Math.Max(1, height));
	}
}
