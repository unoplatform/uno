#nullable enable

using System;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.Performance.RenderStress
{
	/// <summary>Media-gallery archetype: a dense, scrolling grid of image tiles — stresses image draw (sample/scale) at scale.</summary>
	[Sample("Performance", Name = "RealWorld_ImageGrid", Description = "Real-UI perf: a dense scrolling image grid (media gallery). Stresses image draws + scroll. Compare fps/frame-time across Skia and the WebGPU/ProGPU builds.")]
	public sealed class RealWorld_ImageGrid : PerfBenchBase
	{
		private static readonly string[] _imgs =
		{
			"ms-appx:///Assets/LargeWisteria.jpg",
			"ms-appx:///Assets/ingredient1.png", "ms-appx:///Assets/ingredient2.png", "ms-appx:///Assets/ingredient3.png",
			"ms-appx:///Assets/ingredient4.png", "ms-appx:///Assets/ingredient5.png", "ms-appx:///Assets/ingredient6.png",
		};

		private const int Columns = 5;
		private ScrollViewer _sv = null!;
		private double _offset;
		private int _dir = 1;

		protected override string ScenarioName => "ImageGrid";
		protected override int DefaultCount => 300;

		protected override UIElement BuildStage(int count)
		{
			var rng = new Rng(11);
			var grid = new Grid { Padding = new Thickness(8), RowSpacing = 8, ColumnSpacing = 8 };
			for (var c = 0; c < Columns; c++)
			{
				grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			}
			var rows = (count + Columns - 1) / Columns;
			for (var r = 0; r < rows; r++)
			{
				grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(140) });
			}

			for (var i = 0; i < count; i++)
			{
				var tile = new Border
				{
					CornerRadius = new CornerRadius(10),
					Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x20, 0x20, 0x28)),
				};
				var g = new Grid();
				g.Children.Add(new Image
				{
					Source = new BitmapImage(new Uri(_imgs[i % _imgs.Length])),
					Stretch = Stretch.UniformToFill,
				});
				// Caption strip (gradient over the image bottom) → image + gradient + text overlap per tile.
				var strip = new Border
				{
					VerticalAlignment = VerticalAlignment.Bottom,
					Height = 34,
					Padding = new Thickness(8, 0, 8, 4),
				};
				var sb = new LinearGradientBrush { StartPoint = new(0, 0), EndPoint = new(0, 1) };
				sb.GradientStops.Add(new GradientStop { Offset = 0, Color = Color.FromArgb(0x00, 0, 0, 0) });
				sb.GradientStops.Add(new GradientStop { Offset = 1, Color = Color.FromArgb(0xC0, 0, 0, 0) });
				strip.Background = sb;
				strip.Child = new TextBlock
				{
					Text = $"Photo {i}",
					Foreground = new SolidColorBrush(Windows.UI.Colors.White),
					FontSize = 12,
					VerticalAlignment = VerticalAlignment.Bottom,
				};
				g.Children.Add(strip);
				tile.Child = g;
				Grid.SetColumn(tile, i % Columns);
				Grid.SetRow(tile, i / Columns);
				grid.Children.Add(tile);
			}

			_sv = new ScrollViewer { Content = grid, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden, HorizontalScrollMode = ScrollMode.Disabled };
			_offset = 0;
			_dir = 1;
			return _sv;
		}

		protected override void Tick(long frame) => AdvanceScroll(_sv, ref _offset, ref _dir, 8.0);
	}
}
