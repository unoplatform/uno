#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.Foundation;

namespace Uno.UI.Runtime.Skia.MacOS;

/// <summary>
/// macOS Skia implementation of <see cref="IImeTextBoxExtension"/>.
/// Bridges macOS NSTextInputClient composition callbacks (setMarkedText/insertText/unmarkText)
/// to the managed TextBox composition event lifecycle (Started → Updated → Completed → Ended).
/// </summary>
internal sealed class MacOSImeTextBoxExtension : IImeTextBoxExtension
{
	internal static MacOSImeTextBoxExtension Instance { get; } = new();

	private bool _isComposing;
	private string _lastComposingText = string.Empty;
	private TextBox? _activeTextBox;
	private nint _activeWindowHandle;

	public bool IsComposing => _isComposing;

	public event EventHandler? CompositionStarted;
	public event EventHandler<ImeCompositionEventArgs>? CompositionUpdated;
	public event EventHandler<ImeCompositionEventArgs>? CompositionCompleted;
	public event EventHandler? CompositionEnded;

	public void StartImeSession(TextBox textBox)
	{
		// Don't wire up composition events for PasswordBox — IME composition
		// reveals characters, which is not appropriate for password fields.
		if (textBox is PasswordBox)
		{
			return;
		}

		_activeTextBox = textBox;

		// Find the native window handle to activate IME routing on the native view
		_activeWindowHandle = MacOSWindowHost.GetNativeHandleForXamlRoot(textBox.XamlRoot);
		if (_activeWindowHandle != 0)
		{
			NativeUno.uno_set_ime_active(_activeWindowHandle, true);
		}

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"IME session started. Window: {_activeWindowHandle}");
		}
	}

	public void EndImeSession()
	{
		if (_activeWindowHandle != 0)
		{
			NativeUno.uno_set_ime_active(_activeWindowHandle, false);
		}

		if (_isComposing)
		{
			_isComposing = false;
			_lastComposingText = string.Empty;
			CompositionEnded?.Invoke(this, EventArgs.Empty);
		}

		_activeTextBox = null;
		_activeWindowHandle = 0;
	}

	/// <summary>
	/// Called from native via P/Invoke when NSTextInputClient.setMarkedText is invoked.
	/// </summary>
	internal void OnSetMarkedText(string text, int selectedStart, int selectedLength)
	{
		bool wasComposing = _isComposing;

		if (text.Length > 0)
		{
			if (!wasComposing)
			{
				// Transition: Idle → Composing
				_isComposing = true;
				_lastComposingText = text;

				CompositionStarted?.Invoke(this, EventArgs.Empty);
				CompositionUpdated?.Invoke(this, new ImeCompositionEventArgs(text, selectedStart));

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"Composition started: '{text}'");
				}
			}
			else
			{
				// Transition: Composing → Composing (preedit update)
				_lastComposingText = text;
				CompositionUpdated?.Invoke(this, new ImeCompositionEventArgs(text, selectedStart));

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"Composition updated: '{text}'");
				}
			}
		}
		else if (wasComposing)
		{
			// Empty marked text while composing = cancel
			_isComposing = false;
			_lastComposingText = string.Empty;
			CompositionEnded?.Invoke(this, EventArgs.Empty);

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace("Composition cancelled (empty marked text)");
			}
		}
	}

	/// <summary>
	/// Called from native via P/Invoke when NSTextInputClient.insertText is invoked.
	/// </summary>
	internal void OnInsertText(string text)
	{
		bool wasComposing = _isComposing;

		if (!wasComposing)
		{
			// Direct commit without prior composition (e.g., single-key IME commit,
			// or typing a character that doesn't trigger composition like punctuation).
			// Fire the full Started → Completed → Ended cycle so TextBox processes it.
			CompositionStarted?.Invoke(this, EventArgs.Empty);
		}

		_isComposing = false;
		_lastComposingText = string.Empty;

		CompositionCompleted?.Invoke(this, new ImeCompositionEventArgs(text));
		CompositionEnded?.Invoke(this, EventArgs.Empty);

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Composition committed: '{text}' (wasComposing: {wasComposing})");
		}
	}

	/// <summary>
	/// Called from native via P/Invoke when NSTextInputClient.unmarkText is invoked.
	/// </summary>
	internal void OnUnmarkText()
	{
		if (_isComposing)
		{
			_isComposing = false;
			_lastComposingText = string.Empty;
			CompositionEnded?.Invoke(this, EventArgs.Empty);

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace("Composition ended (unmark)");
			}
		}
	}

	/// <summary>
	/// Returns the caret rectangle in view coordinates for candidate window positioning.
	/// Called from native via P/Invoke when NSTextInputClient.firstRectForCharacterRange is invoked.
	/// </summary>
	internal Rect GetCaretRect()
	{
		if (_activeTextBox is { TextBoxView.DisplayBlock.ParsedText: { } parsedText, XamlRoot: { } xamlRoot })
		{
			var selEnd = _activeTextBox.SelectionStart + _activeTextBox.SelectionLength;
			var caretRect = parsedText.GetRectForIndex(selEnd);
			var transform = _activeTextBox.TextBoxView.DisplayBlock.TransformToVisual(null);
			var caretPoint = transform.TransformPoint(
				new Windows.Foundation.Point(caretRect.Left, caretRect.Top));
			var caretBottom = transform.TransformPoint(
				new Windows.Foundation.Point(caretRect.Left, caretRect.Top + caretRect.Height));

			// Return in logical (view) coordinates — the native side converts to screen coordinates
			return new Rect(caretPoint.X, caretPoint.Y, 1, caretBottom.Y - caretPoint.Y);
		}

		return Rect.Empty;
	}
}
