#nullable enable

using System.Diagnostics;
using System.Linq;

namespace Uno.UI.Runtime.Skia.Wpf.Extensions;

internal static class TypeConversionExtensions
{
	public static System.Windows.Media.Brush? ToWpfBrush(this Windows.UI.Xaml.Media.Brush brush)
	{
		if (brush is Windows.UI.Xaml.Media.SolidColorBrush solidBrush)
		{
			return new System.Windows.Media.SolidColorBrush(solidBrush.Color.ToWpfColor()) { Opacity = solidBrush.Opacity };
		}
		else if (brush is Windows.UI.Xaml.Media.LinearGradientBrush gradientBrush)
		{
			return new System.Windows.Media.LinearGradientBrush(gradientBrush.GradientStops.ToWpfGradientStopCollection(), gradientBrush.StartPoint.ToWpfPoint(), gradientBrush.EndPoint.ToWpfPoint())
			{
				MappingMode = gradientBrush.MappingMode.ToWpfBrushMappingMode(),
				Transform = gradientBrush.Transform.ToWpfTransform(),
				RelativeTransform = gradientBrush.RelativeTransform.ToWpfTransform(),
			};
		}
		else if (brush is Windows.UI.Xaml.Media.ImageBrush imageBrush)
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

	private static System.Windows.Media.GradientStopCollection ToWpfGradientStopCollection(this Windows.UI.Xaml.Media.GradientStopCollection gradientStops)
		=> new System.Windows.Media.GradientStopCollection(gradientStops.Select(stop => stop.ToWpfGradientStop()));

	private static System.Windows.Media.GradientStop ToWpfGradientStop(this Windows.UI.Xaml.Media.GradientStop gradientStop)
		=> new System.Windows.Media.GradientStop(gradientStop.Color.ToWpfColor(), gradientStop.Offset);

	private static System.Windows.Media.ImageSource? ToWpfImageSource(this Windows.UI.Xaml.Media.ImageSource imageSource)
	{
		if (imageSource is Windows.UI.Xaml.Media.Imaging.BitmapImage bitmapSource)
		{
			return new System.Windows.Media.Imaging.BitmapImage(bitmapSource.UriSource);
		}

		// TODO: Support more image sources.
		return null;
	}

	private static System.Windows.Media.AlignmentX ToWpfAlignmentX(this Windows.UI.Xaml.Media.AlignmentX alignment) =>
		alignment switch
		{
			Windows.UI.Xaml.Media.AlignmentX.Left => System.Windows.Media.AlignmentX.Left,
			Windows.UI.Xaml.Media.AlignmentX.Center => System.Windows.Media.AlignmentX.Center,
			Windows.UI.Xaml.Media.AlignmentX.Right => System.Windows.Media.AlignmentX.Right,
			_ => throw new System.InvalidOperationException(),
		};

	private static System.Windows.Media.AlignmentY ToWpfAlignmentY(this Windows.UI.Xaml.Media.AlignmentY alignment) =>
		alignment switch
		{
			Windows.UI.Xaml.Media.AlignmentY.Top => System.Windows.Media.AlignmentY.Top,
			Windows.UI.Xaml.Media.AlignmentY.Center => System.Windows.Media.AlignmentY.Center,
			Windows.UI.Xaml.Media.AlignmentY.Bottom => System.Windows.Media.AlignmentY.Bottom,
			_ => throw new System.InvalidOperationException(),
		};

	private static System.Windows.Media.Stretch ToWpfStretch(this Windows.UI.Xaml.Media.Stretch stretch) =>
		stretch switch
		{
			Windows.UI.Xaml.Media.Stretch.None => System.Windows.Media.Stretch.None,
			Windows.UI.Xaml.Media.Stretch.Fill => System.Windows.Media.Stretch.Fill,
			Windows.UI.Xaml.Media.Stretch.Uniform => System.Windows.Media.Stretch.Uniform,
			Windows.UI.Xaml.Media.Stretch.UniformToFill => System.Windows.Media.Stretch.UniformToFill,
			_ => throw new System.InvalidOperationException(),
		};

	private static System.Windows.Media.Transform? ToWpfTransform(this Windows.UI.Xaml.Media.Transform transform)
	{
		if (transform is Windows.UI.Xaml.Media.MatrixTransform matrixTransform)
		{
			return new System.Windows.Media.MatrixTransform(
				m11: matrixTransform.Matrix.M11,
				m12: matrixTransform.Matrix.M12,
				m21: matrixTransform.Matrix.M21,
				m22: matrixTransform.Matrix.M22,
				offsetX: matrixTransform.Matrix.OffsetX,
				offsetY: matrixTransform.Matrix.OffsetY);
		}
		else if (transform is Windows.UI.Xaml.Media.RotateTransform rotateTransform)
		{
			return new System.Windows.Media.RotateTransform(
				angle: rotateTransform.Angle,
				centerX: rotateTransform.CenterX,
				centerY: rotateTransform.CenterY);
		}
		else if (transform is Windows.UI.Xaml.Media.ScaleTransform scaleTransform)
		{
			return new System.Windows.Media.ScaleTransform(
				scaleX: scaleTransform.ScaleX,
				scaleY: scaleTransform.ScaleY,
				centerX: scaleTransform.CenterX,
				centerY: scaleTransform.CenterY);
		}
		else if (transform is Windows.UI.Xaml.Media.SkewTransform skewTransform)
		{
			return new System.Windows.Media.SkewTransform(
				angleX: skewTransform.AngleX,
				angleY: skewTransform.AngleY,
				centerX: skewTransform.CenterX,
				centerY: skewTransform.CenterY);
		}
		else if (transform is Windows.UI.Xaml.Media.TransformGroup transformGroup)
		{
			return new System.Windows.Media.TransformGroup()
			{
				Children = new System.Windows.Media.TransformCollection(transformGroup.Children.Select(g => g.ToWpfTransform()))
			};
		}
		else if (transform is Windows.UI.Xaml.Media.TranslateTransform translateTransform)
		{
			return new System.Windows.Media.TranslateTransform(translateTransform.X, translateTransform.Y);
		}

		return null;
	}

	private static System.Windows.Media.BrushMappingMode ToWpfBrushMappingMode(this Windows.UI.Xaml.Media.BrushMappingMode mappingMode) =>
		mappingMode switch
		{
			Windows.UI.Xaml.Media.BrushMappingMode.Absolute => System.Windows.Media.BrushMappingMode.Absolute,
			Windows.UI.Xaml.Media.BrushMappingMode.RelativeToBoundingBox => System.Windows.Media.BrushMappingMode.RelativeToBoundingBox,
			_ => throw new System.InvalidOperationException(),
		};

	public static System.Windows.FontStretch ToWpfFontStretch(this Windows.UI.Text.FontStretch fontStretch) =>
		fontStretch switch
		{
			Windows.UI.Text.FontStretch.Condensed => System.Windows.FontStretches.Condensed,
			Windows.UI.Text.FontStretch.Expanded => System.Windows.FontStretches.Expanded,
			Windows.UI.Text.FontStretch.ExtraCondensed => System.Windows.FontStretches.ExtraCondensed,
			Windows.UI.Text.FontStretch.ExtraExpanded => System.Windows.FontStretches.ExtraExpanded,
			Windows.UI.Text.FontStretch.Normal => System.Windows.FontStretches.Normal,
			Windows.UI.Text.FontStretch.SemiCondensed => System.Windows.FontStretches.SemiCondensed,
			Windows.UI.Text.FontStretch.SemiExpanded => System.Windows.FontStretches.SemiExpanded,
			Windows.UI.Text.FontStretch.UltraCondensed => System.Windows.FontStretches.UltraCondensed,
			Windows.UI.Text.FontStretch.UltraExpanded => System.Windows.FontStretches.UltraExpanded,
			_ => System.Windows.FontStretches.Normal
		};

	public static System.Windows.FontStyle ToWpfFontStyle(this Windows.UI.Text.FontStyle fontStyle) =>
		fontStyle switch
		{
			Windows.UI.Text.FontStyle.Normal => System.Windows.FontStyles.Normal,
			Windows.UI.Text.FontStyle.Italic => System.Windows.FontStyles.Italic,
			Windows.UI.Text.FontStyle.Oblique => System.Windows.FontStyles.Oblique,
			_ => System.Windows.FontStyles.Normal
		};

	public static System.Windows.Media.FontFamily ToWpfFontFamily(this Windows.UI.Xaml.Media.FontFamily fontFamily) =>
		new System.Windows.Media.FontFamily(fontFamily.Source);
}
