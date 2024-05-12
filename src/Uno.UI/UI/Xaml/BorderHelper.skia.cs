using System;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml;

internal static class BorderHelper
{
	public static void UpdateCornerRadius<T>(this T @this) where T : UIElement, IBorderInfoProvider
	{
		if (@this.Visual is not BorderVisual visual)
		{
			throw new InvalidOperationException($"UIElements that have a corner radius should use a {nameof(BorderVisual)}.");
		}
		visual.CornerRadius = @this.CornerRadius.ToUnoCompositionCornerRadius();
	}

	public static void UpdateBorderThickness<T>(this T @this) where T : UIElement, IBorderInfoProvider
	{
		if (@this.Visual is not BorderVisual visual)
		{
			throw new InvalidOperationException($"UIElements that have a border thickness should use a {nameof(BorderVisual)}.");
		}
		visual.BorderThickness =
			(@this.GetUseLayoutRounding() ? @this.LayoutRound(@this.BorderThickness) : @this.BorderThickness)
			.ToUnoCompositionThickness();
	}

	public static void UpdateBackgroundSizing<T>(this T @this) where T : UIElement, IBorderInfoProvider
	{
		if (@this.Visual is not BorderVisual visual)
		{
			throw new InvalidOperationException($"UIElements that have a border sizing should use a {nameof(BorderVisual)}.");
		}
		visual.UseInnerBorderBoundsAsAreaForBackground = @this.BackgroundSizing == BackgroundSizing.InnerBorderEdge;
	}

	public static void UpdateBackground<T>(this T @this) where T : UIElement, IBorderInfoProvider
	{
		if (@this.Visual is not BorderVisual visual)
		{
			throw new InvalidOperationException($"UIElements that have a background should use a {nameof(BorderVisual)}.");
		}
		@this.BorderBrushSubscriptionDisposable.Disposable = Brush.AssignAndObserveBrush(@this.Background, visual.Compositor, brush => visual.BackgroundBrush = brush);
	}

	public static void UpdateBorderBrush<T>(this T @this) where T : UIElement, IBorderInfoProvider
	{
		if (@this.Visual is not BorderVisual visual)
		{
			throw new InvalidOperationException($"UIElements that have a border should use a {nameof(BorderVisual)}.");
		}
		@this.BorderBrushSubscriptionDisposable.Disposable = Brush.AssignAndObserveBrush(@this.BorderBrush, visual.Compositor, brush => visual.BorderBrush = brush);
	}

	public static void UpdateAllBorderProperties<T>(this T @this) where T : UIElement, IBorderInfoProvider
	{
		@this.UpdateBorderBrush();
		@this.UpdateBackground();
		@this.UpdateCornerRadius();
		@this.UpdateBackgroundSizing();
		@this.UpdateBorderThickness();
	}
}
