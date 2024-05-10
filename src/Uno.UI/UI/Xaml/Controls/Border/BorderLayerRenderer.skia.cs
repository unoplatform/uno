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
	partial void UpdatePlatform()
	{
		var newState = new BorderLayerState(
			new Size(_owner.ActualWidth, _owner.ActualHeight),
			_borderInfoProvider.Background,
			_borderInfoProvider.BackgroundSizing,
			_borderInfoProvider.BorderBrush,
			_borderInfoProvider.BorderThickness,
			_borderInfoProvider.CornerRadius);

		if (newState.Background != null ||
			newState.CornerRadius != CornerRadius.None ||
			(newState.BorderThickness != Thickness.Empty && newState.BorderBrush != null))
		{

			UpdateBorderAndBackground(newState);
		}
	}

	/// <summary>
	/// Removes the added brush subscriptions during a call to <see cref="UpdatePlatform" />.
	/// </summary>
	partial void ClearPlatform()
	{
		_borderBackgroundBrushDisposable.Disposable = null;
		_borderShapeBrushDisposable.Disposable = null;
	}

	private void UpdateBorderAndBackground(BorderLayerState state)
	{
		var area = new Rect(default, state.ElementSize);

		// In case the element has no size, skip everything!
		if (area.Width == 0 && area.Height == 0)
		{
			return;
		}

		if (_owner.Visual is not BorderVisual visual)
		{
			throw new InvalidOperationException($"{nameof(BorderLayerRenderer)} should only be used with UIElements that use a {nameof(BorderVisual)}.");
		}

		visual.CornerRadius = state.CornerRadius.ToUnoCompositionCornerRadius();
		visual.BorderThickness = state.BorderThickness.ToUnoCompositionThickness();
		visual.UseInnerBorderBoundsAsAreaForBackground = state.BackgroundSizing == BackgroundSizing.InnerBorderEdge;

		var borderThickness = state.BorderThickness;
		if (_owner.GetUseLayoutRounding())
		{
			borderThickness = _owner.LayoutRound(borderThickness);
		}

		var compositor = visual.Compositor;

		// Border background (if any)
		_borderBackgroundBrushDisposable.Disposable = null;
		if (state.Background is { } background)
		{
			var backgroundShape = visual.BackgroundShape;
			_borderBackgroundBrushDisposable.Disposable = Brush.AssignAndObserveBrush(background, compositor, brush => backgroundShape.FillBrush = brush);
		}

		// Border shape (if any)
		_borderShapeBrushDisposable.Disposable = null;
		if (borderThickness != Thickness.Empty && state.BorderBrush is { } borderBrush)
		{
			var borderShape = visual.BorderShape;
			// Border brush
			_borderShapeBrushDisposable.Disposable = Brush.AssignAndObserveBrush(borderBrush, compositor, brush => borderShape.FillBrush = brush);
		}
	}
}
