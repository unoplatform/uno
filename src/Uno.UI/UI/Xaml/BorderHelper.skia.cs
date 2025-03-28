#if UNO_HAS_BORDER_VISUAL
using Windows.UI.Composition;
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

		// Begin Uno Specific
		if (isAnimation)
		{
			// When an animation sets the background, it skips the transition logic and is applied immediately.
			// However, it "deactivates" the currently active transition if one exists. Once the animation is done,
			// the deactivated transition is reactivated and continues as if it was running (i.e. it's not paused,
			// but takes into account the duration during which it was deactivated).
			visual.Compositor.DeactivateBackgroundTransition(visual);
			return;
		}
		// End Uno Specific

		var oldBrush = fromBrush as SolidColorBrush;
		var newBrush = toBrush as SolidColorBrush;

		var oldBrushIsNullOrSolidColorBrush = oldBrush is not null || fromBrush is null;
		//var oldBrushIsNullOrStatic = oldBrush == null || !oldBrush->IsEffectiveValueInSparseStorage(KnownPropertyIndex::SolidColorBrush_ColorAnimation);

		if (// The new brush must exist and must be different from the old brush. The old brush can be either a SolidColorBrush
			// or null (in which case it will fade in from transparent). The old brush can't be a non-null brush of some other type.
			// TODO: If we want to allow null new brushes (fade to transparent), that's some unloading storage level of work.
			newBrush is not null
			&& oldBrushIsNullOrSolidColorBrush
			&& oldBrush != newBrush
			// SolidColorBrush animations work only on static brushes. Neither brush can be animating.
			//&& oldBrushIsNullOrStatic
			//&& !newBrush->IsEffectiveValueInSparseStorage(KnownPropertyIndex::SolidColorBrush_ColorAnimation)
			)
		{
			visual.Compositor.RegisterBackgroundTransition(visual, oldBrush?.Color ?? Colors.Transparent, newBrush.Color, transition.Duration);
		}
	}
}
#endif
