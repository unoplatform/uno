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

		/// <summary>
		/// Runs the host's text-input pipeline (BeforeTextChanging, TextChanging, DP coercion, etc.)
		/// on <paramref name="newText"/> and returns the resulting (possibly modified) text.
		/// </summary>
		string ProcessTextInput(string newText);

		/// <summary>Forces an immediate layout pass on the host.</summary>
		void UpdateLayout();
	}
}
