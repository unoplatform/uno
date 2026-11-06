using System.Runtime.CompilerServices;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;
using Windows.System;

namespace DirectUI;

internal static class CoreImports
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static VirtualKeyModifiers Input_GetKeyboardModifiers() => PlatformHelpers.GetKeyboardModifiers();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static short LayoutManager_GetLayoutTickForTransition(Microsoft.UI.Xaml.UIElement target) => Transition.GetLayoutCounter();

	// MUX Reference CoreImports.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d
	internal static void UIElement_SetCurrentTransitionLocation(Microsoft.UI.Xaml.UIElement pTarget, float left, float top, float width, float height)
	{
		if (pTarget.m_pLayoutTransitionStorage is { } pStorage)
		{
			pStorage.m_currentOffset = pStorage.m_nextGenerationOffset = new Point(left, top);
			pStorage.m_currentSize = pStorage.m_nextGenerationSize = new Size(width, height);
			pStorage.m_nextGenerationCounter = Transition.GetLayoutCounter() + 1;
		}
	}

	internal static void UIElement_SetIsEntering(Microsoft.UI.Xaml.UIElement pTarget, bool fValue)
	{
		if (pTarget.m_pLayoutTransitionStorage is { } pStorage)
		{
			pStorage.m_enteredTreeCounter = fValue ? Transition.EnteredInThisTick : Transition.GetLayoutCounter() - 1;
		}
	}

	internal static void UIElement_SetIsLeaving(Microsoft.UI.Xaml.UIElement pTarget, bool fValue)
	{
		if (pTarget.m_pLayoutTransitionStorage is { } pStorage)
		{
			pStorage.m_leftTreeCounter = fValue ? Transition.LeftInThisTick : Transition.GetLayoutCounter() - 1;
		}
	}

	// Also takes the payload WinUI stores in ItemContainerGenerator.DeferredUnlinkingPayload.
	internal static void UIElement_SetDeferUnlinkContainer(Microsoft.UI.Xaml.UIElement pTarget, DeferredUnlinkActionPayload pPayload)
		=> pTarget.m_pLayoutTransitionStorage!.m_deferredUnlinkingPayload = pPayload;

	internal static void UIElement_GetHasTransition(Microsoft.UI.Xaml.UIElement pTarget, out bool pHasTransition, out TransitionTrigger pTransitionTrigger)
	{
		pTransitionTrigger = pTarget.m_pLayoutTransitionStorage?.GetTrigger() ?? TransitionTrigger.NoTrigger;
		pHasTransition = pTransitionTrigger != TransitionTrigger.NoTrigger;
	}

	internal static void UIElement_CancelTransition(Microsoft.UI.Xaml.UIElement pTarget) => Transition.CancelTransitions(pTarget);
}
