using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Uno.UI.Samples.Controls;

using RadialGradientBrush = Microsoft/* UWP don't rename */.UI.Xaml.Media.RadialGradientBrush;

namespace MUXControlsTestApp
{
	[Sample("Brushes", Name = "RadialGradientBrush")]
	public sealed partial class RadialGradientBrushPage : Page
	{
		public RadialGradientBrush DynamicGradientBrush;
		private Random _random;

		public RadialGradientBrushPage()
		{
			InitializeComponent();
			_random = new Random();
		}

		public string[] GetColorSpaceValueNames() => Enum.GetNames(typeof(CompositionColorSpace));

		public string DynamicGradientBrushInterpolationSpace
		{
			get
			{
				if (DynamicGradientBrush != null)
				{
					return DynamicGradientBrush.InterpolationSpace.ToString();
				}
				else
				{
					return null;
				}
			}
			set
			{
				if (DynamicGradientBrush != null && value != null)
				{
					DynamicGradientBrush.InterpolationSpace = (CompositionColorSpace)Enum.Parse(typeof(CompositionColorSpace), value);
				}
			}
		}

		public string[] GetSpreadMethodValueNames()
		{
			return Enum.GetNames(typeof(GradientSpreadMethod));
		}

		public string DynamicGradientBrushSpreadMethod
		{
			get
			{
				if (DynamicGradientBrush != null)
				{
					return DynamicGradientBrush.SpreadMethod.ToString();
				}
				else
				{
					return null;
				}
			}
			set
			{
				if (DynamicGradientBrush != null && value != null)
				{
					DynamicGradientBrush.SpreadMethod = (GradientSpreadMethod)Enum.Parse(typeof(GradientSpreadMethod), value);
				}
			}
		}

		private void ReplaceGradientButton_Click(object sender, RoutedEventArgs e)
		{
			DynamicGradientBrush = new RadialGradientBrush();
			DynamicGradientBrush.FallbackColor = Color.FromArgb(Byte.MaxValue, (byte)_random.Next(256), (byte)_random.Next(256), (byte)_random.Next(256));

			// Set brush before adding stops
			RectangleWithDynamicGradient.Fill = DynamicGradientBrush;

			DynamicGradientBrush.GradientStops.Clear();
			for (int i = 0; i < _random.Next(2, 5); i++)
			{
				AddRandomGradientStop(DynamicGradientBrush);
			}

			// Set brush after adding stops
			TextBlockWithDynamicGradient.Foreground = DynamicGradientBrush;

			Bindings.Update();
		}

		private void AddGradientStopButton_Click(object sender, RoutedEventArgs e)
		{
			AddRandomGradientStop(DynamicGradientBrush);
		}

		private void RemoveGradientStopButton_Click(object sender, RoutedEventArgs e)
		{
			RemoveRandomGradientStop(DynamicGradientBrush);
		}

		private void RandomizeGradientOriginButton_Click(object sender, RoutedEventArgs e)
		{
			RandomizeGradientOrigin(DynamicGradientBrush);
		}

		private void RandomizeCenterButton_Click(object sender, RoutedEventArgs e)
		{
			RandomizeCenter(DynamicGradientBrush);
		}

		private void RandomizeRadiusButton_Click(object sender, RoutedEventArgs e)
		{
			RandomizeRadius(DynamicGradientBrush);
		}

		private void ToggleMappingModeButton_Click(object sender, RoutedEventArgs e)
		{
			ToggleMappingMode(DynamicGradientBrush);
		}

