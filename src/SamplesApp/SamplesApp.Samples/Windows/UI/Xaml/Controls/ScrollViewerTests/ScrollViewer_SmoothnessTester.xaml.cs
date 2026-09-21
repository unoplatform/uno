using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Samples.Controls;
using Windows.UI;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ScrollViewerTests;

[Sample("Scrolling", Name = "ScrollSmoothnessTester", IsManualTest = true, Description = "Stress-tests scroll smoothness with many 300x300 colored rectangles, hosted in a StackPanel (vertical/horizontal), a ListView and an ItemsRepeater")]
public sealed partial class ScrollViewer_SmoothnessTester : Page
{
	private const int ItemCount = 200;

	public ScrollViewer_SmoothnessTester()
	{
		this.InitializeComponent();

		var random = new Random();

		for (var i = 0; i < ItemCount; i++)
		{
			VerticalStack.Children.Add(CreateRectangle(random));
			HorizontalStack.Children.Add(CreateRectangle(random));
		}

		ListViewHost.ItemsSource = Enumerable.Range(0, ItemCount).Select(_ => RandomBrush(random)).ToList();
		ItemsRepeaterHost.ItemsSource = Enumerable.Range(0, ItemCount).Select(_ => RandomBrush(random)).ToList();
	}

	private static Rectangle CreateRectangle(Random random) => new()
	{
		Width = 300,
		Height = 300,
		Margin = new Thickness(4),
		Fill = RandomBrush(random)
	};

	private static SolidColorBrush RandomBrush(Random random) =>
		new(Color.FromArgb(255, (byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256)));
}
