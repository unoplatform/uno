#nullable enable

using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Composition;

namespace Microsoft.UI.Xaml.Shapes
{
	public partial class Path : Shape
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

		private CompositionPathGeometry? _fillGeometry;

		/// <inheritdoc />
		protected override Size MeasureOverride(Size availableSize)
			=> MeasureAbsoluteShape(availableSize, GetPath());

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
			=> ArrangeAbsoluteShape(finalSize, GetPath());

		private SkiaGeometrySource2D? GetPath() => Data?.GetGeometrySource2D();

		private protected override void Render(SkiaGeometrySource2D? path, double? scaleX = null, double? scaleY = null, double? renderOriginX = null,
			double? renderOriginY = null)
		{
			base.Render(path, scaleX, scaleY, renderOriginX, renderOriginY);

			_fillGeometry ??= Visual.Compositor.CreatePathGeometry();
			SpriteShape.FillGeometry = _fillGeometry;
			if (Data?.GetTransformedFilledSKPath() is { } filledPath)
			{
				_fillGeometry.Path = new CompositionPath(new SkiaGeometrySource2D(filledPath));
			}
			else
			{
				_fillGeometry.Path = null;
			}
		}

		#endregion

	}
}
