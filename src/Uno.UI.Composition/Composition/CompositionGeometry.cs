#nullable enable

using Windows.Graphics;

namespace Windows.UI.Composition
{
	public partial class CompositionGeometry : CompositionObject
	{
		internal CompositionGeometry(Compositor compositor) : base(compositor)
		{

		}

		public float TrimStart { get; set; }

		public float TrimOffset { get; set; }

		public float TrimEnd { get; set; }

		internal virtual IGeometrySource2D? BuildGeometry() => null;
	}
}
