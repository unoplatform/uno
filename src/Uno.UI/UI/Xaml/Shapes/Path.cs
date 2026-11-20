#nullable enable

using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Composition;
using Windows.Graphics;
using Uno.UI.Composition.Drawing;

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
			_measuredPath = null;

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
			_measuredPath = null;
			InvalidateMeasure();
		}

		private CompositionPathGeometry? _fillGeometry;
		private IGeometry? _ownedFillPath;

		// The geometry built to measure, handed to the arrange that follows instead of being built again. Data changes
		// invalidate measure, so an arrange always follows the measure of the current Data.
		private IGeometry? _measuredPath;

		/// <inheritdoc />
		protected override Size MeasureOverride(Size availableSize)
			=> MeasureAbsoluteShape(availableSize, _measuredPath = GetPath());

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
		{
			var path = _measuredPath ?? GetPath();
			_measuredPath = null;
			// Every geometry builds a new path, except a StreamGeometry, which keeps the one it hands out unless a
			// transform maps it into a new one.
			RenderPathIsOwned = Data is not global::Uno.Media.StreamGeometry || Data.Transform is { MatrixCore.IsIdentity: false };
			return ArrangeAbsoluteShape(finalSize, path);
		}

		private IGeometry? GetPath() => Data?.GetTransformedGeometry();

		private protected override void Render(IGeometry? path, double? scaleX = null, double? scaleY = null, double? renderOriginX = null,
			double? renderOriginY = null)
		{
			base.Render(path, scaleX, scaleY, renderOriginX, renderOriginY);

			_fillGeometry ??= Visual.Compositor.CreatePathGeometry();
			SpriteShape.FillGeometry = _fillGeometry;
			// Only a PathGeometry has a separate fill geometry, built for this render alone.
			var previousFill = _ownedFillPath;
			_ownedFillPath = Data?.GetTransformedFilledGeometry();
			if (_ownedFillPath is IGeometrySource2D filledSource)
			{
				_fillGeometry.Path = new CompositionPath(filledSource);
			}
			else
			{
				_fillGeometry.Path = null;
			}
			previousFill?.Dispose();
		}

		#endregion

	}
}
