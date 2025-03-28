#nullable enable

using System.Numerics;

namespace Windows.UI.Composition
{
	public partial class CompositionRoundedRectangleGeometry : CompositionGeometry
	{
		private Vector2 _size;
		private Vector2 _offset;
		private Vector2 _cornerRadius;

		internal CompositionRoundedRectangleGeometry(Compositor compositor) : base(compositor)
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

		public Vector2 CornerRadius
		{
			get => _cornerRadius;
			set => SetProperty(ref _cornerRadius, value);
		}
	}
}
