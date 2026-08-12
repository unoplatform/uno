#nullable enable

using Windows.Foundation;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Shapes
{
	public partial class Path
	{
		#region Data

		public Geometry? Data
		{
			get => (Geometry)this.GetValue(DataProperty);
			set => this.SetValue(DataProperty, value);
		}

		public static DependencyProperty DataProperty { get; } =
			DependencyProperty.Register(
				"Data",
				typeof(Geometry),
				typeof(Path),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.ValueInheritsDataContext | FrameworkPropertyMetadataOptions.LogicalChild | FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: (s, e) => ((Path)s).OnDataChanged(e)
				)
			);

		private void OnDataChanged(DependencyPropertyChangedEventArgs e)
		{
			if (e.OldValue is Geometry oldGeometry)
			{
				oldGeometry.GeometryChanged -= OnDataGeometryChanged;
			}

			if (e.NewValue is Geometry newGeometry)
			{
				newGeometry.GeometryChanged += OnDataGeometryChanged;
			}
		}

		private void OnDataGeometryChanged()
		{
			InvalidateMeasure();
		}

		#endregion

#if __NETSTD_REFERENCE__
		protected override Size MeasureOverride(Size availableSize) => base.MeasureOverride(availableSize);
		protected override Size ArrangeOverride(Size finalSize) => base.ArrangeOverride(finalSize);
#endif
	}
}
