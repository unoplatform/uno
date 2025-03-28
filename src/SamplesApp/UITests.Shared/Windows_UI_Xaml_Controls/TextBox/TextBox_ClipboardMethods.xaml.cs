using System;
using System.Threading.Tasks;
using Uno.UI.Samples.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml_Controls.TextBox;

[Sample("TextBox")]
public sealed partial class TextBox_ClipboardMethods : Page
{
	private const int Delay = 3000;

	public TextBox_ClipboardMethods() => InitializeComponent();

	private void PasteFromClipboard() => InputBox.PasteFromClipboard();

	private async void CopySelectionToClipboard()
	{
		await Task.Delay(Delay);
		InputBox.CopySelectionToClipboard();
		await DisplayClipboardContentAsync();
	}

	private async void CutSelectionToClipboard()
	{
		await Task.Delay(Delay);
		InputBox.CutSelectionToClipboard();
		await DisplayClipboardContentAsync();
	}

	private async Task DisplayClipboardContentAsync()
	{
		await Task.Delay(100);
		var content = Clipboard.GetContent();
		var text = await content.GetTextAsync();
		ResultTextBlock.Text = $"{DateTimeOffset.Now}: {text}";
	}
}
