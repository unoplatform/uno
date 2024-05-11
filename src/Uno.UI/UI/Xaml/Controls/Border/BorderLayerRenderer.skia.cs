using System;
using Uno.Disposables;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

partial class BorderLayerRenderer
{
	private readonly SerialDisposable _borderShapeBrushDisposable = new SerialDisposable();
	private readonly SerialDisposable _borderBackgroundBrushDisposable = new SerialDisposable();

	/// <summary>
	/// Updates or creates the owner's Visual to render a border-like shape.
	/// </summary>
	partial void UpdatePlatform() => UpdateBorderAndBackground();

	/// <summary>
	/// Removes the added brush subscriptions during a call to <see cref="UpdatePlatform" />.
	/// </summary>
	partial void ClearPlatform()
	{
		_borderBackgroundBrushDisposable.Disposable = null;
		_borderShapeBrushDisposable.Disposable = null;
	}

	private void UpdateBorderAndBackground()
	{
		var area = new Rect(default, new Size(_owner.ActualWidth, _owner.ActualHeight));

		// In case the element has no size, skip everything!
		if (area is { Width: 0, Height: 0 })
		{
			return;
		}

		if (_owner.Visual is not BorderVisual visual)
		{
			throw new InvalidOperationException($"{nameof(BorderLayerRenderer)} should only be used with UIElements that use a {nameof(BorderVisual)}.");
		}

		visual.CornerRadius = _borderInfoProvider.CornerRadius.ToUnoCompositionCornerRadius();
		visual.BorderThickness = _borderInfoProvider.BorderThickness.ToUnoCompositionThickness();
		visual.UseInnerBorderBoundsAsAreaForBackground = _borderInfoProvider.BackgroundSizing == BackgroundSizing.InnerBorderEdge;

		var borderThickness = _borderInfoProvider.BorderThickness;
		if (_owner.GetUseLayoutRounding())
		{
			borderThickness = _owner.LayoutRound(borderThickness);
		}

		var compositor = visual.Compositor;

		_borderBackgroundBrushDisposable.Disposable = null;
		if (_borderInfoProvider.Background is { } background)
		{
			_borderBackgroundBrushDisposable.Disposable = Brush.AssignAndObserveBrush(background, compositor, brush => visual.BackgroundBrush = brush);
		}

		_borderShapeBrushDisposable.Disposable = null;
		if (borderThickness != Thickness.Empty && _borderInfoProvider.BorderBrush is { } borderBrush)
		{
			_borderShapeBrushDisposable.Disposable = Brush.AssignAndObserveBrush(borderBrush, compositor, brush => visual.BorderBrush = brush);
		}
	}
}
