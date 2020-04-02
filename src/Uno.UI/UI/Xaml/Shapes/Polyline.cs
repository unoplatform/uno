#if !__IOS__ && !__MACOS__
#define LEGACY_SHAPE_MEASURE
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.UI.Xaml.Media;


namespace Windows.UI.Xaml.Shapes
{
	public partial class Polyline
#if LEGACY_SHAPE_MEASURE
		: ArbitraryShapeBase
#endif
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
#if LEGACY_SHAPE_MEASURE
				propertyChangedCallback: (s, e) => ((Polyline)s).OnPointsChanged()
#else
				propertyChangedCallback: (s, e) =>
				{
					(e.OldValue as PointCollection)?.UnRegisterChangedListener(s.InvalidateMeasure);
					(e.NewValue as PointCollection)?.RegisterChangedListener(s.InvalidateMeasure);
				}
#endif
			)
		);
		#endregion

		public Polyline()
		{
			Points = new PointCollection();
			InitializePartial();
		}

		partial void InitializePartial();

#if LEGACY_SHAPE_MEASURE
		partial void OnPointsChanged();

		protected internal override IEnumerable<object> GetShapeParameters()
		{
			yield return Points?.ToArray();

			foreach (var p in base.GetShapeParameters())
			{
				yield return p;
			}
		}
#endif
	}
}
