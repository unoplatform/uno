#nullable enable

using Windows.Graphics;

namespace Windows.UI.Composition
{
	public partial class CompositionPathGeometry : CompositionGeometry
	{
		private CompositionPath? _path;

		internal CompositionPathGeometry(Compositor compositor, CompositionPath? path = null) : base(compositor)
		{
			Path = path;
		}

		public CompositionPath? Path
		{
			get => _path;
			set => SetObjectProperty(ref _path, value);
		}

		internal override IGeometrySource2D? BuildGeometry() => Path?.GeometrySource;
	}
}
