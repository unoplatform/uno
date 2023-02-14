#nullable enable

using Windows.Graphics;

namespace Microsoft.UI.Composition
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
