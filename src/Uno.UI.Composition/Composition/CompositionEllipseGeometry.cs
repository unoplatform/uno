#nullable enable

using System.Numerics;

namespace Windows.UI.Composition
{
	public partial class CompositionEllipseGeometry : CompositionGeometry
	{
		private Vector2 _radius;
		private Vector2 _center;

		internal CompositionEllipseGeometry(Compositor compositor) : base(compositor)
		{

		}

		public Vector2 Radius
		{
			get => _radius;
			set => SetProperty(ref _radius, value);
		}

		public Vector2 Center
		{
			get => _center;
			set => SetProperty(ref _center, value);
		}
	}
}
