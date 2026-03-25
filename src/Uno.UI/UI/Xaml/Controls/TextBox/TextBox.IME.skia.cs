#nullable enable
using System;
using Windows.Foundation;
using Uno.Disposables;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls.Extensions;

namespace Microsoft.UI.Xaml.Controls;

public partial class TextBox
{
	private static IImeTextBoxExtension? _imeExtension;

	private static TextBox? _activeImeTextBox;
	private bool _isComposing;
	private int _compositionStartIndex;
	private int _compositionLength;
	private int _compositionResolvedLength;

	public event TypedEventHandler<TextBox, TextCompositionStartedEventArgs>? TextCompositionStarted;
	public event TypedEventHandler<TextBox, TextCompositionChangedEventArgs>? TextCompositionChanged;
	public event TypedEventHandler<TextBox, TextCompositionEndedEventArgs>? TextCompositionEnded;

	internal bool IsComposing => _isComposing;
	internal int CompositionStartIndex => _compositionStartIndex;
	internal int CompositionLength => _compositionLength;

	/// <summary>
	/// The underline range within the composition, starting after any already-resolved characters.
	/// Used by the renderer to only underline the active (unresolved) portion of the preedit.
	/// </summary>
	internal int CompositionUnderlineStart => _compositionStartIndex + _compositionResolvedLength;
	internal int CompositionUnderlineLength => Math.Max(0, _compositionLength - _compositionResolvedLength);

	private static void InitializeIme()
	{
		if (!ApiExtensibility.CreateInstance<IImeTextBoxExtension>(typeof(TextBox), out var imeExtension))
		{
			typeof(TextBox).LogDebug()?.Debug("No IME extension registered or registration returned null, IME composition will not be supported.");
			return;
		}
		imeExtension.CompositionStarted += static (_, _) => _activeImeTextBox?.OnImeCompositionStarted();
		imeExtension.CompositionUpdated += static (_, e) => _activeImeTextBox?.OnImeCompositionUpdated(e.Text, e.CursorPosition, e.ResolvedLength);
		imeExtension.CompositionCompleted += static (_, e) => _activeImeTextBox?.OnImeCompositionCompleted(e.Text);
		imeExtension.CompositionEnded += static (_, _) => _activeImeTextBox?.OnImeCompositionEnded();
		_imeExtension = imeExtension;
	}

	private void StartImeSession()
	{
		_activeImeTextBox = this;
		_imeExtension?.StartImeSession(this);
	}

	private void EndImeSession()
	{
		_imeExtension?.EndImeSession();
		_activeImeTextBox = null;
	}

	private void OnImeCompositionStarted()
	{
		if (IsReadOnly)
		{
			return;
		}

		_isComposing = true;
		_compositionStartIndex = SelectionStart;
		_compositionLength = 0;
		_compositionResolvedLength = 0;

		TextCompositionStarted?.Invoke(this, new TextCompositionStartedEventArgs(_compositionStartIndex, 0));
	}

	private void OnImeCompositionUpdated(string compositionText, int cursorPosition, int resolvedLength)
	{
		if (IsReadOnly)
		{
			return;
		}

		ReplaceCompositionText(compositionText, cursorPosition);
		_compositionLength = compositionText.Length;
		_compositionResolvedLength = resolvedLength;

		TextCompositionChanged?.Invoke(this, new TextCompositionChangedEventArgs(_compositionStartIndex, _compositionLength));
		InvalidateTextBoxRender();
	}

	private void OnImeCompositionCompleted(string committedText)
	{
		if (IsReadOnly)
		{
			return;
		}

		TrySetCurrentlyTyping(true);
		ReplaceCompositionText(committedText);

		var startIndex = _compositionStartIndex;
		var committedLength = committedText.Length;
		_isComposing = false;
		_compositionLength = 0;
		_compositionStartIndex = 0;
		_compositionResolvedLength = 0;

		TextCompositionEnded?.Invoke(this, new TextCompositionEndedEventArgs(startIndex, committedLength));
		InvalidateTextBoxRender();
	}

