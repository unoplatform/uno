#nullable enable

using System.Linq;
using System.Diagnostics;

namespace Uno.UI.Runtime.Skia.WPF.Extensions.UI.Xaml.Controls
{
	internal static class Extensions
	{
		public static System.Windows.Media.Brush? ToWpfBrush(this Microsoft.UI.Xaml.Media.Brush brush)
		{
			if (brush is Microsoft.UI.Xaml.Media.SolidColorBrush solidBrush)
			{
				return new System.Windows.Media.SolidColorBrush(solidBrush.Color.ToWpfColor()) { Opacity = solidBrush.Opacity };
			}
			else if (brush is Microsoft.UI.Xaml.Media.LinearGradientBrush gradientBrush)
			{
				return new System.Windows.Media.LinearGradientBrush(gradientBrush.GradientStops.ToWpfGradientStopCollection(), gradientBrush.StartPoint.ToWpfPoint(), gradientBrush.EndPoint.ToWpfPoint())
				{
					MappingMode = gradientBrush.MappingMode.ToWpfBrushMappingMode(),
					Transform = gradientBrush.Transform.ToWpfTransform(),
					RelativeTransform = gradientBrush.RelativeTransform.ToWpfTransform(),
				};
			}
			else if (brush is Microsoft.UI.Xaml.Media.ImageBrush imageBrush)
			{
				return new System.Windows.Media.ImageBrush(imageBrush.ImageSource.ToWpfImageSource())
				{
					AlignmentX = imageBrush.AlignmentX.ToWpfAlignmentX(),
					AlignmentY = imageBrush.AlignmentY.ToWpfAlignmentY(),
					Opacity = imageBrush.Opacity,
					Stretch = imageBrush.Stretch.ToWpfStretch(),
					Transform = imageBrush.Transform.ToWpfTransform(),
					RelativeTransform = imageBrush.RelativeTransform.ToWpfTransform(),
				};
			}

			// TODO: Support more brushes.
			return null;
		}

		public static System.Windows.Media.Color ToWpfColor(this Windows.UI.Color color)
			=> System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);

		private static System.Windows.Point ToWpfPoint(this Windows.Foundation.Point point)
			=> new System.Windows.Point(point.X, point.Y);

		private static System.Windows.Media.GradientStopCollection ToWpfGradientStopCollection(this Microsoft.UI.Xaml.Media.GradientStopCollection gradientStops)
			=> new System.Windows.Media.GradientStopCollection(gradientStops.Select(stop => stop.ToWpfGradientStop()));

		private static System.Windows.Media.GradientStop ToWpfGradientStop(this Microsoft.UI.Xaml.Media.GradientStop gradientStop)
			=> new System.Windows.Media.GradientStop(gradientStop.Color.ToWpfColor(), gradientStop.Offset);

		private static System.Windows.Media.ImageSource? ToWpfImageSource(this Microsoft.UI.Xaml.Media.ImageSource imageSource)
		{
			if (imageSource is Microsoft.UI.Xaml.Media.Imaging.BitmapImage bitmapSource)
			{
				new System.Windows.Media.Imaging.BitmapImage(bitmapSource.UriSource);
			}

			// TODO: Support more image sources.
			return null;
		}

		private static System.Windows.Media.AlignmentX ToWpfAlignmentX(this Microsoft.UI.Xaml.Media.AlignmentX alignment)
		{
			Debug.Assert((int)System.Windows.Media.AlignmentX.Left == (int)Microsoft.UI.Xaml.Media.AlignmentX.Left);
			Debug.Assert((int)System.Windows.Media.AlignmentX.Right == (int)Microsoft.UI.Xaml.Media.AlignmentX.Right);
			Debug.Assert((int)System.Windows.Media.AlignmentX.Center == (int)Microsoft.UI.Xaml.Media.AlignmentX.Center);
			return (System.Windows.Media.AlignmentX)alignment;
		}

