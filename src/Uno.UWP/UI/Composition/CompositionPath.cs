#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using Windows.Graphics;

namespace Windows.UI.Composition
{
	public partial class CompositionPath : IGeometrySource2D
	{
		public CompositionPath(IGeometrySource2D source)
		{
			GeometrySource = source;
		}

		internal IGeometrySource2D GeometrySource { get; }
	}
}
