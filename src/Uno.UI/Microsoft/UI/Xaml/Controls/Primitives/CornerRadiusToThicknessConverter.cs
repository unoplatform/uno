using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	public partial class CornerRadiusToThicknessConverter : DependencyObject, IValueConverter
	{
		public double Multiplier
		{
			get => (double)GetValue(MultiplierProperty);
			set => SetValue(MultiplierProperty, value);
		}

		public static DependencyProperty MultiplierProperty { get; } =
			DependencyProperty.Register(nameof(Multiplier), typeof(double), typeof(CornerRadiusToThicknessConverter), new PropertyMetadata(1.0));

		public CornerRadiusToThicknessConverterKind ConversionKind
		{
			get => (CornerRadiusToThicknessConverterKind)GetValue(ConversionKindProperty);
			set => SetValue(ConversionKindProperty, value);
		}

		public static DependencyProperty ConversionKindProperty { get; } = DependencyProperty.Register(
			nameof(ConversionKind), typeof(CornerRadiusToThicknessConverterKind), typeof(CornerRadiusToThicknessConverter), new FrameworkPropertyMetadata(CornerRadiusToThicknessConverterKind.FilterLeftAndRightFromTop));

		private static Thickness Convert(CornerRadius radius, CornerRadiusToThicknessConverterKind filterKind, double multiplier)
		{
			var result = new Thickness { };

			switch (filterKind)
			{
				case CornerRadiusToThicknessConverterKind.FilterLeftAndRightFromTop:
					result.Left = radius.TopLeft * multiplier;
					result.Right = radius.TopRight * multiplier;
					result.Top = 0;
					result.Bottom = 0;
					break;
				case CornerRadiusToThicknessConverterKind.FilterLeftAndRightFromBottom:
					result.Left = radius.BottomLeft * multiplier;
					result.Right = radius.BottomRight * multiplier;
					result.Top = 0;
					result.Bottom = 0;
					break;
				case CornerRadiusToThicknessConverterKind.FilterTopAndBottomFromLeft:
					result.Left = 0;
					result.Right = 0;
					result.Top = radius.TopLeft * multiplier;
					result.Bottom = radius.BottomLeft * multiplier;
					break;
				case CornerRadiusToThicknessConverterKind.FilterTopAndBottomFromRight:
					result.Left = 0;
					result.Right = 0;
					result.Top = radius.TopRight * multiplier;
					result.Bottom = radius.BottomRight * multiplier;
					break;
				case CornerRadiusToThicknessConverterKind.FilterTopFromTopLeft:
					result.Left = 0;
					result.Right = 0;
					result.Top = radius.TopLeft * multiplier;
					result.Bottom = 0;
					break;
				case CornerRadiusToThicknessConverterKind.FilterTopFromTopRight:
					result.Left = 0;
					result.Right = 0;
					result.Top = radius.TopRight * multiplier;
					result.Bottom = 0;
					break;
				case CornerRadiusToThicknessConverterKind.FilterRightFromTopRight:
					result.Left = 0;
					result.Right = radius.TopRight * multiplier;
					result.Top = 0;
					result.Bottom = 0;
					break;
				case CornerRadiusToThicknessConverterKind.FilterRightFromBottomRight:
					result.Left = 0;
					result.Right = radius.BottomRight * multiplier;
					result.Top = 0;
					result.Bottom = 0;
					break;
				case CornerRadiusToThicknessConverterKind.FilterBottomFromBottomRight:
					result.Left = 0;
					result.Right = 0;
					result.Top = 0;
					result.Bottom = radius.BottomRight * multiplier;
					break;
				case CornerRadiusToThicknessConverterKind.FilterBottomFromBottomLeft:
					result.Left = 0;
					result.Right = 0;
					result.Top = 0;
					result.Bottom = radius.BottomLeft * multiplier;
					break;
				case CornerRadiusToThicknessConverterKind.FilterLeftFromBottomLeft:
					result.Left = radius.BottomLeft * multiplier;
					result.Right = 0;
					result.Top = 0;
					result.Bottom = 0;
					break;
				case CornerRadiusToThicknessConverterKind.FilterLeftFromTopLeft:
					result.Left = radius.TopLeft * multiplier;
					result.Right = 0;
					result.Top = 0;
					result.Bottom = 0;
					break;
			}

			return result;
		}

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is CornerRadius radius)
			{
				var multiplier = Multiplier;
				return Convert(radius, ConversionKind, multiplier);
			}
			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
			=> throw new NotSupportedException();
	}
}