		private static System.Windows.Media.AlignmentY ToWpfAlignmentY(this Microsoft.UI.Xaml.Media.AlignmentY alignment)
		{
			Debug.Assert((int)System.Windows.Media.AlignmentY.Top == (int)Microsoft.UI.Xaml.Media.AlignmentY.Top);
			Debug.Assert((int)System.Windows.Media.AlignmentY.Center == (int)Microsoft.UI.Xaml.Media.AlignmentY.Center);
			Debug.Assert((int)System.Windows.Media.AlignmentY.Bottom == (int)Microsoft.UI.Xaml.Media.AlignmentY.Bottom);
			return (System.Windows.Media.AlignmentY)alignment;
		}

		private static System.Windows.Media.Stretch ToWpfStretch(this Microsoft.UI.Xaml.Media.Stretch stretch)
		{
			Debug.Assert((int)System.Windows.Media.Stretch.None == (int)Microsoft.UI.Xaml.Media.Stretch.None);
			Debug.Assert((int)System.Windows.Media.Stretch.Fill == (int)Microsoft.UI.Xaml.Media.Stretch.Fill);
			Debug.Assert((int)System.Windows.Media.Stretch.Uniform == (int)Microsoft.UI.Xaml.Media.Stretch.Uniform);
			Debug.Assert((int)System.Windows.Media.Stretch.UniformToFill == (int)Microsoft.UI.Xaml.Media.Stretch.UniformToFill);
			return (System.Windows.Media.Stretch)stretch;
		}

		private static System.Windows.Media.Transform? ToWpfTransform(this Microsoft.UI.Xaml.Media.Transform transform)
		{
			if (transform is Microsoft.UI.Xaml.Media.MatrixTransform matrixTransform)
			{
				return new System.Windows.Media.MatrixTransform(
					m11: matrixTransform.Matrix.M11,
					m12: matrixTransform.Matrix.M12,
					m21: matrixTransform.Matrix.M21,
					m22: matrixTransform.Matrix.M22,
					offsetX: matrixTransform.Matrix.OffsetX,
					offsetY: matrixTransform.Matrix.OffsetY);
			}
			else if (transform is Microsoft.UI.Xaml.Media.RotateTransform rotateTransform)
			{
				return new System.Windows.Media.RotateTransform(
					angle: rotateTransform.Angle,
					centerX: rotateTransform.CenterX,
					centerY: rotateTransform.CenterY);
			}
			else if (transform is Microsoft.UI.Xaml.Media.ScaleTransform scaleTransform)
			{
				return new System.Windows.Media.ScaleTransform(
					scaleX: scaleTransform.ScaleX,
					scaleY: scaleTransform.ScaleY,
					centerX: scaleTransform.CenterX,
					centerY: scaleTransform.CenterY);
			}
			else if (transform is Microsoft.UI.Xaml.Media.SkewTransform skewTransform)
			{
				return new System.Windows.Media.SkewTransform(
					angleX: skewTransform.AngleX,
					angleY: skewTransform.AngleY,
					centerX: skewTransform.CenterX,
					centerY: skewTransform.CenterY);
			}
			else if (transform is Microsoft.UI.Xaml.Media.TransformGroup transformGroup)
			{
				return new System.Windows.Media.TransformGroup()
				{
					Children = new System.Windows.Media.TransformCollection(transformGroup.Children.Select(g => g.ToWpfTransform()))
				};
			}
			else if (transform is Microsoft.UI.Xaml.Media.TranslateTransform translateTransform)
			{
				return new System.Windows.Media.TranslateTransform(translateTransform.X, translateTransform.Y);
			}

			return null;
		}

		private static System.Windows.Media.BrushMappingMode ToWpfBrushMappingMode(this Microsoft.UI.Xaml.Media.BrushMappingMode mappingMode)
		{
			Debug.Assert((int)System.Windows.Media.BrushMappingMode.Absolute == (int)Microsoft.UI.Xaml.Media.BrushMappingMode.Absolute);
			Debug.Assert((int)System.Windows.Media.BrushMappingMode.RelativeToBoundingBox == (int)Microsoft.UI.Xaml.Media.BrushMappingMode.RelativeToBoundingBox);

			return (System.Windows.Media.BrushMappingMode)mappingMode;
		}
	}
}
