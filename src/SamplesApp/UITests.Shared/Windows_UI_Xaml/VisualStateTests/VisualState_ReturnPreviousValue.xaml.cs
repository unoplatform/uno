using System;
using System.Threading.Tasks;
using Microsoft.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Uno.UI.Samples.Controls;
using Windows.UI;

namespace UITests.Windows_UI_Xaml.VisualStateTests;

[Sample(
	"Visual states",
	Description = "Background should start off as green, but turn to different color depending on button, keeping in mind that once changed to white, you shouldn't be able to get it green again",
	IsManualTest = true)]
public sealed partial class VisualState_ReturnPreviousValue : Page
{
	private bool _wasWhiteSet;

	public VisualState_ReturnPreviousValue()
	{
		this.InitializeComponent();
	}

	private async void SetState_Click(object sender, RoutedEventArgs e)
	{
		var button = (Button)sender;
		var tag = button.Tag.ToString();
		VisualStateManager.GoToState(this, tag, true);

		if (tag == "DefaultState")
		{
			if (_wasWhiteSet && ((SolidColorBrush)RootGrid.Background).Color != Windows.UI.Colors.White)
			{
				await ErrorAsync("Background is expected to be white.");
			}
			else if (!_wasWhiteSet && ((SolidColorBrush)RootGrid.Background).Color != Windows.UI.Colors.Green)
			{
				await ErrorAsync("Background is expected to be green.");
			}
		}
		else if (tag == "SecondState" && ((SolidColorBrush)RootGrid.Background).Color != Windows.UI.Colors.Red)
		{
			await ErrorAsync("Background is expected to be red.");
		}
		else if (tag == "ThirdState" && ((SolidColorBrush)RootGrid.Background).Color != Windows.UI.Colors.Blue)
		{
			await ErrorAsync("Background is expected to be blue.");
		}
	}

	private async Task ErrorAsync(string text)
	{
		var dialog = new ContentDialog()
		{
			XamlRoot = this.XamlRoot,
			Content = text,
		};

		await dialog.ShowAsync();
	}

	private async void ChangeBackground_Click(object sender, RoutedEventArgs e)
	{
		RootGrid.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
		_wasWhiteSet = true;
		if (((SolidColorBrush)RootGrid.Background).Color != Windows.UI.Colors.White)
		{
			await ErrorAsync("Background is expected to be white.");
		}
	}
}
