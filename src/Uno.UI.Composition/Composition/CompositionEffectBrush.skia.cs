#nullable enable

using System;
using Windows.Foundation;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Composition;

public partial class CompositionEffectBrush : CompositionBrush
{
	private Rect _currentBounds;
	private IEffectFilter? _filter;
	private bool _hasBackdropBrushInput;

	internal bool HasBackdropBrushInput
	{
		get => _hasBackdropBrushInput;
		private set => SetProperty(ref _hasBackdropBrushInput, value);
	}

	internal override bool RequiresRepaintOnEveryFrame => HasBackdropBrushInput;

	internal bool UseBackdropBlurClamp { get; set; }

	internal override bool TryPaint(IDrawingSession session, float opacity, Rect bounds)
	{
		UpdateFilter(bounds);
		if (_filter is { } filter)
		{
			session.DrawEffectBackdrop(filter, opacity);
			return true;
		}
		// The backend couldn't realize the effect as a filter (e.g. the WebGPU backend for a per-pixel colour
		// effect). Fall back to the neutral "recipe": reduce the graph to a single source + composed 4×5 colour
		// matrix and paint the source with that matrix applied.
		if (TryGetWebGpuEffectRecipe(out var source, out _, out var solidColor, out var matrix))
		{
			if (solidColor is { } sc)
			{
				session.DrawRect(bounds, ApplyColorMatrix(sc, matrix));
			}
			else if (source is { } src)
			{
				var count = session.Save();
				session.SaveLayer(DrawingFactory.Current.CreateColorMatrixColorFilter(matrix));
				src.TryPaint(session, opacity, bounds);
				session.RestoreToCount(count);
			}
		}
		return true;
	}

	private void UpdateFilter(Rect bounds)
	{
		if (_currentBounds != bounds || _filter is null)
		{
			_filter?.Dispose();
			// A null filter means the backend can't realize this effect as a backdrop filter — TryPaint falls back
			// to the recipe path (a source + composed colour matrix). Not an error.
			_filter = DrawingFactory.Current.CreateEffectFilter(
				_effect,
				bounds,
				name => CompositionBrushEffectSource.From(GetSourceParameter(name)),
				UseBackdropBlurClamp,
				out var hasBackdropInput);
			HasBackdropBrushInput = hasBackdropInput;
			_currentBounds = bounds;
		}
	}

	private protected override void DisposeInternal()
	{
		base.DisposeInternal();

		_filter?.Dispose();
	}

	internal override bool CanPaint() => _effect is not null;
}
