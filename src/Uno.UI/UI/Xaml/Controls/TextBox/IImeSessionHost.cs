#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Contract implemented by a control (e.g. <see cref="TextBox"/> or RichEditBox) that can host an
	/// IME composition session on Skia. It exposes both:
	/// <list type="bullet">
	/// <item>the positioning surface the platform <c>IImeTextBoxExtension</c> reads to place the
	/// candidate/preedit window (window/root, the shared <see cref="TextBoxView"/> for caret geometry,
	/// and the current selection), and</item>
	/// <item>the composition callbacks the shared <see cref="ImeSessionCoordinator"/> routes the OS
	/// composition events to.</item>
	/// </list>
	/// A single active host is arbitrated by the coordinator so the one global OS IME can be shared by
	/// multiple text controls without them cross-firing.
	/// </summary>
	internal interface IImeSessionHost
	{
		/// <summary>The XAML root the host lives in, used to resolve the native window for IME context.</summary>
		XamlRoot? XamlRoot { get; }

		/// <summary>The shared rendering companion whose <c>DisplayBlock</c> provides caret geometry.</summary>
		TextBoxView? TextBoxView { get; }

		/// <summary>Start of the current selection, in plain-text offsets.</summary>
		int SelectionStart { get; }

		/// <summary>Length of the current selection, in plain-text offsets.</summary>
		int SelectionLength { get; }

		/// <summary>Whether the current selection is anchored at its end (caret at the start).</summary>
		bool IsBackwardSelection { get; }

		/// <summary>The input scope used by platform keyboard and IME services.</summary>
		InputScope InputScope { get; }

		/// <summary>Whether text prediction is enabled for this session.</summary>
		bool IsTextPredictionEnabled { get; }

		/// <summary>The requested candidate-window alignment.</summary>
		CandidateWindowAlignment DesiredCandidateWindowAlignment { get; }

		/// <summary>The plain text mirrored into the platform input connection.</summary>
		string Text { get; }

		/// <summary>Whether the platform input connection should accept line breaks.</summary>
		bool AcceptsReturn { get; }

		/// <summary>Whether the platform input connection should request spell checking.</summary>
		bool IsSpellCheckEnabled { get; }

		/// <summary>Whether the host currently accepts text changes from the platform input connection.</summary>
		bool CanAcceptTextInput { get; }

		/// <summary>The maximum accepted text length, or 0 when no limit is applied.</summary>
		int MaxLength { get; }

		/// <summary>Whether the host is currently tracking an IME composition.</summary>
		bool IsComposing { get; }

		/// <summary>The casing requested for platform-entered text.</summary>
		CharacterCasing CharacterCasing { get; }

		/// <summary>Applies text and selection produced by a platform input connection.</summary>
		void UpdateTextFromNative(string text, int selectionStart, int selectionLength);

		/// <summary>Applies a selection-only update produced by a platform input connection.</summary>
		void SelectFromNative(int selectionStart, int selectionLength);

		/// <summary>Raises the host's paste event and returns whether the default paste was suppressed.</summary>
		bool RaisePaste();

		/// <summary>Called when the user begins an IME composition.</summary>
		void OnImeCompositionStarted();

		/// <summary>Called when the IME composition string changes.</summary>
		void OnImeCompositionUpdated(string compositionText, int cursorPosition, int resolvedLength, bool textAlreadyApplied);

		/// <summary>Called when the user commits text from the IME.</summary>
		void OnImeCompositionCompleted(string committedText, bool textAlreadyApplied);

		/// <summary>Called when the IME commits a prefix and continues composing.</summary>
		void OnImeCompositionPartiallyCommitted(
			string committedText,
			string compositionText,
			int cursorPosition,
			int resolvedLength,
			bool textAlreadyApplied);

		/// <summary>Called when the IME cancels the transient preedit.</summary>
		void OnImeCompositionCanceled(bool textAlreadyApplied);

		/// <summary>Called when the IME composition session ends (after commit or cancel).</summary>
		void OnImeCompositionEnded();

		/// <summary>Called when the platform reports candidate-window bounds.</summary>
		void OnCandidateWindowBoundsChanged(Rect bounds);
	}
}
