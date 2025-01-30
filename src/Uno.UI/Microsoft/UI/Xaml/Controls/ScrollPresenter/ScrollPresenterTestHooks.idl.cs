namespace Microsoft.UI.Private.Controls;

internal enum ScrollPresenterViewChangeResult
{
	Completed = 0,
	Interrupted = 1,
	Ignored = 2,
}

// internal class ScrollPresenterTestHooksInteractionSourcesChangedEventArgs
// {
//     CompositionInteractionSourceCollection InteractionSources { get; }
// }


// internal class ScrollPresenterTestHooks
// {
//     static Boolean AreAnchorNotificationsRaised { get; set; }
//     static Boolean AreInteractionSourcesNotificationsRaised { get; set; }
//     static Boolean AreExpressionAnimationStatusNotificationsRaised { get; set; }
//     static Windows.Foundation.IReference<Boolean> IsAnimationsEnabledOverride { get; set; }
//     static void GetOffsetsChangeVelocityParameters(out Int32 millisecondsPerUnit, out Int32 minMilliseconds, out Int32 maxMilliseconds);
//     static void SetOffsetsChangeVelocityParameters(Int32 millisecondsPerUnit, Int32 minMilliseconds, Int32 maxMilliseconds);
//     static void GetZoomFactorChangeVelocityParameters(out Int32 millisecondsPerUnit, out Int32 minMilliseconds, out Int32 maxMilliseconds);
//     static void SetZoomFactorChangeVelocityParameters(Int32 millisecondsPerUnit, Int32 minMilliseconds, Int32 maxMilliseconds);
//     static void GetContentLayoutOffsetX(MU_XCP_NAMESPACE.ScrollPresenter scrollPresenter, out Single contentLayoutOffsetX);
//     static void SetContentLayoutOffsetX(MU_XCP_NAMESPACE.ScrollPresenter scrollPresenter, Single contentLayoutOffsetX);
//     static void GetContentLayoutOffsetY(MU_XCP_NAMESPACE.ScrollPresenter scrollPresenter, out Single contentLayoutOffsetY);
//     static void SetContentLayoutOffsetY(MU_XCP_NAMESPACE.ScrollPresenter scrollPresenter, Single contentLayoutOffsetY);
//     static Windows.Foundation.Numerics.Vector2 GetArrangeRenderSizesDelta(MU_XCP_NAMESPACE.ScrollPresenter scrollPresenter);
//     static Windows.Foundation.Numerics.Vector2 GetMinPosition(MU_XCP_NAMESPACE.ScrollPresenter scrollPresenter);
//     static Windows.Foundation.Numerics.Vector2 GetMaxPosition(MU_XCP_NAMESPACE.ScrollPresenter scrollPresenter);
//     static ScrollPresenterViewChangeResult GetScrollCompletedResult(MU_XC_NAMESPACE.ScrollingScrollCompletedEventArgs scrollCompletedEventArgs);
//     static ScrollPresenterViewChangeResult GetZoomCompletedResult(MU_XC_NAMESPACE.ScrollingZoomCompletedEventArgs zoomCompletedEventArgs);
//     static Windows.Foundation.Collections.IVector<MU_XCP_NAMESPACE.ScrollSnapPointBase> GetConsolidatedHorizontalScrollSnapPoints(MU_XCP_NAMESPACE.ScrollPresenter scrollPresenter);
//     static Windows.Foundation.Collections.IVector<MU_XCP_NAMESPACE.ScrollSnapPointBase> GetConsolidatedVerticalScrollSnapPoints(MU_XCP_NAMESPACE.ScrollPresenter scrollPresenter);
//     static Windows.Foundation.Collections.IVector<MU_XCP_NAMESPACE.ZoomSnapPointBase> GetConsolidatedZoomSnapPoints(MU_XCP_NAMESPACE.ScrollPresenter scrollPresenter);
//     static Windows.Foundation.Numerics.Vector2 GetHorizontalSnapPointActualApplicableZone(MU_XCP_NAMESPACE.ScrollPresenter scrollPresenter, MU_XCP_NAMESPACE.ScrollSnapPointBase scrollSnapPoint);
//     static Windows.Foundation.Numerics.Vector2 GetVerticalSnapPointActualApplicableZone(MU_XCP_NAMESPACE.ScrollPresenter scrollPresenter, MU_XCP_NAMESPACE.ScrollSnapPointBase scrollSnapPoint);
//     static Windows.Foundation.Numerics.Vector2 GetZoomSnapPointActualApplicableZone(MU_XCP_NAMESPACE.ScrollPresenter scrollPresenter, MU_XCP_NAMESPACE.ZoomSnapPointBase zoomSnapPoint);
//     static Int32 GetHorizontalSnapPointCombinationCount(MU_XCP_NAMESPACE.ScrollPresenter scrollPresenter, MU_XCP_NAMESPACE.ScrollSnapPointBase scrollSnapPoint);
//     static Int32 GetVerticalSnapPointCombinationCount(MU_XCP_NAMESPACE.ScrollPresenter scrollPresenter, MU_XCP_NAMESPACE.ScrollSnapPointBase scrollSnapPoint);
//     static Int32 GetZoomSnapPointCombinationCount(MU_XCP_NAMESPACE.ScrollPresenter scrollPresenter, MU_XCP_NAMESPACE.ZoomSnapPointBase zoomSnapPoint);
//     static Windows.UI.Color GetSnapPointVisualizationColor(MU_XCP_NAMESPACE.SnapPointBase snapPoint);
//     static void SetSnapPointVisualizationColor(MU_XCP_NAMESPACE.SnapPointBase snapPoint, Windows.UI.Color color);
//     static event Windows.Foundation.TypedEventHandler<MU_XCP_NAMESPACE.ScrollPresenter, ScrollPresenterTestHooksAnchorEvaluatedEventArgs> AnchorEvaluated;
//     static event Windows.Foundation.TypedEventHandler<MU_XCP_NAMESPACE.ScrollPresenter, ScrollPresenterTestHooksInteractionSourcesChangedEventArgs> InteractionSourcesChanged;
//     static event Windows.Foundation.TypedEventHandler<MU_XCP_NAMESPACE.ScrollPresenter, ScrollPresenterTestHooksExpressionAnimationStatusChangedEventArgs> ExpressionAnimationStatusChanged;
//     static event Windows.Foundation.TypedEventHandler<MU_XCP_NAMESPACE.ScrollPresenter, Object> ContentLayoutOffsetXChanged;
//     static event Windows.Foundation.TypedEventHandler<MU_XCP_NAMESPACE.ScrollPresenter, Object> ContentLayoutOffsetYChanged;
// }
