using System;

namespace Windows.UI.Composition
{
	public partial class SpriteVisual : ContainerVisual
	{
		private CompositionBrush _brush;

		public SpriteVisual(Compositor compositor) : base(compositor)
		{

		}

		public CompositionBrush Brush
		{
			get
			{
				return _brush;
			}
			set
			{
				var previousBrush = _brush;
				if (_brush != value)
				{
					_brush = value;
					OnBrushChangedPartial(_brush);
				}
			}
		}

		partial void OnBrushChangedPartial(CompositionBrush brush);
	}
}
