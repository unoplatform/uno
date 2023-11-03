#nullable enable

using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Shapes
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
					options: FrameworkPropertyMetadataOptions.ValueInheritsDataContext | FrameworkPropertyMetadataOptions.LogicalChild | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange
				)
			);

		#endregion

#if __NETSTD_REFERENCE__
		protected override Size MeasureOverride(Size availableSize) => base.MeasureOverride(availableSize);
		protected override Size ArrangeOverride(Size finalSize) => base.ArrangeOverride(finalSize);
#endif
	}
}
