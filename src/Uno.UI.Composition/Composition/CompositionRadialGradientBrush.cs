#nullable enable

using System.Numerics;

namespace Windows.UI.Composition
{
	public partial class CompositionRadialGradientBrush : CompositionGradientBrush
	{
		private Vector2 _gradientOriginOffset = Vector2.Zero;
		private Vector2 _ellipseRadius = new Vector2(0.5f, 0.5f);
		private Vector2 _ellipseCenter = new Vector2(0.5f, 0.5f);

		internal CompositionRadialGradientBrush(Compositor compositor)
			: base(compositor)
		{

		}

		public Vector2 GradientOriginOffset
		{
			get => _gradientOriginOffset;
			set => SetProperty(ref _gradientOriginOffset, value);
		}

		public Vector2 EllipseRadius
		{
			get => _ellipseRadius;
			set => SetProperty(ref _ellipseRadius, value);
		}

		public Vector2 EllipseCenter
		{
			get => _ellipseCenter;
			set => SetProperty(ref _ellipseCenter, value);
		}
	}
}