		private async void GenerateRenderTargetBitmapButton_Click(object sender, RoutedEventArgs e)
		{
			var rtb = new RenderTargetBitmap();
			await rtb.RenderAsync(GradientRectangle);

			RenderTargetBitmapResultRectangle.Fill = new ImageBrush() { ImageSource = rtb };

			var pixelBuffer = await rtb.GetPixelsAsync();
			byte[] pixelArray = pixelBuffer.ToArray();

			// Sample top left and center pixels to verify rendering is correct.
			var centerColor = GetPixelAtPoint(new Point(rtb.PixelWidth / 2, rtb.PixelHeight / 2), rtb, pixelArray);
			var outerColor = GetPixelAtPoint(new Point(0, 0), rtb, pixelArray);

			if (ApiInformation.IsTypePresent("Windows.UI.Composition.CompositionRadialGradientBrush"))
			{
				// If CompositionRadialGradientBrush is available then should be rendering a gradient.
				if (centerColor == Windows.UI.Colors.Orange && outerColor == Windows.UI.Colors.Green)
				{
					ColorMatchTestResult.Text = "Passed";
				}
				else
				{
					ColorMatchTestResult.Text = "Failed";
				}
			}
			else
			{
				if (centerColor == Windows.UI.Colors.Red && outerColor == Windows.UI.Colors.Red)
				{
					ColorMatchTestResult.Text = "Passed";
				}
				else
				{
					ColorMatchTestResult.Text = "Failed";
				}
			}
		}

		private void AddRandomGradientStop(RadialGradientBrush gradientBrush)
		{
			if (gradientBrush != null)
			{
				var stop = new GradientStop();
				stop.Color = Color.FromArgb(Byte.MaxValue, (byte)_random.Next(256), (byte)_random.Next(256), (byte)_random.Next(256));
				stop.Offset = _random.NextDouble();

				gradientBrush.GradientStops.Add(stop);
			}
		}

		private void RemoveRandomGradientStop(RadialGradientBrush gradientBrush)
		{
			if (gradientBrush != null && gradientBrush.GradientStops.Count > 0)
			{
				gradientBrush.GradientStops.RemoveAt(_random.Next(0, gradientBrush.GradientStops.Count - 1));
			}
		}

		private void RandomizeGradientOrigin(RadialGradientBrush gradientBrush)
		{
			if (gradientBrush != null)
			{
				if (gradientBrush.MappingMode == BrushMappingMode.Absolute)
				{
					gradientBrush.GradientOrigin = new Point(_random.Next(0, 100), _random.Next(0, 100));
				}
				else
				{
					gradientBrush.GradientOrigin = new Point(_random.Next(-100, 100) / 100f, _random.Next(-100, 100) / 100f);
				}
			}
		}

		private void RandomizeCenter(RadialGradientBrush gradientBrush)
		{
			if (gradientBrush != null)
			{
				if (gradientBrush.MappingMode == BrushMappingMode.Absolute)
				{
					gradientBrush.Center = new Point(_random.Next(0, 100), _random.Next(0, 100));
				}
				else
				{
					gradientBrush.Center = new Point(_random.NextDouble(), _random.NextDouble());
				}
			}
		}

		private void RandomizeRadius(RadialGradientBrush gradientBrush)
		{
			if (gradientBrush != null)
			{
				if (gradientBrush.MappingMode == BrushMappingMode.Absolute)
				{
					gradientBrush.RadiusX = _random.Next(10, 200);
					gradientBrush.RadiusY = _random.Next(10, 200);
				}
				else
				{
					gradientBrush.RadiusX = _random.NextDouble();
					gradientBrush.RadiusY = _random.NextDouble();
				}
			}
		}

		private void ToggleMappingMode(RadialGradientBrush gradientBrush)
		{
			if (gradientBrush != null)
			{
				gradientBrush.MappingMode = ((gradientBrush.MappingMode == BrushMappingMode.RelativeToBoundingBox) ? BrushMappingMode.Absolute : BrushMappingMode.RelativeToBoundingBox);

				RandomizeCenter(gradientBrush);
				RandomizeRadius(gradientBrush);
				RandomizeGradientOrigin(gradientBrush);
			}
		}

		private Color GetPixelAtPoint(Point p, RenderTargetBitmap rtb, byte[] pixelArray)
		{
			Color pixelColor = new Color();
			int x = (int)(p.X);
			int y = (int)(p.Y);
			int pointPosition = 4 * (rtb.PixelWidth * y + x);

			pixelColor.B = pixelArray[pointPosition];
			pixelColor.G = pixelArray[pointPosition + 1];
			pixelColor.R = pixelArray[pointPosition + 2];
			pixelColor.A = pixelArray[pointPosition + 3];

			return pixelColor;
		}
	}
}
