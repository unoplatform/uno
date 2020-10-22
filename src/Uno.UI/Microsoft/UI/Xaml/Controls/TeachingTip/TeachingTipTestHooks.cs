// MUX Reference TeachingTip.properties.cpp, commit 956f1ec

#nullable enable

using System;
using Microsoft.UI.Xaml.Controls;
using Uno;
using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Private.Controls
{
	internal class TeachingTipTestHooks
	{
		private static TeachingTipTestHooks s_testHooks = new TeachingTipTestHooks();

		private static void EnsureGlobalTestHooks()
		{
			// Not needed in Uno, we are using static singleton.
		}

		internal void SetExpandEasingFunction(TeachingTip teachingTip, CompositionEasingFunction easingFunction)
		{
			if (teachingTip != null && easingFunction != null)
			{
				teachingTip.SetExpandEasingFunction(easingFunction);
			}
		}

		internal void SetContractEasingFunction(TeachingTip teachingTip, CompositionEasingFunction easingFunction)
		{
			if (teachingTip != null && easingFunction != null)
			{
				teachingTip.SetContractEasingFunction(easingFunction);
			}
		}

		internal void SetTipShouldHaveShadow(TeachingTip teachingTip, bool tipShouldHaveShadow)
		{
			if (teachingTip != null)
			{
				teachingTip.SetTipShouldHaveShadow(tipShouldHaveShadow);
			}
		}

		internal void SetContentElevation(TeachingTip teachingTip, float elevation)
		{
			if (teachingTip != null)
			{
				teachingTip.SetContentElevation(elevation);
			}
		}

		internal void SetTailElevation(TeachingTip teachingTip, float elevation)
		{
			if (teachingTip != null)
			{
				teachingTip.SetTailElevation(elevation);
			}
		}

		internal void SetUseTestWindowBounds(TeachingTip teachingTip, bool useTestWindowBounds)
		{
			if (teachingTip != null)
			{
				teachingTip.SetUseTestWindowBounds(useTestWindowBounds);
			}
		}

		void SetTestWindowBounds(TeachingTip teachingTip, Rect testWindowBounds)
		{
			if (teachingTip != null)
			{
				teachingTip.SetTestWindowBounds(testWindowBounds);
			}
		}

		void SetUseTestScreenBounds(TeachingTip teachingTip, bool useTestScreenBounds)
		{
			if (teachingTip != null)
			{
				teachingTip.SetUseTestScreenBounds(useTestScreenBounds);
			}
		}

		void SetTestScreenBounds(TeachingTip teachingTip, Rect testScreenBounds)
		{
			if (teachingTip != null)
			{
				teachingTip.SetTestScreenBounds(testScreenBounds);
			}
		}

		void SetTipFollowsTarget(TeachingTip teachingTip, bool tipFollowsTarget)
		{
			if (teachingTip != null)
			{
				teachingTip.SetTipFollowsTarget(tipFollowsTarget);
			}
		}

		void SetReturnTopForOutOfWindowPlacement(TeachingTip teachingTip, bool returnTopForOutOfWindowPlacement)
		{
			if (teachingTip != null)
			{
				teachingTip.SetReturnTopForOutOfWindowPlacement(returnTopForOutOfWindowPlacement);
			}
		}

		void SetExpandAnimationDuration(TeachingTip teachingTip, TimeSpan expandAnimationDuration)
		{
			if (teachingTip != null)
			{
				teachingTip.SetExpandAnimationDuration(expandAnimationDuration);
			}
		}

		void SetContractAnimationDuration(TeachingTip teachingTip, TimeSpan contractAnimationDuration)
		{
			if (teachingTip != null)
			{
				teachingTip.SetContractAnimationDuration(contractAnimationDuration);
			}
		}

		internal static void NotifyOpenedStatusChanged(TeachingTip sender)
		{
			s_testHooks.OpenedStatusChanged?.Invoke(sender, null);
		}

		internal event EventHandler? OpenedStatusChanged;

		internal static void NotifyIdleStatusChanged(TeachingTip sender)
		{
			s_testHooks.IdleStatusChanged?.Invoke(sender, null);
		}

		internal event EventHandler? IdleStatusChanged;

		internal bool GetIsIdle(TeachingTip teachingTip)
		{
			if (teachingTip != null)
			{
				return teachingTip.GetIsIdle();
			}
			return true;
		}

		internal static void NotifyEffectivePlacementChanged(TeachingTip sender)
		{
			s_testHooks.EffectivePlacementChanged?.Invoke(sender, null);
		}

		internal TypedEventHandler<TeachingTip, object> EffectivePlacementChanged;

		internal TeachingTipPlacementMode GetEffectivePlacement(TeachingTip teachingTip)
		{
			if (teachingTip != null)
			{
				return teachingTip.GetEffectivePlacement();
			}
			return TeachingTipPlacementMode.Auto;
		}

		internal static void NotifyEffectiveHeroContentPlacementChanged(TeachingTip sender)
		{
			s_testHooks.EffectiveHeroContentPlacementChanged?.Invoke(sender, null);
		}

		internal TypedEventHandler<TeachingTip, object> EffectiveHeroContentPlacementChanged;

		internal TeachingTipHeroContentPlacementMode GetEffectiveHeroContentPlacement(TeachingTip teachingTip)
		{
			if (teachingTip != null)
			{
				return teachingTip.GetEffectiveHeroContentPlacement();
			}
			return TeachingTipHeroContentPlacementMode.Auto;
		}

		internal TypedEventHandler<TeachingTip, object> OffsetChanged;

		internal TypedEventHandler<TeachingTip, object> TitleVisibilityChanged;

		internal TypedEventHandler<TeachingTip, object> SubtitleVisibilityChanged;

		internal double GetVerticalOffset(TeachingTip teachingTip)
		{
			if (teachingTip != null)
			{
				return teachingTip.GetVerticalOffset();
			}
			return 0.0;
		}

		internal double GetHorizontalOffset(TeachingTip teachingTip)
		{
			if (teachingTip != null)
			{
				return teachingTip.GetHorizontalOffset();
			}
			return 0.0;
		}

		internal Visibility GetTitleVisibility(TeachingTip teachingTip)
		{
			if (teachingTip != null)
			{
				return teachingTip.GetTitleVisibility();
			}
			return Visibility.Collapsed;
		}

		Visibility GetSubtitleVisibility(TeachingTip teachingTip)
		{
			if (teachingTip != null)
			{
				return teachingTip.GetSubtitleVisibility();
			}
			return Visibility.Collapsed;
		}

		internal Popup GetPopup(TeachingTip teachingTip)
		{
			if (teachingTip != null)
			{
				return teachingTip.m_popup;
			}
			return null;
		}
	}
}
