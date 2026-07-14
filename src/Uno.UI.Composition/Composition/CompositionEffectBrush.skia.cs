#nullable enable

using System;
using SkiaSharp;
using Windows.Foundation;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Composition;

public partial class CompositionEffectBrush : CompositionBrush
{
	private Rect _currentBounds;
	private bool? _currentCompMode;
	private IEffectFilter? _filter;
	private bool _hasBackdropBrushInput;

	internal bool HasBackdropBrushInput
	{
		get => _hasBackdropBrushInput;
		private set => SetProperty(ref _hasBackdropBrushInput, value);
	}

	internal override bool RequiresRepaintOnEveryFrame => HasBackdropBrushInput;

	internal bool UseBackdropBlurClamp { get; set; }

	internal override void Paint(SKCanvas canvas, float opacity, SKRect bounds)
	{
		UpdateFilter(bounds.ToRect());
		if (_filter is { } filter)
		{
			new SkiaDrawingSession(canvas).DrawEffectBackdrop(filter, opacity);
		}
	}

	internal override bool TryPaint(IDrawingSession session, float opacity, Rect bounds)
	{
		UpdateFilter(bounds);
		if (_filter is { } filter)
		{
			session.DrawEffectBackdrop(filter, opacity);
		}
		return true;
	}

	private void UpdateFilter(Rect bounds)
	{
		if (_currentBounds != bounds || _filter is null || Compositor.IsSoftwareRenderer != _currentCompMode)
		{
			_filter?.Dispose();
			_filter = DrawingBackend.Current.CreateEffectFilter(
				_effect,
				bounds,
				GetSourceParameter,
				UseBackdropBlurClamp,
				Compositor.IsSoftwareRenderer is true,
				out var hasBackdropInput)
				?? throw new NotSupportedException($"Unsupported effect description.\r\nEffect name: {_effect.Name}");
			HasBackdropBrushInput = hasBackdropInput;
			_currentBounds = bounds;
			_currentCompMode = Compositor.IsSoftwareRenderer;
		}
	}

	private protected override void DisposeInternal()
	{
		base.DisposeInternal();

		_filter?.Dispose();
	}

	internal override bool CanPaint() => _effect is not null;
}
