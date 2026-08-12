using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace SkiaFreeProof;

public sealed class App : Application
{
	private Window? _window;

	protected override void OnLaunched(LaunchActivatedEventArgs args)
	{
		_window = new Window();
		_window.Content = new Border
		{
			Background = new SolidColorBrush(Color.FromArgb(255, 32, 96, 160)),
			Child = new TextBlock
			{
				Text = "SkiaSharp-free Uno desktop app",
				Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
			},
		};
		_window.Activate();
	}
}
