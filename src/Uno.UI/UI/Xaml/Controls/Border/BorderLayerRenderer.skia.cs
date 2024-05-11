using System;
using Uno.Disposables;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

partial class BorderLayerRenderer
{
	/// <summary>
	/// Updates or creates the owner's Visual to render a border-like shape.
	/// </summary>
	partial void UpdatePlatform() => UpdateBorderAndBackground();

	/// <summary>
	/// Removes the added brush subscriptions during a call to <see cref="UpdatePlatform" />.
	/// </summary>
	partial void ClearPlatform()
	{
		_borderInfoProvider.BackgroundBrushSubscriptionDisposable.Disposable = null;
		_borderInfoProvider.BorderBrushSubscriptionDisposable.Disposable = null;
	}

	private void UpdateBorderAndBackground()
	{
		_borderInfoProvider.UpdateCornerRadius();
		_borderInfoProvider.UpdateBorderThickness();
		_borderInfoProvider.UpdateBorderSizing();
		_borderInfoProvider.UpdateBackground();
		_borderInfoProvider.UpdateBorderBrush();
	}
}

internal static class BorderHelper
{
	public static void UpdateCornerRadius<T>(this T @this) where T : IBorderInfoProvider
	{
		if ((@this as UIElement)!.Visual is not BorderVisual visual)
		{
			throw new InvalidOperationException($"UIElements that have a corner radius should use a {nameof(BorderVisual)}.");
		}
		visual.CornerRadius = @this.CornerRadius.ToUnoCompositionCornerRadius();
	}

	public static void UpdateBorderThickness<T>(this T @this) where T : IBorderInfoProvider
	{
		if ((@this as UIElement)!.Visual is not BorderVisual visual)
		{
			throw new InvalidOperationException($"UIElements that have a border thickness should use a {nameof(BorderVisual)}.");
		}
		visual.BorderThickness = @this.BorderThickness.ToUnoCompositionThickness();
	}

	public static void UpdateBorderSizing<T>(this T @this) where T : IBorderInfoProvider
	{
		if ((@this as UIElement)!.Visual is not BorderVisual visual)
		{
			throw new InvalidOperationException($"UIElements that have a border sizing should use a {nameof(BorderVisual)}.");
		}
		visual.UseInnerBorderBoundsAsAreaForBackground = @this.BackgroundSizing == BackgroundSizing.InnerBorderEdge;
	}

	public static void UpdateBackground<T>(this T @this) where T : IBorderInfoProvider
	{
		if ((@this as UIElement)!.Visual is not BorderVisual visual)
		{
			throw new InvalidOperationException($"UIElements that have a background should use a {nameof(BorderVisual)}.");
		}
		@this.BorderBrushSubscriptionDisposable.Disposable = Brush.AssignAndObserveBrush(@this.Background, visual.Compositor, brush => visual.BackgroundBrush = brush);
	}

	public static void UpdateBorderBrush<T>(this T @this) where T : IBorderInfoProvider
	{
		if ((@this as UIElement)!.Visual is not BorderVisual visual)
		{
			throw new InvalidOperationException($"UIElements that have a background should use a {nameof(BorderVisual)}.");
		}
		@this.BorderBrushSubscriptionDisposable.Disposable = Brush.AssignAndObserveBrush(@this.BorderBrush, visual.Compositor, brush => visual.BorderBrush = brush);
	}
}
