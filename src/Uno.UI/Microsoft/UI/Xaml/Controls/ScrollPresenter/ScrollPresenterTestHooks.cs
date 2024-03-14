// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Numerics;
using Microsoft.UI.Private.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;
using Windows.UI;

namespace Microsoft.UI.Private.Controls;

internal partial class ScrollPresenterTestHooks
{
	private static ScrollPresenterTestHooks s_testHooks;

	public ScrollPresenterTestHooks()
	{
	}

	private static ScrollPresenterTestHooks EnsureGlobalTestHooks()
	{
		return s_testHooks ??= new ScrollPresenterTestHooks();
	}

	internal static bool AreAnchorNotificationsRaised
	{
		get
		{
			var hooks = EnsureGlobalTestHooks();
			return hooks.m_areAnchorNotificationsRaised;
		}
		set
		{
			var hooks = EnsureGlobalTestHooks();
			hooks.m_areAnchorNotificationsRaised = value;

		}
	}

	internal static bool AreInteractionSourcesNotificationsRaised
	{
		get
		{
			var hooks = EnsureGlobalTestHooks();
			return hooks.m_areInteractionSourcesNotificationsRaised;
		}
		set
		{
			var hooks = EnsureGlobalTestHooks();
			hooks.m_areInteractionSourcesNotificationsRaised = value;
		}
	}

	internal static bool AreExpressionAnimationStatusNotificationsRaised
	{
		get
		{
			var hooks = EnsureGlobalTestHooks();
			return hooks.m_areExpressionAnimationStatusNotificationsRaised;
		}
		set
		{
			var hooks = EnsureGlobalTestHooks();
			hooks.m_areExpressionAnimationStatusNotificationsRaised = value;
		}
	}

	internal static bool? IsAnimationsEnabledOverride
	{
		get
		{
			var hooks = EnsureGlobalTestHooks();
			return hooks.m_isAnimationsEnabledOverride;
		}
		set
		{
			var hooks = EnsureGlobalTestHooks();
			hooks.m_isAnimationsEnabledOverride = value;
		}
	}

	internal static void GetOffsetsChangeVelocityParameters(out int millisecondsPerUnit, out int minMilliseconds, out int maxMilliseconds)
	{
		var hooks = EnsureGlobalTestHooks();
		millisecondsPerUnit = hooks.m_offsetsChangeMsPerUnit;
		minMilliseconds = hooks.m_offsetsChangeMinMs;
		maxMilliseconds = hooks.m_offsetsChangeMaxMs;
	}

	internal static void SetOffsetsChangeVelocityParameters(int millisecondsPerUnit, int minMilliseconds, int maxMilliseconds)
	{
		var hooks = EnsureGlobalTestHooks();
		hooks.m_offsetsChangeMsPerUnit = millisecondsPerUnit;
		hooks.m_offsetsChangeMinMs = minMilliseconds;
		hooks.m_offsetsChangeMaxMs = maxMilliseconds;
	}

	internal static void GetZoomFactorChangeVelocityParameters(out int millisecondsPerUnit, out int minMilliseconds, out int maxMilliseconds)
	{
		var hooks = EnsureGlobalTestHooks();
		millisecondsPerUnit = hooks.m_zoomFactorChangeMsPerUnit;
		minMilliseconds = hooks.m_zoomFactorChangeMinMs;
		maxMilliseconds = hooks.m_zoomFactorChangeMaxMs;
	}

	internal static void SetZoomFactorChangeVelocityParameters(int millisecondsPerUnit, int minMilliseconds, int maxMilliseconds)
	{
		var hooks = EnsureGlobalTestHooks();
		hooks.m_zoomFactorChangeMsPerUnit = millisecondsPerUnit;
		hooks.m_zoomFactorChangeMinMs = minMilliseconds;
		hooks.m_zoomFactorChangeMaxMs = maxMilliseconds;
	}

	internal static void GetContentLayoutOffsetX(ScrollPresenter scrollPresenter, out float contentLayoutOffsetX)
	{
		if (scrollPresenter is not null)
		{
			contentLayoutOffsetX = scrollPresenter.GetContentLayoutOffsetX();
		}
		else
		{
			contentLayoutOffsetX = 0.0f;
		}
	}

	internal static void SetContentLayoutOffsetX(ScrollPresenter scrollPresenter, float contentLayoutOffsetX)
	{
		if (scrollPresenter is not null)
		{
			scrollPresenter.SetContentLayoutOffsetX(contentLayoutOffsetX);
		}
	}

	internal static void GetContentLayoutOffsetY(ScrollPresenter scrollPresenter, out float contentLayoutOffsetY)
	{
		if (scrollPresenter is not null)
		{
			contentLayoutOffsetY = scrollPresenter.GetContentLayoutOffsetY();
		}
		else
		{
			contentLayoutOffsetY = 0.0f;
		}
	}

	internal static void SetContentLayoutOffsetY(ScrollPresenter scrollPresenter, float contentLayoutOffsetY)
	{
		if (scrollPresenter is not null)
		{
			scrollPresenter.SetContentLayoutOffsetY(contentLayoutOffsetY);
		}
	}

