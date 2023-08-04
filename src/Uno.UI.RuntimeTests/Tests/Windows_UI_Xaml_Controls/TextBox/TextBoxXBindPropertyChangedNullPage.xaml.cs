#nullable enable

using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.TextBoxPages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class TextBoxXBindPropertyChangedNullPage : Page
{
	public TextBoxXBindPropertyChangedNullPage()
	{
		this.InitializeComponent();
	}

	public TextBox TextBox => this.textBox;

	public TextBoxXBindPropertyChangedNullViewModel? ViewModel { get; set; } = new TextBoxXBindPropertyChangedNullViewModel();
}

public class TextBoxXBindPropertyChangedNullViewModel
{
	public string Text { get; set; } = "";
}
