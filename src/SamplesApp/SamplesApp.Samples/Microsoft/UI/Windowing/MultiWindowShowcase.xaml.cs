using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Samples.Controls;
using Windows.UI;

#if HAS_UNO
using Uno.UI.Xaml.Controls;
#endif

namespace UITests.Microsoft_UI_Windowing;

[Sample(
	"Windowing",
	IsManualTest = true,
	Description =
		"Opens additional windows, each with its own content, accent and running clock. " +
		"On Android each one is a task of its own, so they can be placed side by side.")]
public sealed partial class MultiWindowShowcase : Page
{
	// Distinct per window so a screenshot shows at a glance that these are separate trees.
	private static readonly Color[] _accents =
	{
		Color.FromArgb(255, 0x53, 0x2D, 0xE0),
		Color.FromArgb(255, 0xD8, 0x2B, 0x7E),
		Color.FromArgb(255, 0x00, 0x93, 0x8A),
		Color.FromArgb(255, 0xE0, 0x6C, 0x00),
	};

	private static int _windowCount;

	public MultiWindowShowcase()
	{
		InitializeComponent();

		if (!SupportsMultipleWindows)
		{
			OpenWindowButton.IsEnabled = false;
			UnsupportedPanel.Visibility = Visibility.Visible;
		}
	}

	private static bool SupportsMultipleWindows =>
#if HAS_UNO
		NativeWindowFactory.SupportsMultipleWindows;
#else
		true;
#endif

	private void OnOpenWindow(object sender, RoutedEventArgs args)
	{
		var index = ++_windowCount;
		var accent = _accents[(index - 1) % _accents.Length];

		var window = new Window { Title = $"Uno Platform window {index}" };
		window.Content = BuildContent(index, accent, out var clock);

		var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
		timer.Tick += (_, _) => clock.Text = DateTime.Now.ToString("HH:mm:ss");
		timer.Start();

		window.Closed += (_, _) => timer.Stop();

		window.Activate();
	}

	private static UIElement BuildContent(int index, Color accent, out TextBlock clock)
	{
		clock = new TextBlock
		{
			Text = DateTime.Now.ToString("HH:mm:ss"),
			FontSize = 20,
			Opacity = 0.75,
			Foreground = new SolidColorBrush(Colors.White),
			HorizontalAlignment = HorizontalAlignment.Center,
		};

		var panel = new StackPanel
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Spacing = 4,
			Children =
			{
				new TextBlock
				{
					Text = "WINDOW",
					FontSize = 15,
					Opacity = 0.7,
					CharacterSpacing = 260,
					Foreground = new SolidColorBrush(Colors.White),
					HorizontalAlignment = HorizontalAlignment.Center,
				},
				new TextBlock
				{
					Text = index.ToString(),
					FontSize = 150,
					FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
					Foreground = new SolidColorBrush(Colors.White),
					HorizontalAlignment = HorizontalAlignment.Center,
				},
				clock,
				new Ellipse
				{
					Width = 64,
					Height = 4,
					Margin = new Thickness(0, 20, 0, 0),
					Opacity = 0.5,
					Fill = new SolidColorBrush(Colors.White),
					HorizontalAlignment = HorizontalAlignment.Center,
				},
			},
		};

		return new Grid
		{
			Background = new LinearGradientBrush
			{
				StartPoint = new Windows.Foundation.Point(0, 0),
				EndPoint = new Windows.Foundation.Point(1, 1),
				GradientStops =
				{
					new GradientStop { Color = accent, Offset = 0 },
					new GradientStop { Color = Darken(accent), Offset = 1 },
				},
			},
			Children = { panel },
		};
	}

	private static Color Darken(Color color) => Color.FromArgb(
		color.A,
		(byte)(color.R * 0.55),
		(byte)(color.G * 0.55),
		(byte)(color.B * 0.55));
}
