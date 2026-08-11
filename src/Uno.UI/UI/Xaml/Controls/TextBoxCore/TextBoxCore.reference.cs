#nullable enable

namespace Microsoft.UI.Xaml.Controls;

partial class TextBoxCore
{
	private TextBoxView? _textBoxView;

	private void UpdateTextBoxView() { }

	internal int SelectionStart { get; set; }

	internal int SelectionLength { get; set; }
}
