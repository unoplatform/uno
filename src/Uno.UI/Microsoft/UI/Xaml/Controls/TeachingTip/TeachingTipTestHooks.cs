// MUX Reference TeachingTip.properties.cpp, commit 956f1ec

#nullable enable

using System;
using Uno;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

#pragma warning disable IDE0051 // Some of the methods in this file are defined in the WinUI source but are not used.

namespace Microsoft.UI.Private.Controls;

internal class TeachingTipTestHooks
{
	private static TeachingTipTestHooks s_testHooks = new TeachingTipTestHooks();

	private static void EnsureGlobalTestHooks()
	{
		// Not needed in Uno, we are using static singleton.
	}

	internal static void SetExpandEasingFunction(TeachingTip teachingTip, CompositionEasingFunction easingFunction)
	{
		if (teachingTip != null && easingFunction != null)
		{
			teachingTip.SetExpandEasingFunction(easingFunction);
		}
	}

	internal static void SetContractEasingFunction(TeachingTip teachingTip, CompositionEasingFunction easingFunction)
	{
		if (teachingTip != null && easingFunction != null)
		{
			teachingTip.SetContractEasingFunction(easingFunction);
		}
	}

	internal static void SetTipShouldHaveShadow(TeachingTip teachingTip, bool tipShouldHaveShadow)
	{
		if (teachingTip != null)
		{
			teachingTip.SetTipShouldHaveShadow(tipShouldHaveShadow);
		}
	}

	internal static void SetContentElevation(TeachingTip teachingTip, float elevation)
	{
		if (teachingTip != null)
		{
			teachingTip.SetContentElevation(elevation);
		}
	}

	internal static void SetTailElevation(TeachingTip teachingTip, float elevation)
	{
		if (teachingTip != null)
		{
			teachingTip.SetTailElevation(elevation);
		}
	}

	internal static void SetUseTestWindowBounds(TeachingTip teachingTip, bool useTestWindowBounds)
	{
		if (teachingTip != null)
		{
			teachingTip.SetUseTestWindowBounds(useTestWindowBounds);
		}
	}

	internal static void SetTestWindowBounds(TeachingTip teachingTip, Rect testWindowBounds)
	{
		if (teachingTip != null)
		{
			teachingTip.SetTestWindowBounds(testWindowBounds);
		}
	}

	internal static void SetUseTestScreenBounds(TeachingTip teachingTip, bool useTestScreenBounds)
	{
		if (teachingTip != null)
		{
			teachingTip.SetUseTestScreenBounds(useTestScreenBounds);
		}
	}

	internal static void SetTestScreenBounds(TeachingTip teachingTip, Rect testScreenBounds)
	{
		if (teachingTip != null)
		{
			teachingTip.SetTestScreenBounds(testScreenBounds);
		}
	}

	internal static void SetTipFollowsTarget(TeachingTip teachingTip, bool tipFollowsTarget)
	{
		if (teachingTip != null)
		{
			teachingTip.SetTipFollowsTarget(tipFollowsTarget);
		}
	}

	internal static void SetReturnTopForOutOfWindowPlacement(TeachingTip teachingTip, bool returnTopForOutOfWindowPlacement)
	{
		if (teachingTip != null)
		{
			teachingTip.SetReturnTopForOutOfWindowPlacement(returnTopForOutOfWindowPlacement);
		}
	}

	internal static void SetExpandAnimationDuration(TeachingTip teachingTip, TimeSpan expandAnimationDuration)
	{
		if (teachingTip != null)
		{
			teachingTip.SetExpandAnimationDuration(expandAnimationDuration);
		}
	}

	internal static void SetContractAnimationDuration(TeachingTip teachingTip, TimeSpan contractAnimationDuration)
	{
		if (teachingTip != null)
		{
			teachingTip.SetContractAnimationDuration(contractAnimationDuration);
		}
	}

	internal static void NotifyOpenedStatusChanged(TeachingTip sender)
	{
		OpenedStatusChanged?.Invoke(sender, null);
	}

	internal static event TypedEventHandler<TeachingTip, object?>? OpenedStatusChanged;

	internal static void NotifyIdleStatusChanged(TeachingTip sender)
	{
		IdleStatusChanged?.Invoke(sender, null);
	}

	internal static event TypedEventHandler<TeachingTip, object?>? IdleStatusChanged;

	internal static bool GetIsIdle(TeachingTip teachingTip)
	{
		if (teachingTip != null)
		{
			return teachingTip.GetIsIdle();
		}
		return true;
	}

	internal static void NotifyEffectivePlacementChanged(TeachingTip sender)
	{
		EffectivePlacementChanged?.Invoke(sender, null);
	}

	internal static event TypedEventHandler<TeachingTip, object?>? EffectivePlacementChanged;

	internal static TeachingTipPlacementMode GetEffectivePlacement(TeachingTip teachingTip)
	{
		if (teachingTip != null)
		{
			return teachingTip.GetEffectivePlacement();
		}
		return TeachingTipPlacementMode.Auto;
	}

	internal static void NotifyEffectiveHeroContentPlacementChanged(TeachingTip sender)
	{
		EffectiveHeroContentPlacementChanged?.Invoke(sender, null);
	}

	internal static TypedEventHandler<TeachingTip, object?>? EffectiveHeroContentPlacementChanged;

	internal static TeachingTipHeroContentPlacementMode GetEffectiveHeroContentPlacement(TeachingTip teachingTip)
	{
		if (teachingTip != null)
		{
			return teachingTip.GetEffectiveHeroContentPlacement();
		}
		return TeachingTipHeroContentPlacementMode.Auto;
	}

	internal static void NotifyOffsetChanged(TeachingTip sender)
	{
		OffsetChanged?.Invoke(sender, null);
	}

	internal static TypedEventHandler<TeachingTip, object?>? OffsetChanged;

	internal static void NotifyTitleVisibilityChanged(TeachingTip sender)
	{
		TitleVisibilityChanged?.Invoke(sender, null);
	}

	internal static TypedEventHandler<TeachingTip, object?>? TitleVisibilityChanged;

	internal static void NotifySubtitleVisibilityChanged(TeachingTip sender)
	{
		SubtitleVisibilityChanged?.Invoke(sender, null);
	}

	internal static TypedEventHandler<TeachingTip, object?>? SubtitleVisibilityChanged;

	internal static double GetVerticalOffset(TeachingTip teachingTip)
	{
		if (teachingTip != null)
		{
			return teachingTip.GetVerticalOffset();
		}
		return 0.0;
	}

	internal static double GetHorizontalOffset(TeachingTip teachingTip)
	{
		if (teachingTip != null)
		{
			return teachingTip.GetHorizontalOffset();
		}
		return 0.0;
	}

	internal static Visibility GetTitleVisibility(TeachingTip teachingTip)
	{
		if (teachingTip != null)
		{
			return teachingTip.GetTitleVisibility();
		}
		return Visibility.Collapsed;
	}

	internal static Visibility GetSubtitleVisibility(TeachingTip teachingTip)
	{
		if (teachingTip != null)
		{
			return teachingTip.GetSubtitleVisibility();
		}
		return Visibility.Collapsed;
	}

	internal static Popup? GetPopup(TeachingTip teachingTip)
	{
		if (teachingTip != null)
		{
			return teachingTip.m_popup;
		}
		return null;
	}
}