	private void OnImeCompositionEnded()
	{
		if (!_isComposing)
		{
			return;
		}

		// Composition ended without explicit commit — keep text as-is (matches WinUI behavior).
		// The composition text was already inserted via ProcessTextInput during OnImeCompositionUpdated.
		var startIndex = _compositionStartIndex;
		var length = _compositionLength;
		_isComposing = false;
		_compositionLength = 0;
		_compositionStartIndex = 0;
		_compositionResolvedLength = 0;

		TextCompositionEnded?.Invoke(this, new TextCompositionEndedEventArgs(startIndex, length));
		InvalidateTextBoxRender();
	}

	private void ReplaceCompositionText(string newText, int cursorPosition = -1)
	{
		var text = Text;
		var replaced = text[.._compositionStartIndex] + newText + text[(_compositionStartIndex + _compositionLength)..];

		// Place the caret at the IME-reported cursor position within the composition,
		// or at the end of the new text if not available.
		var caretOffset = cursorPosition >= 0 ? cursorPosition : newText.Length;

		_suppressCurrentlyTyping = true;
		_clearHistoryOnTextChanged = false;
		try
		{
			_pendingSelection = (_compositionStartIndex + caretOffset, 0);
			ProcessTextInput(replaced);
		}
		finally
		{
			_clearHistoryOnTextChanged = true;
			_suppressCurrentlyTyping = false;
		}
	}

	/// <summary>
	/// Called from OnTextChangedPartial. If the text was changed externally
	/// (not by the IME composition path), cancel the active composition.
	/// </summary>
	private void CancelCompositionOnExternalChange()
	{
		if (!_isComposing || _suppressCurrentlyTyping)
		{
			// Not composing, or the change came from ReplaceCompositionText — nothing to do.
			return;
		}

		var startIndex = _compositionStartIndex;
		var length = _compositionLength;
		_isComposing = false;
		_compositionLength = 0;
		_compositionStartIndex = 0;
		_compositionResolvedLength = 0;

		// End and restart the session so that further IME input still works
		// while the active TextBox reference stays in sync.
		_imeExtension?.EndImeSession();
		TextCompositionEnded?.Invoke(this, new TextCompositionEndedEventArgs(startIndex, length));
		InvalidateTextBoxRender();

		// Restart the IME session so the user can continue typing with IME.
		if (_activeImeTextBox == this)
		{
			_imeExtension?.StartImeSession(this);
		}
	}

	private void InvalidateTextBoxRender() => TextBoxView?.DisplayBlock.InvalidateInlines(false);

	/// <summary>
	/// Installs a fake IME extension for testing. The extension's events are
	/// forwarded to the active TextBox. Returns a disposable that restores the original.
	/// </summary>
	internal static IDisposable SetImeExtensionForTesting(IImeTextBoxExtension extension)
	{
		var original = _imeExtension;
		_imeExtension = extension;

		EventHandler onStarted = (_, _) => _activeImeTextBox?.OnImeCompositionStarted();
		EventHandler<ImeCompositionEventArgs> onUpdated = (_, e) => _activeImeTextBox?.OnImeCompositionUpdated(e.Text, e.CursorPosition, e.ResolvedLength);
		EventHandler<ImeCompositionEventArgs> onCompleted = (_, e) => _activeImeTextBox?.OnImeCompositionCompleted(e.Text);
		EventHandler onEnded = (_, _) => _activeImeTextBox?.OnImeCompositionEnded();

		extension.CompositionStarted += onStarted;
		extension.CompositionUpdated += onUpdated;
		extension.CompositionCompleted += onCompleted;
		extension.CompositionEnded += onEnded;

		return Disposable.Create(() =>
		{
			extension.CompositionStarted -= onStarted;
			extension.CompositionUpdated -= onUpdated;
			extension.CompositionCompleted -= onCompleted;
			extension.CompositionEnded -= onEnded;
			_imeExtension = original;
		});
	}
}
