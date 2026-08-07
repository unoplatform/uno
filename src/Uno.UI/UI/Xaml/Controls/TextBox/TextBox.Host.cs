#nullable enable

namespace Microsoft.UI.Xaml.Controls;

partial class TextBox : ITextBoxHost
{
	private readonly TextBoxCore _core;

	Control ITextBoxHost.Owner => this;

	string ITextBoxHost.TextValue
	{
		get => Text;
		set => Text = value;
	}
}
