namespace Microsoft.UI.Private.Controls
{
	internal class TeachingTipTestHooks
    {
//		com_ptr<TeachingTipTestHooks> EnsureGlobalTestHooks()
//		{
//			static bool s_initialized = []() {
//				s_testHooks = make_self<TeachingTipTestHooks>();
//				return true;
//			} ();
//			return s_testHooks;
//		}

//		void SetExpandEasingFunction(TeachingTip& teachingTip, CompositionEasingFunction& easingFunction)
//		{
//			if (teachingTip && easingFunction)
//			{
//				get_self<TeachingTip>(teachingTip).SetExpandEasingFunction(easingFunction);
//			}
//		}

//		void SetContractEasingFunction(TeachingTip& teachingTip, CompositionEasingFunction& easingFunction)
//		{
//			if (teachingTip && easingFunction)
//			{
//				get_self<TeachingTip>(teachingTip).SetContractEasingFunction(easingFunction);
//			}
//		}

//		void SetTipShouldHaveShadow(TeachingTip& teachingTip, bool tipShouldHaveShadow)
//		{
//			if (teachingTip)
//			{
//				get_self<TeachingTip>(teachingTip).SetTipShouldHaveShadow(tipShouldHaveShadow);
//			}
//		}

//		void SetContentElevation(TeachingTip& teachingTip, float elevation)
//		{
//			if (teachingTip)
//			{
//				get_self<TeachingTip>(teachingTip).SetContentElevation(elevation);
//			}
//		}

//		void SetTailElevation(TeachingTip& teachingTip, float elevation)
//		{
//			if (teachingTip)
//			{
//				get_self<TeachingTip>(teachingTip).SetTailElevation(elevation);
//			}
//		}

//		void SetUseTestWindowBounds(TeachingTip& teachingTip, bool useTestWindowBounds)
//		{
//			if (teachingTip)
//			{
//				get_self<TeachingTip>(teachingTip).SetUseTestWindowBounds(useTestWindowBounds);
//			}
//		}

//		void SetTestWindowBounds(TeachingTip& teachingTip, Rect& testWindowBounds)
//		{
//			if (teachingTip)
//			{
//				get_self<TeachingTip>(teachingTip).SetTestWindowBounds(testWindowBounds);
//			}
//		}

//		void SetUseTestScreenBounds(TeachingTip& teachingTip, bool useTestScreenBounds)
//		{
//			if (teachingTip)
//			{
//				get_self<TeachingTip>(teachingTip).SetUseTestScreenBounds(useTestScreenBounds);
//			}
//		}

//		void SetTestScreenBounds(TeachingTip& teachingTip, Rect& testScreenBounds)
//		{
//			if (teachingTip)
//			{
//				get_self<TeachingTip>(teachingTip).SetTestScreenBounds(testScreenBounds);
//			}
//		}

//		void SetTipFollowsTarget(TeachingTip& teachingTip, bool tipFollowsTarget)
//		{
//			if (teachingTip)
//			{
//				get_self<TeachingTip>(teachingTip).SetTipFollowsTarget(tipFollowsTarget);
//			}
//		}

//		void SetReturnTopForOutOfWindowPlacement(TeachingTip& teachingTip, bool returnTopForOutOfWindowPlacement)
//		{
//			if (teachingTip)
//			{
//				get_self<TeachingTip>(teachingTip).SetReturnTopForOutOfWindowPlacement(returnTopForOutOfWindowPlacement);
//			}
//		}

//		void SetExpandAnimationDuration(TeachingTip& teachingTip, TimeSpan& expandAnimationDuration)
//		{
//			if (teachingTip)
//			{
//				get_self<TeachingTip>(teachingTip).SetExpandAnimationDuration(expandAnimationDuration);
//			}
//		}

//		void SetContractAnimationDuration(TeachingTip& teachingTip, TimeSpan& contractAnimationDuration)
//		{
//			if (teachingTip)
//			{
//				get_self<TeachingTip>(teachingTip).SetContractAnimationDuration(contractAnimationDuration);
//			}
//		}

//		void NotifyOpenedStatusChanged(TeachingTip& sender)
//		{
//			var hooks = EnsureGlobalTestHooks();
//			if (hooks.m_openedStatusChangedEventSource)
//			{
//				hooks.m_openedStatusChangedEventSource(sender, null);
//			}
//		}

//		event_token OpenedStatusChanged(TypedEventHandler<TeachingTip, object> & value)
//		{
//			var hooks = EnsureGlobalTestHooks();
//			return hooks.m_openedStatusChangedEventSource.add(value);
//		}

//		void OpenedStatusChanged(event_token & token)
//		{
//			var hooks = EnsureGlobalTestHooks();
//			hooks.m_openedStatusChangedEventSource.remove(token);
//		}

//		void NotifyIdleStatusChanged(TeachingTip& sender)
//		{
//			var hooks = EnsureGlobalTestHooks();
//			if (hooks.m_idleStatusChangedEventSource)
//			{
//				hooks.m_idleStatusChangedEventSource(sender, null);
//			}
//		}

//		event_token IdleStatusChanged(TypedEventHandler<TeachingTip, object> & value)
//		{
//			var hooks = EnsureGlobalTestHooks();
//			return hooks.m_idleStatusChangedEventSource.add(value);
//		}

//		void IdleStatusChanged(event_token & token)
//		{
//			var hooks = EnsureGlobalTestHooks();
//			hooks.m_idleStatusChangedEventSource.remove(token);
//		}


//		bool GetIsIdle(TeachingTip& teachingTip)
//		{
//			if (teachingTip)
//			{
//				return get_self<TeachingTip>(teachingTip).GetIsIdle();
//			}
//			return true;
//		}

//		void NotifyEffectivePlacementChanged(TeachingTip& sender)
//		{
//			var hooks = EnsureGlobalTestHooks();
//			if (hooks.m_effectivePlacementChangedEventSource)
//			{
//				hooks.m_effectivePlacementChangedEventSource(sender, null);
//			}
//		}

//		event_token EffectivePlacementChanged(TypedEventHandler<TeachingTip, object> & value)
//		{
//			var hooks = EnsureGlobalTestHooks();
//			return hooks.m_effectivePlacementChangedEventSource.add(value);
//		}

//		void EffectivePlacementChanged(event_token & token)
//		{
//			var hooks = EnsureGlobalTestHooks();
//			hooks.m_effectivePlacementChangedEventSource.remove(token);
//		}

//		TeachingTipPlacementMode GetEffectivePlacement(TeachingTip& teachingTip)
//		{
//			if (teachingTip)
//			{
//				return get_self<TeachingTip>(teachingTip).GetEffectivePlacement();
//			}
//			return TeachingTipPlacementMode.Auto;
//		}

//		void NotifyEffectiveHeroContentPlacementChanged(TeachingTip& sender)
//		{
//			var hooks = EnsureGlobalTestHooks();
//			if (hooks.m_effectiveHeroContentPlacementChangedEventSource)
//			{
//				hooks.m_effectiveHeroContentPlacementChangedEventSource(sender, null);
//			}
//		}

//		event_token EffectiveHeroContentPlacementChanged(TypedEventHandler<TeachingTip, object> & value)
//		{
//			var hooks = EnsureGlobalTestHooks();
//			return hooks.m_effectiveHeroContentPlacementChangedEventSource.add(value);
//		}

//		void EffectiveHeroContentPlacementChanged(event_token & token)
//		{
//			var hooks = EnsureGlobalTestHooks();
//			hooks.m_effectiveHeroContentPlacementChangedEventSource.remove(token);
//		}

//		TeachingTipHeroContentPlacementMode GetEffectiveHeroContentPlacement(TeachingTip& teachingTip)
//		{
//			if (teachingTip)
//			{
//				return get_self<TeachingTip>(teachingTip).GetEffectiveHeroContentPlacement();
//			}
//			return TeachingTipHeroContentPlacementMode.Auto;
//		}

//		void NotifyOffsetChanged(TeachingTip& sender)
//		{
//			var hooks = EnsureGlobalTestHooks();
//			if (hooks.m_offsetChangedEventSource)
//			{
//				hooks.m_offsetChangedEventSource(sender, null);
//			}
//		}

//		event_token OffsetChanged(TypedEventHandler<TeachingTip, object> & value)
//		{
//			var hooks = EnsureGlobalTestHooks();
//			return hooks.m_offsetChangedEventSource.add(value);
//		}

//		void OffsetChanged(event_token & token)
//		{
//			var hooks = EnsureGlobalTestHooks();
//			hooks.m_offsetChangedEventSource.remove(token);
//		}

//		void NotifyTitleVisibilityChanged(TeachingTip& sender)
//		{
//			var hooks = EnsureGlobalTestHooks();
//			if (hooks.m_titleVisibilityChangedEventSource)
//			{
//				hooks.m_titleVisibilityChangedEventSource(sender, null);
//			}
//		}

//		event_token TitleVisibilityChanged(TypedEventHandler<TeachingTip, object> & value)
//		{
//			var hooks = EnsureGlobalTestHooks();
//			return hooks.m_titleVisibilityChangedEventSource.add(value);
//		}

//		void TitleVisibilityChanged(event_token & token)
//		{
//			var hooks = EnsureGlobalTestHooks();
//			hooks.m_titleVisibilityChangedEventSource.remove(token);
//		}

//		void NotifySubtitleVisibilityChanged(TeachingTip& sender)
//		{
//			var hooks = EnsureGlobalTestHooks();
//			if (hooks.m_subtitleVisibilityChangedEventSource)
//			{
//				hooks.m_subtitleVisibilityChangedEventSource(sender, null);
//			}
//		}

//		event_token SubtitleVisibilityChanged(TypedEventHandler<TeachingTip, object> & value)
//		{
//			var hooks = EnsureGlobalTestHooks();
//			return hooks.m_subtitleVisibilityChangedEventSource.add(value);
//		}

//		void SubtitleVisibilityChanged(event_token & token)
//		{
//			var hooks = EnsureGlobalTestHooks();
//			hooks.m_subtitleVisibilityChangedEventSource.remove(token);
//		}

//		double GetVerticalOf = TeachingTip & teachingTip
//{
//    if (teachingTip)
//    {
//        return get_self<TeachingTip>(teachingTip).GetVerticalOf = ;
//    }
//    return 0.0;
//}

//double GetHorizontalOf = TeachingTip & teachingTip
//{
//    if (teachingTip)
//    {
//	return get_self<TeachingTip>(teachingTip).GetHorizontalOf = ;
//}
//return 0.0;
//}

//Visibility GetTitleVisibility(TeachingTip& teachingTip)
//{
//	if (teachingTip)
//	{
//		return get_self<TeachingTip>(teachingTip).GetTitleVisibility();
//	}
//	return Visibility.Collapsed;
//}

//Visibility GetSubtitleVisibility(TeachingTip& teachingTip)
//{
//	if (teachingTip)
//	{
//		return get_self<TeachingTip>(teachingTip).GetSubtitleVisibility();
//	}
//	return Visibility.Collapsed;
//}

//Popup GetPopup(TeachingTip& teachingTip)
//{
//	if (teachingTip)
//	{
//		return get_self<TeachingTip>(teachingTip).m_popup;
//	}
//	return null;
//}

    }
}
