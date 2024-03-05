using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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

	private async void TestButtonClick3(object sender, RoutedEventArgs e)
	{
		ToggleThemeTeachingTip3.IsOpen = true;

		await System.Threading.Tasks.Task.Delay(1000);

		ToggleThemeTeachingTip3.HeroContentPlacement = TeachingTipHeroContentPlacementMode.Bottom;
	}
}
