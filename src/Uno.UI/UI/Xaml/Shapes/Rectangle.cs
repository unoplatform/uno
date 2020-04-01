#if !__IOS__ && !__MACOS__
#define LEGACY_SHAPE_MEASURE
#endif

using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Rectangle : Shape
	{

		#region RadiusY (DP)
		public static readonly DependencyProperty RadiusYProperty = DependencyProperty.Register(
			"RadiusY",
			typeof(double),
			typeof(Rectangle),
			new FrameworkPropertyMetadata(
				defaultValue: 0.0,
#if LEGACY_SHAPE_MEASURE
				propertyChangedCallback: (s, e) => ((Rectangle)s).OnRadiusYChangedPartial()
#else
				options: FrameworkPropertyMetadataOptions.AffectsArrange
#endif
			)
		);

		public double RadiusY
		{
			get => (double)this.GetValue(RadiusYProperty);
			set => this.SetValue(RadiusYProperty, value);
		}
		#endregion

		#region RadiusX (DP)
		public static readonly DependencyProperty RadiusXProperty = DependencyProperty.Register(
			"RadiusX",
			typeof(double),
			typeof(Rectangle),
			new FrameworkPropertyMetadata(
				defaultValue: 0.0,
#if LEGACY_SHAPE_MEASURE
				propertyChangedCallback: (s, e) => ((Rectangle)s).OnRadiusXChangedPartial()
#else
				options: FrameworkPropertyMetadataOptions.AffectsArrange
#endif
			)
		);

		public double RadiusX
		{
			get => (double)this.GetValue(RadiusXProperty);
			set => this.SetValue(RadiusXProperty, value);
		}
		#endregion

#if LEGACY_SHAPE_MEASURE
		partial void OnRadiusXChangedPartial();
		partial void OnRadiusYChangedPartial();
#endif
	}
}
