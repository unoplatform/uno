#nullable enable

namespace Windows.UI.Composition
{
	public partial class CompositionGeometricClip : CompositionClip
	{
		private CompositionViewBox? _viewBox;
		private CompositionGeometry? _geometry;

		public CompositionGeometricClip(Compositor compositor) : base(compositor)
		{

		}

		public CompositionViewBox? ViewBox
		{
			get => _viewBox;
			set => SetProperty(ref _viewBox, value);
		}

		public CompositionGeometry? Geometry
		{
			get => _geometry;
			set => SetProperty(ref _geometry, value);
		}
	}
}
