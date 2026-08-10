using SkiaSharp;
using Uno.UI.Composition;
#nullable enable

namespace Microsoft.UI.Composition
{
	public partial class RedirectVisual : ContainerVisual
	{
		private Visual? _source;

		public RedirectVisual(Compositor compositor) : base(compositor)
		{

		}

		public Visual? Source
		{
			get => _source;
			set => SetProperty(ref _source, value);
		}

		private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
		{
			// Call base implementation - Visual calls Compositor.InvalidateRender().
			base.OnPropertyChangedCore(propertyName, isSubPropertyChange);

			switch (propertyName)
			{
				case nameof(Source):
					OnSourceChangedPartial(Source);
					break;
				default:
					break;
			}
		}

		partial void OnSourceChangedPartial(Visual? source);

		internal override SKPath? Paint(in PaintingSession session)
		{
			base.Paint(in session);

			if (Source is not null && session.Canvas is { } canvas)
			{
				Source.RenderRootVisual(canvas, null);
			}

			return null;
		}

		internal override bool CanPaint() => Source?.CanPaint() ?? false;
		internal override bool RequiresRepaintOnEveryFrame => true;
	}
}