	internal static Vector2 GetArrangeRenderSizesDelta(ScrollPresenter scrollPresenter)
	{
		if (scrollPresenter is not null)
		{
			return scrollPresenter.GetArrangeRenderSizesDelta();
		}
		return new Vector2(0.0f, 0.0f);
	}

	internal static Vector2 GetMinPosition(ScrollPresenter scrollPresenter)
	{
		if (scrollPresenter is not null)
		{
			return scrollPresenter.GetMinPosition();
		}
		return new Vector2(0.0f, 0.0f);
	}

	internal static Vector2 GetMaxPosition(ScrollPresenter scrollPresenter)
	{
		if (scrollPresenter is not null)
		{
			return scrollPresenter.GetMaxPosition();
		}
		return new Vector2(0.0f, 0.0f);
	}

	internal static ScrollPresenterViewChangeResult GetScrollCompletedResult(ScrollingScrollCompletedEventArgs scrollCompletedEventArgs)
	{
		if (scrollCompletedEventArgs is not null)
		{
			ScrollPresenterViewChangeResult result = scrollCompletedEventArgs.Result;
			return TestHooksViewChangeResult(result);
		}
		return ScrollPresenterViewChangeResult.Completed;
	}

	internal static ScrollPresenterViewChangeResult GetZoomCompletedResult(ScrollingZoomCompletedEventArgs zoomCompletedEventArgs)
	{
		if (zoomCompletedEventArgs is not null)
		{
			ScrollPresenterViewChangeResult result = zoomCompletedEventArgs.Result;
			return TestHooksViewChangeResult(result);
		}
		return ScrollPresenterViewChangeResult.Completed;
	}

	internal void NotifyAnchorEvaluated(
		ScrollPresenter sender,
		UIElement anchorElement,
		double viewportAnchorPointHorizontalOffset,
		double viewportAnchorPointVerticalOffset)
	{
		var hooks = EnsureGlobalTestHooks();
		if (AnchorEvaluated is not null)
		{
			var anchorEvaluatedEventArgs = new ScrollPresenterTestHooksAnchorEvaluatedEventArgs(
				anchorElement, viewportAnchorPointHorizontalOffset, viewportAnchorPointVerticalOffset);

			AnchorEvaluated.Invoke(sender, anchorEvaluatedEventArgs);
		}
	}

	public static event TypedEventHandler<ScrollPresenter, ScrollPresenterTestHooksAnchorEvaluatedEventArgs> AnchorEvaluated;

	internal void NotifyInteractionSourcesChanged(
			ScrollPresenter sender,
			Microsoft.UI.Composition.Interactions.CompositionInteractionSourceCollection interactionSources)
	{
		var hooks = EnsureGlobalTestHooks();
		if (InteractionSourcesChanged is not null)
		{
			var interactionSourcesChangedEventArgs = new ScrollPresenterTestHooksInteractionSourcesChangedEventArgs(
				interactionSources);

			InteractionSourcesChanged.Invoke(sender, interactionSourcesChangedEventArgs);
		}
	}

	public static event TypedEventHandler<ScrollPresenter, ScrollPresenterTestHooksInteractionSourcesChangedEventArgs> InteractionSourcesChanged;

	internal void NotifyExpressionAnimationStatusChanged(
		ScrollPresenter sender,
		bool isExpressionAnimationStarted,
		string propertyName)
	{
		var hooks = EnsureGlobalTestHooks();
		if (ExpressionAnimationStatusChanged is not null)
		{
			var expressionAnimationStatusChangedEventArgs = new ScrollPresenterTestHooksExpressionAnimationStatusChangedEventArgs(
				isExpressionAnimationStarted, propertyName);

			ExpressionAnimationStatusChanged.Invoke(sender, expressionAnimationStatusChangedEventArgs);
		}
	}

	public static event TypedEventHandler<ScrollPresenter, ScrollPresenterTestHooksExpressionAnimationStatusChangedEventArgs> ExpressionAnimationStatusChanged;

	internal void NotifyContentLayoutOffsetXChanged(ScrollPresenter sender)
	{
		var hooks = EnsureGlobalTestHooks();
		if (hooks.ContentLayoutOffsetXChanged is not null)
		{
			hooks.ContentLayoutOffsetXChanged.Invoke(sender, null);
		}
	}

	public event TypedEventHandler<ScrollPresenter, object> ContentLayoutOffsetXChanged;

	internal void NotifyContentLayoutOffsetYChanged(ScrollPresenter sender)
	{
		var hooks = EnsureGlobalTestHooks();
		if (hooks.ContentLayoutOffsetYChanged is not null)
		{
			hooks.ContentLayoutOffsetYChanged.Invoke(sender, null);
		}
	}

	public event TypedEventHandler<ScrollPresenter, object> ContentLayoutOffsetYChanged;

