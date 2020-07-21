#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	public partial class CompositionGeometricClip : global::Windows.UI.Composition.CompositionClip
	{
		public CompositionViewBox ViewBox
		{
			get; set;
		}

		public CompositionGeometry Geometry
		{
			get; set;
		}
	}
}
