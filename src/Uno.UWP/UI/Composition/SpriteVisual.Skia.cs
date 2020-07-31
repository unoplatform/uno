#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using SkiaSharp;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Logging;
using Windows.UI.WebUI;

namespace Windows.UI.Composition
{
	public partial class SpriteVisual : ContainerVisual
	{
		private readonly SerialDisposable _csbSubscription = new SerialDisposable();

		private readonly SKPaint _paint
			= new SKPaint()
			{
				IsAntialias = true,
			};

		partial void OnBrushChangedPartial(CompositionBrush? brush)
		{
			_csbSubscription.Disposable = null;

			if (brush is CompositionSurfaceBrush csb)
			{
				csb.PropertyChanged += UpdatePaint;
				_csbSubscription.Disposable = Disposable.Create(() => csb.PropertyChanged -= UpdatePaint);
			}

			UpdatePaint();
		}

		private void UpdatePaint()
		{
			Brush?.UpdatePaint(_paint);
		}

		internal override void Render(SKSurface surface, SKImageInfo info)
		{
			base.Render(surface, info);

			surface.Canvas.Save();

			surface.Canvas.DrawRect(
				new SKRect(left: 0, top: 0, right: Size.X, bottom: Size.Y),
				_paint
			);

			surface.Canvas.Restore();
		}
	}
}
