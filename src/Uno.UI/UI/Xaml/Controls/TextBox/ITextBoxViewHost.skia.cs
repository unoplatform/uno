#nullable enable

using Windows.UI.Text;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Contract implemented by a control (e.g. <see cref="TextBox"/> or RichEditBox) that hosts a
	/// shared <see cref="TextBoxView"/>. It captures everything the view/editing engine needs to read
	/// from, or call back into, its owning control so the engine can stay control-agnostic.
	/// </summary>
	internal interface ITextBoxViewHost
	{
		/// <summary>The current plain-text content of the host.</summary>
		string Text { get; }

		/// <summary>Whether spell-checking is enabled on the host.</summary>
		bool IsSpellCheckEnabled { get; }

		/// <summary>The template's content presenter that hosts the <see cref="TextBoxView.DisplayBlock"/>.</summary>
		ContentControl? ContentElement { get; }

		FontFamily FontFamily { get; }

		double FontSize { get; }

		FontStyle FontStyle { get; }

		FontStretch FontStretch { get; }

		FontWeight FontWeight { get; }

		FlowDirection FlowDirection { get; }

		TextWrapping TextWrapping { get; }

		TextAlignment TextAlignment { get; }

		/// <summary>Whether an IME composition is currently in progress on the host.</summary>
		bool IsComposing { get; }

		/// <summary>Start index (in plain-text offsets) of the unresolved IME composition underline.</summary>
		int CompositionUnderlineStart { get; }

		/// <summary>Length of the unresolved IME composition underline (0 when not composing).</summary>
		int CompositionUnderlineLength { get; }

		/// <summary>
		/// Whether the host's <see cref="TextAlignment"/> is still at its default-value precedence
		/// (i.e. not explicitly set). Used by the shared <see cref="TextBoxView.DisplayBlock"/> to
		/// decide whether to defer to its own alignment.
		/// </summary>
		bool IsTextAlignmentSetToDefault { get; }

		/// <summary>
		/// Runs the host's text-input pipeline (BeforeTextChanging, TextChanging, DP coercion, etc.)
		/// on <paramref name="newText"/> and returns the resulting (possibly modified) text.
		/// </summary>
		string ProcessTextInput(string newText);

		/// <summary>Forces an immediate layout pass on the host.</summary>
		void UpdateLayout();
	}
}
