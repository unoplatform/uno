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

}
