using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UITests.Microsoft_UI_Xaml_Controls.TeachingTipTests;

[Sample("TeachingTip", "WinUI")]
public sealed partial class TeachingTipBasicPage : Page
{
	public TeachingTipBasicPage()
	{
		this.InitializeComponent();
	}

	private void ShowTip(object sender, RoutedEventArgs args)
	{
		AutoSaveTip.IsOpen = true;
	}

	private void TestButtonClick1(object sender, RoutedEventArgs e)
	{
		ToggleThemeTeachingTip1.IsOpen = true;
	}

	private void TestButtonClick2(object sender, RoutedEventArgs e)
	{
		ToggleThemeTeachingTip2.IsOpen = true;
	}

	private void TestButtonClick3(object sender, RoutedEventArgs e)
	{
		ToggleThemeTeachingTip3.IsOpen = true;
	}
}
