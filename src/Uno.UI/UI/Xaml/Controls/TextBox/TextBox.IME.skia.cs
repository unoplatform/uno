#nullable enable
using System;
using Windows.Foundation;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Xaml.Controls.Extensions;

namespace Microsoft.UI.Xaml.Controls;

public partial class TextBox : IImeSessionHost
{
	private bool _isComposing;
	private bool _platformTextApplyInProgress;
	// True when the current composition session has the platform applying text directly
	// (e.g., Android's InputConnection). In this mode, key events arrive independently
	// from composition events and should NOT be swallowed by the IsComposing check.
	private bool _compositionAppliedByPlatform;
	private int _compositionStartIndex;
	private int _compositionLength;
	private int _compositionResolvedLength;

	internal bool ShouldSwallowKeyDuringComposition => _isComposing && !_compositionAppliedByPlatform;

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

	// --- IImeSessionHost positioning surface (read by the platform IME extensions) ---

	XamlRoot? IImeSessionHost.XamlRoot => XamlRoot;

	TextBoxView? IImeSessionHost.TextBoxView => TextBoxView;

	int IImeSessionHost.SelectionStart => SelectionStart;

	int IImeSessionHost.SelectionLength => SelectionLength;

	bool IImeSessionHost.IsBackwardSelection => IsBackwardSelection;

	InputScope IImeSessionHost.InputScope => InputScope;

	private static void InitializeIme() => ImeSessionCoordinator.Initialize();

	private void StartImeSession()
	{
		// Don't route IME composition events to PasswordBox — password text
		// must not be processed as composition text, even if the extension
		// no-ops for PasswordBox (any later global composition event would
		// otherwise be forwarded to this PasswordBox via the coordinator's active host).
		if (this is PasswordBox)
		{
			return;
		}

		ImeSessionCoordinator.StartSession(this);
	}

	private void EndImeSession()
	{
		// Symmetric with StartImeSession — PasswordBoxes never activate the
		// IME session, so don't tear it down (which would clobber the active host).
		if (this is PasswordBox)
		{
			return;
		}

		ImeSessionCoordinator.EndSession(this);

		// Defensively reset composition state in case the extension's CompositionEnded
		// event didn't fire (e.g., extension's _isComposing already false but TextBox's
		// _isComposing still true from a stale CompositionStarted). Without this, all
		// subsequent key events would be swallowed at the IsComposing check in OnKeyDown.
		_compositionAppliedByPlatform = false;
		if (_isComposing)
		{
			var startIndex = _compositionStartIndex;
			var length = _compositionLength;
			_isComposing = false;
			_compositionLength = 0;
			_compositionStartIndex = 0;
			_compositionResolvedLength = 0;

			TextCompositionEnded?.Invoke(this, new TextCompositionEndedEventArgs(startIndex, length));
			InvalidateTextBoxRender();
		}
	}

	void IImeSessionHost.OnImeCompositionStarted()
	{
		if (IsReadOnly)
		{
			return;
		}

		_isComposing = true;
		_compositionStartIndex = SelectionStart;
		// Initialize from SelectionLength so the first ReplaceCompositionText
		// replaces the selected range, matching normal typing behavior.
		_compositionLength = SelectionLength;
		_compositionResolvedLength = 0;

		TextCompositionStarted?.Invoke(this, new TextCompositionStartedEventArgs(_compositionStartIndex, _compositionLength));
	}

	void IImeSessionHost.OnImeCompositionUpdated(string compositionText, int cursorPosition, int resolvedLength, bool textAlreadyApplied)
	{
		if (IsReadOnly)
		{
			return;
		}

		if (textAlreadyApplied)
		{
			// The platform (e.g., Android InputConnection) already applied the text.
			// Suppress CancelCompositionOnExternalChange for the ProcessTextInput call
			// that will follow from the platform's text sync (EndBatchEdit).
			_platformTextApplyInProgress = true;
			// Mark the session so key events aren't swallowed by the IsComposing check
			// (Android key events are independent of composition events).
			_compositionAppliedByPlatform = true;
		}
		else
		{
			ReplaceCompositionText(compositionText, cursorPosition);
		}

		_compositionLength = compositionText.Length;
		_compositionResolvedLength = resolvedLength;

		TextCompositionChanged?.Invoke(this, new TextCompositionChangedEventArgs(_compositionStartIndex, _compositionLength));
		InvalidateTextBoxRender();
	}

