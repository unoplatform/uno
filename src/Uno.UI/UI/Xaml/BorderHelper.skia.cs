#if UNO_HAS_BORDER_VISUAL
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml.Controls;
using Windows.UI;

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

	public static void SetUpBrushTransitionIfAllowed(BorderVisual visual, Brush fromBrush, Brush toBrush, BrushTransition transition, bool isAnimation)
	{
		if (transition is null)
		{
			return;
		}

		if (isAnimation)
		{
			// When an animation sets the background, it skips the transition logic and is applied immediately.
			// Stop any running brush transition animation.
			if (visual.BackgroundBrush is CompositionColorBrush animatingBrush)
			{
				animatingBrush.StopAnimation("Color");
			}

			return;
		}

		var oldBrush = fromBrush as SolidColorBrush;
		var newBrush = toBrush as SolidColorBrush;

		var oldBrushIsNullOrSolidColorBrush = oldBrush is not null || fromBrush is null;

		if (newBrush is not null
			&& oldBrushIsNullOrSolidColorBrush
			&& oldBrush != newBrush)
		{
			// Get the from-color. For hand-off scenarios (brush changing mid-transition),
			// the old CompositionColorBrush's Color holds the current animated value.
			var fromColor = (oldBrush?.GetOrCreateCompositionBrush(visual.Compositor) as CompositionColorBrush)?.Color
				?? Colors.Transparent;

			var toColor = newBrush.Color;

			// The new CompositionColorBrush was already set on the visual by UpdateBackground().
			// Start a ColorKeyFrameAnimation on it to animate from fromColor to toColor.
			if (visual.BackgroundBrush is CompositionColorBrush colorBrush)
			{
				var animation = transition.CreateAnimation(visual.Compositor, fromColor, toColor);
				colorBrush.StartAnimation("Color", animation);
			}
		}
	}
}
#endif
