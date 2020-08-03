#nullable enable

namespace Windows.UI.Composition
{
	public partial class CompositionGeometricClip : global::Windows.UI.Composition.CompositionClip
	{
		public CompositionGeometricClip(Compositor compositor) : base(compositor)
		{

		}
		public CompositionViewBox? ViewBox
		{
			get; set;
		}

		public CompositionGeometry? Geometry
		{
			get; set;
		}
	}
}