	void IImeSessionHost.OnImeCompositionCompleted(string committedText, bool textAlreadyApplied)
	{
		if (IsReadOnly)
		{
			return;
		}

		TrySetCurrentlyTyping(true);
		if (!textAlreadyApplied)
		{
			ReplaceCompositionText(committedText);
		}

		var startIndex = _compositionStartIndex;
		var committedLength = committedText.Length;
		_isComposing = false;
		_compositionAppliedByPlatform = false;
		_compositionLength = 0;
		_compositionStartIndex = 0;
		_compositionResolvedLength = 0;

		TextCompositionEnded?.Invoke(this, new TextCompositionEndedEventArgs(startIndex, committedLength));
		InvalidateTextBoxRender();
	}

	void IImeSessionHost.OnImeCompositionEnded()
	{
		if (!_isComposing)
		{
			_compositionAppliedByPlatform = false;
			return;
		}

		// Composition ended without explicit commit — keep text as-is (matches WinUI behavior).
		// The composition text was already inserted via ProcessTextInput during OnImeCompositionUpdated.
		var startIndex = _compositionStartIndex;
		var length = _compositionLength;
		_isComposing = false;
		_compositionAppliedByPlatform = false;
		_compositionLength = 0;
		_compositionStartIndex = 0;
		_compositionResolvedLength = 0;

		TextCompositionEnded?.Invoke(this, new TextCompositionEndedEventArgs(startIndex, length));
		InvalidateTextBoxRender();
	}

	private void ReplaceCompositionText(string newText, int cursorPosition = -1)
	{
		var text = Text;
		// Clamp indices to text bounds in case the platform modified the text
		// out-of-band (e.g., Android IME autocorrect during a composition session)
		// leaving _compositionStartIndex/_compositionLength stale.
		var startIndex = Math.Min(_compositionStartIndex, text.Length);
		var endIndex = Math.Min(_compositionStartIndex + _compositionLength, text.Length);
		var replaced = text[..startIndex] + newText + text[endIndex..];

		// Place the caret at the IME-reported cursor position within the composition,
		// or at the end of the new text if not available. Clamp to valid range
		// since some IMEs may report offsets beyond the current preedit length.
		var caretOffset = cursorPosition >= 0
			? Math.Min(cursorPosition, newText.Length)
			: newText.Length;

		_suppressCurrentlyTyping = true;
		_clearHistoryOnTextChanged = false;
		try
		{
			_pendingSelection = (startIndex + caretOffset, 0);
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

		if (_platformTextApplyInProgress)
		{
			// The platform already applied the text (e.g., Android EndBatchEdit).
			// Don't cancel the composition — just clear the flag.
			_platformTextApplyInProgress = false;
			return;
		}

		var startIndex = _compositionStartIndex;
		var length = _compositionLength;
		_isComposing = false;
		_compositionAppliedByPlatform = false;
		_compositionLength = 0;
		_compositionStartIndex = 0;
		_compositionResolvedLength = 0;

		// End and restart the session so that further IME input still works
		// while the active host reference stays in sync.
		ImeSessionCoordinator.Extension?.EndImeSession();
		TextCompositionEnded?.Invoke(this, new TextCompositionEndedEventArgs(startIndex, length));
		InvalidateTextBoxRender();

		// Restart the IME session so the user can continue typing with IME.
		if (ReferenceEquals(ImeSessionCoordinator.ActiveHost, this))
		{
			ImeSessionCoordinator.Extension?.StartImeSession(this);
		}
	}

	private void InvalidateTextBoxRender() => TextBoxView?.DisplayBlock.InvalidateInlines(false);

	/// <summary>
	/// Installs a fake IME extension for testing. The extension's events are
	/// forwarded to the active host. Returns a disposable that restores the original.
	/// </summary>
	internal static IDisposable SetImeExtensionForTesting(IImeTextBoxExtension extension)
		=> ImeSessionCoordinator.SetExtensionForTesting(extension);
}