	internal static IList<ScrollSnapPointBase> GetConsolidatedHorizontalScrollSnapPoints(ScrollPresenter scrollPresenter)
	{
		if (scrollPresenter is not null)
		{
			return scrollPresenter.GetConsolidatedHorizontalScrollSnapPoints();
		}
		else
		{
			return new List<ScrollSnapPointBase>();
		}
	}

	internal static IList<ScrollSnapPointBase> GetConsolidatedVerticalScrollSnapPoints(ScrollPresenter scrollPresenter)
	{
		if (scrollPresenter is not null)
		{
			return scrollPresenter.GetConsolidatedVerticalScrollSnapPoints();
		}
		else
		{
			return new List<ScrollSnapPointBase>();
		}
	}

	internal static IList<ZoomSnapPointBase> GetConsolidatedZoomSnapPoints(ScrollPresenter scrollPresenter)
	{
		if (scrollPresenter is not null)
		{
			return scrollPresenter.GetConsolidatedZoomSnapPoints();
		}
		else
		{
			return new List<ZoomSnapPointBase>();
		}
	}

	internal static Vector2 GetHorizontalSnapPointActualApplicableZone(
		ScrollPresenter scrollPresenter,
		ScrollSnapPointBase scrollSnapPoint)
	{
		if (scrollSnapPoint is not null)
		{
			var snapPointWrapper = scrollPresenter.GetHorizontalSnapPointWrapper(scrollSnapPoint);
			var zone = snapPointWrapper.ActualApplicableZone();

			return new Vector2((float)zone.Item1, (float)zone.Item2);
		}
		else
		{
			return new Vector2(0.0f, 0.0f);
		}
	}

	internal static Vector2 GetVerticalSnapPointActualApplicableZone(
		ScrollPresenter scrollPresenter,
		ScrollSnapPointBase scrollSnapPoint)
	{
		if (scrollSnapPoint is not null)
		{
			var snapPointWrapper = scrollPresenter.GetVerticalSnapPointWrapper(scrollSnapPoint);
			var zone = snapPointWrapper.ActualApplicableZone();

			return new Vector2((float)zone.Item1, (float)zone.Item2);
		}
		else
		{
			return new Vector2(0.0f, 0.0f);
		}
	}

	internal static Vector2 GetZoomSnapPointActualApplicableZone(
		ScrollPresenter scrollPresenter,
		ZoomSnapPointBase zoomSnapPoint)
	{
		if (zoomSnapPoint is not null)
		{
			var snapPointWrapper = scrollPresenter.GetZoomSnapPointWrapper(zoomSnapPoint);
			var zone = snapPointWrapper.ActualApplicableZone();

			return new Vector2((float)zone.Item1, (float)zone.Item2);
		}
		else
		{
			return new Vector2(0.0f, 0.0f);
		}
	}

	internal static int GetHorizontalSnapPointCombinationCount(
		ScrollPresenter scrollPresenter,
		ScrollSnapPointBase scrollSnapPoint)
	{
		if (scrollSnapPoint is not null)
		{
			var snapPointWrapper = scrollPresenter.GetHorizontalSnapPointWrapper(scrollSnapPoint);

			return snapPointWrapper.CombinationCount();
		}
		else
		{
			return 0;
		}
	}

	internal static int GetVerticalSnapPointCombinationCount(
		ScrollPresenter scrollPresenter,
		ScrollSnapPointBase scrollSnapPoint)
	{
		if (scrollSnapPoint is not null)
		{
			var snapPointWrapper = scrollPresenter.GetVerticalSnapPointWrapper(scrollSnapPoint);

			return snapPointWrapper.CombinationCount();
		}
		else
		{
			return 0;
		}
	}

	internal static int GetZoomSnapPointCombinationCount(
			ScrollPresenter scrollPresenter,
			ZoomSnapPointBase zoomSnapPoint)
	{
		if (zoomSnapPoint is not null)
		{
			var snapPointWrapper = scrollPresenter.GetZoomSnapPointWrapper(zoomSnapPoint);

			return snapPointWrapper.CombinationCount();
		}
		else
		{
			return 0;
		}
	}

	internal static Color GetSnapPointVisualizationColor(SnapPointBase snapPoint)
	{

#if DEBUG
		if (snapPoint is not null)
		{
			return snapPoint.VisualizationColor;
		}
#endif // DBG
		return Colors.Black;
	}

	internal static void SetSnapPointVisualizationColor(SnapPointBase snapPoint, Color color)
	{
#if DEBUG
		if (snapPoint is not null)
		{
			snapPoint.VisualizationColor = color;
		}
#endif // DBG
	}

	private static ScrollPresenterViewChangeResult TestHooksViewChangeResult(ScrollPresenterViewChangeResult result)
	{
		switch (result)
		{
			case ScrollPresenterViewChangeResult.Ignored:
				return ScrollPresenterViewChangeResult.Ignored;
			case ScrollPresenterViewChangeResult.Interrupted:
				return ScrollPresenterViewChangeResult.Interrupted;
			default:
				return ScrollPresenterViewChangeResult.Completed;
		}
	}
}
