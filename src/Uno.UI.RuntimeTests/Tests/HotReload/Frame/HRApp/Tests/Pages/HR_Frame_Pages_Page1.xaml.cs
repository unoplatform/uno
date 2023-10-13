using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.Pages;

public sealed partial class HR_Frame_Pages_Page1 : Page
{
	public HR_Frame_Pages_Page1()
	{
		this.InitializeComponent();
	}

	public void Page2Click(object sender, RoutedEventArgs e)
	{
		this.Frame.Navigate(typeof(HR_Frame_Pages_Page2));
	}

	public void SetTextBoxText(string text)
	{
		this.InputTextBox.Text = text;
	}
}
