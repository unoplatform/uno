// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.Interactions;
using Uno.Disposables;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class ScrollPresenter : FrameworkElement
{
	// Properties of the ExpressionAnimationSources CompositionPropertySet
	private const string s_extentSourcePropertyName = "Extent";
	private const string s_viewportSourcePropertyName = "Viewport";
	private const string s_offsetSourcePropertyName = "Offset";
	private const string s_positionSourcePropertyName = "Position";
	private const string s_minPositionSourcePropertyName = "MinPosition";
	private const string s_maxPositionSourcePropertyName = "MaxPosition";
	private const string s_zoomFactorSourcePropertyName = "ZoomFactor";

	// Properties' default values.
	private const ScrollingChainMode s_defaultHorizontalScrollChainMode = ScrollingChainMode.Auto;
	private const ScrollingChainMode s_defaultVerticalScrollChainMode = ScrollingChainMode.Auto;
	private const ScrollingRailMode s_defaultHorizontalScrollRailMode = ScrollingRailMode.Enabled;
	private const ScrollingRailMode s_defaultVerticalScrollRailMode = ScrollingRailMode.Enabled;
	private const ScrollingScrollMode s_defaultHorizontalScrollMode = ScrollingScrollMode.Auto;
	private const ScrollingScrollMode s_defaultVerticalScrollMode = ScrollingScrollMode.Auto;
	private const ScrollingScrollMode s_defaultComputedHorizontalScrollMode = ScrollingScrollMode.Disabled;
	private const ScrollingScrollMode s_defaultComputedVerticalScrollMode = ScrollingScrollMode.Disabled;
	private const ScrollingChainMode s_defaultZoomChainMode = ScrollingChainMode.Auto;
	private const ScrollingZoomMode s_defaultZoomMode = ScrollingZoomMode.Disabled;
	private const ScrollingInputKinds s_defaultIgnoredInputKinds = ScrollingInputKinds.None;
	private const ScrollingContentOrientation s_defaultContentOrientation = ScrollingContentOrientation.Both;
	//private const bool s_defaultAnchorAtExtent = true;
	private const double s_defaultMinZoomFactor = 0.1;
	private const double s_defaultMaxZoomFactor = 10.0;
	private const double s_defaultAnchorRatio = 0.0;

	// ChangeOffsets scrolling constants
	internal const int s_offsetsChangeMsPerUnit = 5;
	internal const int s_offsetsChangeMinMs = 50;
	internal const int s_offsetsChangeMaxMs = 1000;

	// ChangeZoomFactor zooming constants
	internal const int s_zoomFactorChangeMsPerUnit = 250;
	internal const int s_zoomFactorChangeMinMs = 50;
	internal const int s_zoomFactorChangeMaxMs = 1000;

	// Number of ticks ellapsed before restarting the Translation and Scale animations to allow the Content
	// rasterization to be triggered after the Idle State is reached or a zoom factor change operation completed.
	private const int s_translationAndZoomFactorAnimationsRestartTicks = 4;

	private enum ScrollPresenterDimension
	{
		HorizontalScroll,
		VerticalScroll,
		HorizontalZoomFactor,
		VerticalZoomFactor,
		Scroll,
		ZoomFactor
	}

	// Invoked by ScrollPresenterTestHooks
	internal float GetContentLayoutOffsetX()
	{
		return m_contentLayoutOffsetX;
	}

	internal float GetContentLayoutOffsetY()
	{
		return m_contentLayoutOffsetY;
	}

	internal IList<ScrollSnapPointBase> GetConsolidatedHorizontalScrollSnapPoints()
	{
		return GetConsolidatedScrollSnapPoints(ScrollPresenterDimension.HorizontalScroll);
	}

	internal IList<ScrollSnapPointBase> GetConsolidatedVerticalScrollSnapPoints()
	{
		return GetConsolidatedScrollSnapPoints(ScrollPresenterDimension.VerticalScroll);
	}

	internal SnapPointWrapper<ScrollSnapPointBase> GetHorizontalSnapPointWrapper(ScrollSnapPointBase scrollSnapPoint)
	{
		return GetScrollSnapPointWrapper(ScrollPresenterDimension.HorizontalScroll, scrollSnapPoint);
	}

	internal SnapPointWrapper<ScrollSnapPointBase> GetVerticalSnapPointWrapper(ScrollSnapPointBase scrollSnapPoint)
	{
		return GetScrollSnapPointWrapper(ScrollPresenterDimension.VerticalScroll, scrollSnapPoint);
	}

	private bool HasBringingIntoViewListener()
	{
		return BringingIntoView is not null;
	}

	private int m_latestViewChangeCorrelationId;
	private int m_latestInteractionTrackerRequest;
#pragma warning disable CS0414 // The field 'ScrollPresenter.m_lastInteractionTrackerAsyncOperationType' is assigned but its value is never used
	private InteractionTrackerAsyncOperationType m_lastInteractionTrackerAsyncOperationType = InteractionTrackerAsyncOperationType.None;
#pragma warning restore CS0414 // The field 'ScrollPresenter.m_lastInteractionTrackerAsyncOperationType' is assigned but its value is never used
	private Vector2 m_endOfInertiaPosition = new Vector2(0.0f, 0.0f);
	private float m_animationRestartZoomFactor = 1.0f;
	private float m_endOfInertiaZoomFactor = 1.0f;
	private float m_zoomFactor = 1.0f;
	private float m_contentLayoutOffsetX = 0.0f;
	private float m_contentLayoutOffsetY = 0.0f;
	private double m_zoomedHorizontalOffset = 0.0;
	private double m_zoomedVerticalOffset = 0.0;
	private double m_unzoomedExtentWidth = 0.0;
	private double m_unzoomedExtentHeight = 0.0;
	private double m_viewportWidth = 0.0;
	private double m_viewportHeight = 0.0;
	private bool m_horizontalSnapPointsNeedViewportUpdates = false; // True when at least one horizontal snap point is not near aligned.
	private bool m_verticalSnapPointsNeedViewportUpdates = false; // True when at least one vertical snap point is not near aligned.
	private bool m_isAnchorElementDirty = true; // False when m_anchorElement is up-to-date, True otherwise.
	private bool m_isInertiaFromImpulse = false; // Only used on pre-RS5 versions, as a replacement for the InteractionTracker.IsInertiaFromImpulse property.

	// Number of ticks remaining before restarting the Translation and Scale animations to allow the Content
	// rasterization to be triggered after the Idle State is reached or a zoom factor change operation completed.
	private byte m_translationAndZoomFactorAnimationsRestartTicksCountdown;

	// For perf reasons, the value of ContentOrientation is cached.
	private ScrollingContentOrientation m_contentOrientation = s_defaultContentOrientation;
	private Size m_availableSize;

	private IScrollController m_horizontalScrollController;
	private IScrollController m_verticalScrollController;
	private IScrollControllerPanningInfo m_horizontalScrollControllerPanningInfo;
	private IScrollControllerPanningInfo m_verticalScrollControllerPanningInfo;
	private UIElement m_anchorElement;
	private ScrollingAnchorRequestedEventArgs m_anchorRequestedEventArgs;
	private List<UIElement> m_anchorCandidates = new();
	private List<InteractionTrackerAsyncOperation> m_interactionTrackerAsyncOperations = new();
	private Rect m_anchorElementBounds;
	private ScrollingInteractionState m_state = ScrollingInteractionState.Idle;
	private object m_pointerPressedEventHandler;
	private CompositionPropertySet m_expressionAnimationSources;
	private CompositionPropertySet m_horizontalScrollControllerExpressionAnimationSources;
	private CompositionPropertySet m_verticalScrollControllerExpressionAnimationSources;
	private VisualInteractionSource m_scrollPresenterVisualInteractionSource;
	private VisualInteractionSource m_horizontalScrollControllerVisualInteractionSource;
	private VisualInteractionSource m_verticalScrollControllerVisualInteractionSource;
	private InteractionTracker m_interactionTracker;
	private IInteractionTrackerOwner m_interactionTrackerOwner;
	private ExpressionAnimation m_minPositionExpressionAnimation;
	private ExpressionAnimation m_maxPositionExpressionAnimation;
	private ExpressionAnimation m_translationExpressionAnimation;
	//private ExpressionAnimation m_transformMatrixTranslateXExpressionAnimation;
	//private ExpressionAnimation m_transformMatrixTranslateYExpressionAnimation;
	private ExpressionAnimation m_zoomFactorExpressionAnimation;
	//private ExpressionAnimation m_transformMatrixZoomFactorExpressionAnimation;

	private ExpressionAnimation m_positionSourceExpressionAnimation;
	private ExpressionAnimation m_minPositionSourceExpressionAnimation;
	private ExpressionAnimation m_maxPositionSourceExpressionAnimation;
	private ExpressionAnimation m_zoomFactorSourceExpressionAnimation;

	private ExpressionAnimation m_horizontalScrollControllerOffsetExpressionAnimation;
	private ExpressionAnimation m_horizontalScrollControllerMaxOffsetExpressionAnimation;
	private ExpressionAnimation m_verticalScrollControllerOffsetExpressionAnimation;
	private ExpressionAnimation m_verticalScrollControllerMaxOffsetExpressionAnimation;

	// Event Sources
	//event_source<winrt::ViewportChangedEventHandler> m_viewportChanged{ this };
	//event_source<winrt::PostArrangeEventHandler> m_postArrange{ this };
	//event_source<winrt::ConfigurationChangedEventHandler> m_configurationChanged{ this };

	// Event Revokers
	private readonly SerialDisposable m_renderingRevoker = new();
	private readonly SerialDisposable m_loadedRevoker = new();
	private readonly SerialDisposable m_unloadedRevoker = new();
	private readonly SerialDisposable m_bringIntoViewRequestedRevoker = new();
	private readonly SerialDisposable m_contentMinWidthChangedRevoker = new();
	private readonly SerialDisposable m_contentWidthChangedRevoker = new();
	private readonly SerialDisposable m_contentMaxWidthChangedRevoker = new();
	private readonly SerialDisposable m_contentMinHeightChangedRevoker = new();
	private readonly SerialDisposable m_contentHeightChangedRevoker = new();
	private readonly SerialDisposable m_contentMaxHeightChangedRevoker = new();
	private readonly SerialDisposable m_contentHorizontalAlignmentChangedRevoker = new();
	private readonly SerialDisposable m_contentVerticalAlignmentChangedRevoker = new();

	private readonly SerialDisposable m_horizontalScrollControllerScrollToRequestedRevoker = new();
	private readonly SerialDisposable m_horizontalScrollControllerScrollByRequestedRevoker = new();
	private readonly SerialDisposable m_horizontalScrollControllerAddScrollVelocityRequestedRevoker = new();
	private readonly SerialDisposable m_horizontalScrollControllerPanningInfoChangedRevoker = new();
	private readonly SerialDisposable m_horizontalScrollControllerPanningInfoPanRequestedRevoker = new();

	private readonly SerialDisposable m_verticalScrollControllerScrollToRequestedRevoker = new();
	private readonly SerialDisposable m_verticalScrollControllerScrollByRequestedRevoker = new();
	private readonly SerialDisposable m_verticalScrollControllerAddScrollVelocityRequestedRevoker = new();
	private readonly SerialDisposable m_verticalScrollControllerPanningInfoChangedRevoker = new();
	private readonly SerialDisposable m_verticalScrollControllerPanningInfoPanRequestedRevoker = new();

	// Used on platforms where we don't have XamlRoot.
	// winrt::ICoreWindow::KeyDown_revoker m_coreWindowKeyDownRevoker{};
	// winrt::ICoreWindow::KeyUp_revoker m_coreWindowKeyUpRevoker{};

	private readonly SerialDisposable m_horizontalSnapPointsVectorChangedRevoker = new();
	private readonly SerialDisposable m_verticalSnapPointsVectorChangedRevoker = new();
	private readonly SerialDisposable m_zoomSnapPointsVectorChangedRevoker = new();

	private IList<ScrollSnapPointBase> m_horizontalSnapPoints;
	private IList<ScrollSnapPointBase> m_verticalSnapPoints;
	private IList<ZoomSnapPointBase> m_zoomSnapPoints;
	private SortedSet<SnapPointWrapper<ScrollSnapPointBase>> m_sortedConsolidatedHorizontalSnapPoints = new(new SnapPointWrapperComparator<ScrollSnapPointBase>());
	private SortedSet<SnapPointWrapper<ScrollSnapPointBase>> m_sortedConsolidatedVerticalSnapPoints = new(new SnapPointWrapperComparator<ScrollSnapPointBase>());
	private SortedSet<SnapPointWrapper<ZoomSnapPointBase>> m_sortedConsolidatedZoomSnapPoints = new(new SnapPointWrapperComparator<ZoomSnapPointBase>());

	// Maximum difference for offsets to be considered equal. Used for pointer wheel scrolling.
	//private const float s_offsetEqualityEpsilon = 0.00001f;
	// Maximum difference for zoom factors to be considered equal. Used for pointer wheel zooming.
	//private const float s_zoomFactorEqualityEpsilon = 0.00001f;

	// Property names being targeted for the ScrollPresenter.Content's Visual.
	// RedStone v1 case:
	//private const string s_transformMatrixTranslateXPropertyName = "TransformMatrix._41";
	//private const string s_transformMatrixTranslateYPropertyName = "TransformMatrix._42";
	//private const string s_transformMatrixScaleXPropertyName = "TransformMatrix._11";
	//private const string s_transformMatrixScaleYPropertyName = "TransformMatrix._22";
	// RedStone v2 and higher case:
	private const string s_translationPropertyName = "Translation";
	private const string s_scalePropertyName = "Scale";

	// Properties of the IScrollController's ExpressionAnimationSources CompositionPropertySet
	private const string s_minOffsetPropertyName = "MinOffset";
	private const string s_maxOffsetPropertyName = "MaxOffset";
	private const string s_offsetPropertyName = "Offset";
	private const string s_multiplierPropertyName = "Multiplier";

	// Properties used in snap points composition expressions
	private const string s_naturalRestingPositionXPropertyName = "NaturalRestingPosition.x";
	private const string s_naturalRestingPositionYPropertyName = "NaturalRestingPosition.y";
	private const string s_naturalRestingScalePropertyName = "NaturalRestingScale";
	private const string s_targetScalePropertyName = "this.Target.Scale";
}
