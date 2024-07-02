using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Foundation;
using Windows.UI.Xaml.Media;


namespace Windows.UI.Xaml.Shapes
{
	public partial class Polyline
	{
		#region Points (DP)
		public PointCollection Points
		{
			get => (PointCollection)GetValue(PointsProperty);
			set => SetValue(PointsProperty, value);
		}

		public static global::Windows.UI.Xaml.DependencyProperty PointsProperty { get; } = DependencyProperty.Register(
			"Points",
			typeof(PointCollection),
			typeof(Polyline),
			new FrameworkPropertyMetadata(
				defaultValue: default(PointCollection),
				options: FrameworkPropertyMetadataOptions.LogicalChild | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange,
				propertyChangedCallback: (s, e) =>
				{
					(e.OldValue as PointCollection)?.UnRegisterChangedListener(s.InvalidateMeasure);
					(e.NewValue as PointCollection)?.RegisterChangedListener(s.InvalidateMeasure);
				}
			)
		);
		#endregion

		public Polyline()
#if __WASM__
			: base("polyline")
#endif
		{
			Points = new PointCollection();
		}

#if __NETSTD_REFERENCE__
		protected override Size MeasureOverride(Size availableSize) => base.MeasureOverride(availableSize);
		protected override Size ArrangeOverride(Size finalSize) => base.ArrangeOverride(finalSize);
#endif
	}
}
