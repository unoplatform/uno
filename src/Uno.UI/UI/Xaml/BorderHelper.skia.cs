#if UNO_HAS_BORDER_VISUAL
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml;

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
		@this.BorderVisual.BackgroundBrush = @this.Background?.GetOrCreateCompositionBrush(@this.BorderVisual.Compositor);
	}

	public static void UpdateBorderBrush(this IBorderInfoProvider @this)
	{
		@this.BorderVisual.BorderBrush = @this.BorderBrush?.GetOrCreateCompositionBrush(@this.BorderVisual.Compositor);
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
