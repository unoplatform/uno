#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition
{
	public partial class CompositionMaskBrush : CompositionBrush
	{
		private CompositionBrush? _source;
		private CompositionBrush? _mask;

		internal CompositionMaskBrush(Compositor compositor) : base(compositor)
		{

		}

		public CompositionBrush? Source
		{
			get => _source;
			set => SetProperty(ref _source, value);
		}

		public CompositionBrush? Mask
		{
			get => _mask;
			set => SetProperty(ref _mask, value);
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
				case nameof(Mask):
					OnMaskChangedPartial(Mask);
					break;
				default:
					break;
			}
		}

		partial void OnSourceChangedPartial(CompositionBrush? source);
		partial void OnMaskChangedPartial(CompositionBrush? mask);

		internal override bool RequiresRepaintOnEveryFrame => Source is not null && Mask is not null && (Source.RequiresRepaintOnEveryFrame || Mask.RequiresRepaintOnEveryFrame);
		internal override float DamageRegionSamplingMargin => global::System.Math.Max(Source?.DamageRegionSamplingMargin ?? 0, Mask?.DamageRegionSamplingMargin ?? 0);

		internal override void Paint(SKCanvas canvas, float opacity, SKRect bounds)
		{
			if (Source is null || Mask is null)
			{
				return;
			}
			_spareResultPaint.Reset();
			_spareResultPaint.IsAntialias = true;
			_spareResultPaint.BlendMode = SKBlendMode.SrcOver;
			_spareResultPaint2.Reset();
			_spareResultPaint2.IsAntialias = true;
			_spareResultPaint2.BlendMode = SKBlendMode.DstIn;
			// The first SaveLayer call along with DrawColor(Transparent) basically create a clean secondary drawing surface
			// but without having to call SKSurface.Create and having to deal with all the details like HWA.
			canvas.SaveLayer(new SKCanvasSaveLayerRec { Paint = _spareResultPaint });
			canvas.ClipRect(bounds, antialias: true);
			canvas.DrawColor(SKColors.Transparent);
			Source.Paint(canvas, opacity, bounds);
			// The second SaveLayer call with SKBlendMode.DstIn creates the masking effect
			canvas.SaveLayer(new SKCanvasSaveLayerRec { Paint = _spareResultPaint2 });
			Mask.Paint(canvas, opacity, bounds);
			canvas.Restore();
			canvas.Restore();
		}

		internal override bool CanPaint() => (Source?.CanPaint() ?? false) || (Mask?.CanPaint() ?? false);

		private static readonly SKPaint _spareResultPaint = new SKPaint();
		private static readonly SKPaint _spareResultPaint2 = new SKPaint();
	}
}
