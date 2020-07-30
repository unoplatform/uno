using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	public partial class CornerRadiusToThicknessConverter: DependencyObject, IValueConverter
	{
		public static DependencyProperty ConversionKindProperty { get ; } = DependencyProperty.Register(
			"ConversionKind", typeof(CornerRadiusToThicknessConverterKind), typeof(CornerRadiusToThicknessConverter), new FrameworkPropertyMetadata(CornerRadiusToThicknessConverterKind.FilterLeftAndRightFromTop));

		public CornerRadiusToThicknessConverterKind ConversionKind
		{
			get => (CornerRadiusToThicknessConverterKind)GetValue(ConversionKindProperty);
			set => SetValue(ConversionKindProperty, value);
		}

		private object Convert(CornerRadius radius, CornerRadiusToThicknessConverterKind filterKind)
		{
			var result = new Thickness{ };

			switch (filterKind)
			{
				case CornerRadiusToThicknessConverterKind.FilterLeftAndRightFromTop:
					result.Left = radius.TopLeft;
					result.Right = radius.TopRight;
					result.Top = 0;
					result.Bottom = 0;
					break;
				case CornerRadiusToThicknessConverterKind.FilterLeftAndRightFromBottom:
					result.Left = radius.BottomLeft;
					result.Right = radius.BottomRight;
					result.Top = 0;
					result.Bottom = 0;
					break;
				case CornerRadiusToThicknessConverterKind.FilterTopAndBottomFromLeft:
					result.Left = 0;
					result.Right = 0;
					result.Top = radius.TopLeft;
					result.Bottom = radius.BottomLeft;
					break;
				case CornerRadiusToThicknessConverterKind.FilterTopAndBottomFromRight:
					result.Left = 0;
					result.Right = 0;
					result.Top = radius.TopRight;
					result.Bottom = radius.BottomRight;
					break;
			}

			return result;
		}

		public object Convert(object value, Type targetType, object parameter, string language)
			=> value is CornerRadius radius ? Convert(radius, ConversionKind) : null;

		public object ConvertBack(object value, Type targetType, object parameter, string language)
			=> throw new NotSupportedException();
	}
}
