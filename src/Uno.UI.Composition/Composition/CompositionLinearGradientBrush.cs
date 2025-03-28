#nullable enable

using System.Numerics;

namespace Windows.UI.Composition
{
	public partial class CompositionLinearGradientBrush : CompositionGradientBrush
	{
		private Vector2 _startPoint = Vector2.Zero;
		private Vector2 _endPoint = new Vector2(1, 0);

		internal CompositionLinearGradientBrush(Compositor compositor)
			: base(compositor)
		{

		}

		public Vector2 StartPoint
		{
			get => _startPoint;
			set => SetProperty(ref _startPoint, value);
		}

		public Vector2 EndPoint
		{
			get => _endPoint;
			set => SetProperty(ref _endPoint, value);
		}
	}
}
