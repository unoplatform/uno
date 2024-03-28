#pragma warning disable 105 // Disabled until the tree is migrate to WinUI

using System;
using Microsoft/* UWP don't rename */.UI.Xaml.Media;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace UITests.Windows_UI_Xaml_Media.BrushesTests
{
	[Sample("Brushes")]
	public sealed partial class DynamicBrushes_On_Shapes : Page
	{
		private int shape1LayoutCount;
		private int shape2LayoutCount;
		private int shape3LayoutCount;

		private SolidColorBrush _solid1 = new SolidColorBrush { Color = Colors.Red };
		private SolidColorBrush _solid2 = new SolidColorBrush { Color = Colors.Blue };

		private ImageBrush _image1 = new ImageBrush { ImageSource = new BitmapImage { UriSource = new Uri("ms-appx:///Assets/RatingControl.png") } };
		private ImageBrush _image2 = new ImageBrush { ImageSource = new BitmapImage { UriSource = new Uri("ms-appx:///Assets/Formats/uno-overalls.jpg") } };

		public DynamicBrushes_On_Shapes()
		{
			this.InitializeComponent();

			shape1.LayoutUpdated += (snd, evt) =>
			{
				shape1LayoutCount++;
				UpdateText();
			};
			shape2.LayoutUpdated += (snd, evt) =>
			{
				shape2LayoutCount++;
				UpdateText();
			};
			shape3.LayoutUpdated += (snd, evt) =>
			{
				shape3LayoutCount++;
				UpdateText();
			};

			void UpdateText()
			{
				//txt.Text = $"Layout Updated: {shape1LayoutCount}, {shape2LayoutCount}, {shape3LayoutCount}";
			}

		}

		private void SetFill(object sender, RoutedEventArgs e)
		{
			if (sender is Button btn)
			{
				var brush = GetBrush(btn.Tag as string);
				border1.Background = brush;
				shape1.Fill = brush;
				shape2.Fill = brush;
				shape3.Fill = brush;
			}
		}

		private Brush GetBrush(string tag)
		{
			switch (tag)
			{
				case "null": return null;
				case "solid1": return _solid1;
				case "solid2": return _solid2;
				case "linear":
					return new LinearGradientBrush
					{
						StartPoint = new Point(0, 0),
						EndPoint = new Point(1, 1),
						GradientStops =
					{
						new GradientStop { Offset = 0, Color = Colors.Red },
						new GradientStop { Offset = .3, Color = Colors.Blue },
						new GradientStop { Offset = .5, Color = Colors.Yellow },
						new GradientStop { Offset = 1, Color = Colors.Violet }
					}
					};
				case "radial":
					return new RadialGradientBrush
					{
						GradientStops =
						{
							new GradientStop { Offset = 0, Color = Colors.Red },
							new GradientStop { Offset = .3, Color = Colors.Blue },
							new GradientStop { Offset = .5, Color = Colors.Yellow },
							new GradientStop { Offset = 1, Color = Colors.Violet }
						}
					};
				case "img1":
					return _image1;
				case "img2":
					return _image2;
				default:
					throw new Exception();
			}
		}

		private void SetColor(object sender, RoutedEventArgs e)
		{
			if (sender is RadioButton radioButton)
			{
				var brush = radioButton.GroupName.EndsWith("1") ? _solid1 : _solid2;
				var color = (radioButton.Background as SolidColorBrush)?.Color ?? throw new Exception();
				brush.Color = color;
			}
		}
		private void SetImage(object sender, RoutedEventArgs e)
		{
			if (sender is RadioButton radioButton)
			{
				var brush = radioButton.GroupName.EndsWith("1") ? _image1 : _image2;
				var image = radioButton.Tag as string;

				brush.ImageSource = new BitmapImage { UriSource = new Uri(image) };
			}
		}
	}
}
