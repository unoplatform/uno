#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

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

		/// <summary>Called when the user begins an IME composition.</summary>
		void OnImeCompositionStarted();

		/// <summary>Called when the IME composition string changes.</summary>
		void OnImeCompositionUpdated(string compositionText, int cursorPosition, int resolvedLength, bool textAlreadyApplied);

		/// <summary>Called when the user commits text from the IME.</summary>
		void OnImeCompositionCompleted(string committedText, bool textAlreadyApplied);

		/// <summary>Called when the IME composition session ends (after commit or cancel).</summary>
		void OnImeCompositionEnded();
	}
}
