#nullable enable

namespace Windows.UI.Composition
{
	public partial class SpriteVisual : ContainerVisual
	{
		private CompositionBrush? _brush;

		public SpriteVisual(Compositor compositor) : base(compositor)
		{

		}

		public CompositionBrush? Brush
		{
			get => _brush;
			set => SetProperty(ref _brush, value);
		}

		private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
		{
			// Call base implementation - Visual calls Compositor.InvalidateRender().
			base.OnPropertyChangedCore(propertyName, isSubPropertyChange);

			switch (propertyName)
			{
				case nameof(Brush):
					OnBrushChangedPartial(Brush);
					break;
				default:
					break;
			}
		}

		partial void OnBrushChangedPartial(CompositionBrush? brush);
	}
}
