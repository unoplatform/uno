using System;
using System.Collections.Generic;
using System.Text;
using SkiaSharp;
using Windows.UI.Xaml.Controls;

#nullable enable

namespace Windows.UI.Composition
{
	internal class SkiaTextBoxVisual : Visual
	{
		private readonly SkiaTextBoxView _view;

		public SkiaTextBoxVisual(Compositor compositor, SkiaTextBoxView view) : base(compositor)
		{
			_view = view;
		}

		internal override void Render(SKSurface surface)
		{
			_view.Inlines.Render(surface, Compositor);

			//TODO: render carat / text selection
		}
	}
}
