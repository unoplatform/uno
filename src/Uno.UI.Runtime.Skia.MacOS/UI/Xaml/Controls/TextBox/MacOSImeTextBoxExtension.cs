#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
	private IImeSessionHost? _activeTextBox;
	private nint _activeWindowHandle;
	private Rect _lastCaretRect = Rect.Empty;

	public bool IsComposing => _isComposing;

	public event EventHandler? CompositionStarted;
	public event EventHandler<ImeCompositionEventArgs>? CompositionUpdated;
	public event EventHandler<ImeCompositionEventArgs>? CompositionCompleted;
	public event EventHandler<ImePartialCompositionEventArgs>? CompositionPartiallyCommitted
	{
		add { }
		remove { }
	}
	public event EventHandler<ImeCompositionEventArgs>? CompositionCanceled;
	public event EventHandler? CompositionEnded;

	public void StartImeSession(IImeSessionHost host, ImeSessionActivation activation)
	{
		// Don't wire up composition events for PasswordBox — IME composition
		// reveals characters, which is not appropriate for password fields.
		if (host is PasswordBox)
		{
			return;
		}

		_activeTextBox = host;
		_lastCaretRect = Rect.Empty;

		// Find the native window handle to activate IME routing on the native view
		_activeWindowHandle = MacOSWindowHost.GetNativeHandleForXamlRoot(host.XamlRoot);
		if (_activeWindowHandle != 0)
		{
			NativeUno.uno_set_ime_active(_activeWindowHandle, true);
		}

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"IME session started. Window: {_activeWindowHandle}");
		}
	}

	public void UpdateImeSession(IImeSessionHost host, ImeSessionUpdate update)
	{
		if (_activeWindowHandle != 0
			&& (update & (ImeSessionUpdate.CandidateWindowAlignment | ImeSessionUpdate.TextAndSelection)) != 0)
		{
			if ((update & ImeSessionUpdate.CandidateWindowAlignment) != 0)
			{
				_lastCaretRect = Rect.Empty;
			}
			var caretRect = GetCaretRect();
			if (caretRect != Rect.Empty && !caretRect.Equals(_lastCaretRect))
			{
				_lastCaretRect = caretRect;
				NativeUno.uno_notify_ime_position_changed(_activeWindowHandle);
			}
		}
	}

	public Task<IReadOnlyList<string>> GetLinguisticAlternativesAsync(string compositionText, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
	}

	public event EventHandler<ImeCandidateWindowBoundsChangedEventArgs>? CandidateWindowBoundsChanged
	{
		add { }
		remove { }
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
		_lastCaretRect = Rect.Empty;
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
			CompositionCanceled?.Invoke(this, new ImeCompositionEventArgs(string.Empty));
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
		return _activeTextBox?.TryGetCandidateWindowRect(out var rect) == true
			? rect
			: Rect.Empty;
	}
}
