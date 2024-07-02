#if UNO_HAS_BORDER_VISUAL
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Uno.UI.Xaml.Controls;

namespace Windows.UI.Xaml;

internal static class BorderHelper
{
	public static void UpdateCornerRadius(this IBorderInfoProvider @this)
	{
		@this.BorderVisual.CornerRadius = @this.CornerRadius.ToUnoCompositionCornerRadius();
	}

	public static void UpdateBorderThickness(this IBorderInfoProvider @this)
	{
		// We're using a case instead of a generic <T> where T : UIElement, IBorderInfoProvider
		// but we use this with with arguments that are only UIElements (so even if you cast, you can't get both UIElement and IBorderInfoProvider)
		var thickness = @this is UIElement ue && ue.GetUseLayoutRounding()
			? ue.LayoutRound(@this.BorderThickness)
			: @this.BorderThickness;
		@this.BorderVisual.BorderThickness = thickness.ToUnoCompositionThickness();
	}

	public static void UpdateBackgroundSizing(this IBorderInfoProvider @this)
	{
		@this.BorderVisual.UseInnerBorderBoundsAsAreaForBackground = @this.BackgroundSizing == BackgroundSizing.InnerBorderEdge;
	}

	public static void UpdateBackground(this IBorderInfoProvider @this)
	{
		@this.BackgroundBrushSubscriptionDisposable.Disposable = Brush.AssignAndObserveBrush(@this.Background, @this.BorderVisual.Compositor, brush => @this.BorderVisual.BackgroundBrush = brush);
	}

	public static void UpdateBorderBrush(this IBorderInfoProvider @this)
	{
		@this.BorderBrushSubscriptionDisposable.Disposable = Brush.AssignAndObserveBrush(@this.BorderBrush, @this.BorderVisual.Compositor, brush => @this.BorderVisual.BorderBrush = brush);
	}

	public static void UpdateAllBorderProperties(this IBorderInfoProvider @this)
	{
		@this.UpdateBorderBrush();
		@this.UpdateBackground();
		@this.UpdateCornerRadius();
		@this.UpdateBackgroundSizing();
		@this.UpdateBorderThickness();
	}
}
#endif
