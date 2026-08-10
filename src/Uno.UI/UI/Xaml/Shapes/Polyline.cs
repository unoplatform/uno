using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
using Uno.Media;
using Microsoft.UI.Composition;


namespace Microsoft.UI.Xaml.Shapes
{
	public partial class Polyline : Shape
	{
		#region Points (DP)
		public PointCollection Points
		{
			get => (PointCollection)GetValue(PointsProperty);
			set => SetValue(PointsProperty, value);
		}

		public static global::Microsoft.UI.Xaml.DependencyProperty PointsProperty { get; } = DependencyProperty.Register(
			"Points",
			typeof(PointCollection),
			typeof(Polyline),
			new FrameworkPropertyMetadata(
				defaultValue: default(PointCollection),
				options: FrameworkPropertyMetadataOptions.LogicalChild | FrameworkPropertyMetadataOptions.AffectsMeasure,
				propertyChangedCallback: (s, e) =>
				{
					(e.OldValue as PointCollection)?.UnRegisterChangedListener(s.InvalidateMeasure);
					(e.NewValue as PointCollection)?.RegisterChangedListener(s.InvalidateMeasure);
				}
			)
		);
		#endregion

		public Polyline()
		{
			Points = new PointCollection();
		}

		/// <inheritdoc />
		protected override Size MeasureOverride(Size availableSize)
			=> MeasureAbsoluteShape(availableSize, GetPath());

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
			=> ArrangeAbsoluteShape(finalSize, GetPath());

		private SkiaGeometrySource2D GetPath()
		{
			var points = Points;
			if (points == null || points.Count <= 1)
			{
				return null;
			}

			var streamGeometry = Uno.Media.GeometryHelper.Build(c =>
			{
				c.BeginFigure(points[0], true);
				for (var i = 1; i < points.Count; i++)
				{
					c.LineTo(points[i], true, false);
				}
			});

			return streamGeometry.GetGeometrySource2D();
		}

#if __NETSTD_REFERENCE__
		protected override Size MeasureOverride(Size availableSize) => base.MeasureOverride(availableSize);
		protected override Size ArrangeOverride(Size finalSize) => base.ArrangeOverride(finalSize);
#endif
	}
}
