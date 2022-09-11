#nullable enable

using System.Numerics;

namespace Windows.UI.Composition
{
	public partial class CompositionRectangleGeometry : CompositionGeometry
	{
		private Vector2 _size;
		private Vector2 _offset;

		internal CompositionRectangleGeometry(Compositor compositor) : base(compositor)
		{

		}

		public Vector2 Size
		{
			get => _size;
			set => SetProperty(ref _size, value);
		}

		public Vector2 Offset
		{
			get => _offset;
			set => SetProperty(ref _offset, value);
		}
	}
}
