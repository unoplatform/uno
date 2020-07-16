using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	public partial class CornerRadiusFilterConverter : DependencyObject, IValueConverter
	{
		public static DependencyProperty FilterProperty { get ; } = DependencyProperty.Register(
			"Filter", typeof(CornerRadiusFilterKind), typeof(CornerRadiusFilterConverter), new FrameworkPropertyMetadata(CornerRadiusFilterKind.None));

		public CornerRadiusFilterKind Filter
		{
			get { return (CornerRadiusFilterKind)GetValue(FilterProperty); }
			set { SetValue(FilterProperty, value); }
		}


		private static CornerRadius Convert(CornerRadius radius, CornerRadiusFilterKind filterKind)
		{
			var result = radius;

			switch (filterKind)
			{
				case CornerRadiusFilterKind.Top:
					result.BottomLeft = 0;
					result.BottomRight = 0;
					break;
				case CornerRadiusFilterKind.Right:
					result.TopLeft = 0;
					result.BottomLeft = 0;
					break;
				case CornerRadiusFilterKind.Bottom:
					result.TopLeft = 0;
					result.TopRight = 0;
					break;
				case CornerRadiusFilterKind.Left:
					result.TopRight = 0;
					result.BottomRight = 0;
					break;
			}

			return result;
		}

		private static double GetDoubleValue(CornerRadius radius, CornerRadiusFilterKind filterKind)
			=>
				filterKind switch
				{
					CornerRadiusFilterKind.TopLeftValue => radius.TopLeft,
					CornerRadiusFilterKind.BottomRightValue => radius.BottomRight,
					_ => 0d
				};

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			var filter = Filter;

			if (value is CornerRadius cornerRadius)
			{
				if (filter == CornerRadiusFilterKind.TopLeftValue || filter == CornerRadiusFilterKind.BottomRightValue)
				{
					return GetDoubleValue(cornerRadius, filter);
				}
				return Convert(cornerRadius, filter);
			}

			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotSupportedException();
		}
	}
}
