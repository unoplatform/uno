// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.Interactions;
using Microsoft.UI.Input;
using Microsoft.UI.Private.Controls;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Uno;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;

#if HAS_UNO_WINUI
using PointerDeviceType = Microsoft.UI.Input.PointerDeviceType;
#else
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
#endif

using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls.Primitives;

[ContentProperty(Name = "Content")]
#if !__SKIA__
[NotImplemented("__ANDROID__", "__APPLE_UIKIT__", "__WASM__")]
#endif
public partial class ScrollPresenter : FrameworkElement, IScrollAnchorProvider, IScrollPresenter
{
	// Change to 'true' to turn on debugging outputs in Output window
	//bool ScrollPresenterTrace::s_IsDebugOutputEnabled{ false };
	//bool ScrollPresenterTrace::s_IsVerboseDebugOutputEnabled{ false };

	// Number of pixels scrolled when the automation peer requests a line-type change.
	private const double c_scrollPresenterLineDelta = 16.0;

	// Default inertia decay rate used when a IScrollController makes a request for
	// an offset change with additional velocity.
	private const float c_scrollPresenterDefaultInertiaDecayRate = 0.95f;

	private const int s_noOpCorrelationId = -1;

	~ScrollPresenter()
	{
		// SCROLLPRESENTER_TRACE_INFO(nullptr, TRACE_MSG_METH, METH_NAME, this);

		UnhookCompositionTargetRendering();
		UnhookScrollPresenterEvents();
	}

	private UIElementCollection Children { get; }

	public ScrollPresenter()
	{
		// SCROLLPRESENTER_TRACE_INFO(nullptr, TRACE_MSG_METH, METH_NAME, this);

		// __RP_Marker_ClassById(RuntimeProfiler::ProfId_ScrollPresenter);

		// EnsureProperties();

		UIElement.RegisterAsScrollPort(this);

		HookScrollPresenterEvents();

		// Uno specific
		InitializePartial();
		Children = new UIElementCollection(this);
		base.Background = new SolidColorBrush(Colors.Transparent); // TODO: Remove when Background is moved from FrameworkElement to Control

		// Set the default Transparent background so that hit-testing allows to start a touch manipulation
		// outside the boundaries of the Content, when it's smaller than the ScrollPresenter.
		Background = new SolidColorBrush(Colors.Transparent);
	}

	// Uno specific
	partial void InitializePartial();

	#region Automation Peer Helpers

	// Public methods accessed by the ScrollPresenterAutomationPeer class

	internal double GetZoomedExtentWidth()
	{
		return m_unzoomedExtentWidth * m_zoomFactor;
	}

	internal double GetZoomedExtentHeight()
	{
		return m_unzoomedExtentHeight * m_zoomFactor;
	}

	internal void PageLeft()
	{
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH, METH_NAME, this);

		ScrollToHorizontalOffset(m_zoomedHorizontalOffset - ViewportWidth);
	}

	internal void PageRight()
	{
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH, METH_NAME, this);

		ScrollToHorizontalOffset(m_zoomedHorizontalOffset + ViewportWidth);
	}

	internal void PageUp()
	{
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH, METH_NAME, this);

		ScrollToVerticalOffset(m_zoomedVerticalOffset - ViewportHeight);
	}

	internal void PageDown()
	{
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH, METH_NAME, this);

		ScrollToVerticalOffset(m_zoomedVerticalOffset + ViewportHeight);
	}

	internal void LineLeft()
	{
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH, METH_NAME, this);

		ScrollToHorizontalOffset(m_zoomedHorizontalOffset - c_scrollPresenterLineDelta);
	}

	internal void LineRight()
	{
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH, METH_NAME, this);

		ScrollToHorizontalOffset(m_zoomedHorizontalOffset + c_scrollPresenterLineDelta);
	}

	internal void LineUp()
	{
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH, METH_NAME, this);

		ScrollToVerticalOffset(m_zoomedVerticalOffset - c_scrollPresenterLineDelta);
	}

	internal void LineDown()
	{
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH, METH_NAME, this);

		ScrollToVerticalOffset(m_zoomedVerticalOffset + c_scrollPresenterLineDelta);
	}

	internal void ScrollToHorizontalOffset(double offset)
	{
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_DBL, METH_NAME, this, offset);

		ScrollToOffsets(offset /*zoomedHorizontalOffset*/, m_zoomedVerticalOffset /*zoomedVerticalOffset*/);
	}

	internal void ScrollToVerticalOffset(double offset)
	{
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_DBL, METH_NAME, this, offset);

		ScrollToOffsets(m_zoomedHorizontalOffset /*zoomedHorizontalOffset*/, offset /*zoomedVerticalOffset*/);
	}

	internal void ScrollToOffsets(
		double horizontalOffset, double verticalOffset)
	{
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_DBL_DBL, METH_NAME, this, horizontalOffset, verticalOffset);

		if (m_interactionTracker is not null)
		{
			ScrollingScrollOptions options =
				new ScrollingScrollOptions(
					ScrollingAnimationMode.Disabled,
					ScrollingSnapPointsMode.Ignore);

			OffsetsChange offsetsChange =
				new OffsetsChange(
					horizontalOffset,
					verticalOffset,
					ScrollPresenterViewKind.Absolute,
					options); // NOTE: Using explicit cast to IInspectable to work around 17532876

			ProcessOffsetsChange(
				InteractionTrackerAsyncOperationTrigger.DirectViewChange,
				offsetsChange,
				s_noOpCorrelationId /*offsetsChangeCorrelationId*/,
				false /*isForAsyncOperation*/);
		}
	}

	#endregion

	#region IUIElementOverridesHelper

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH, METH_NAME, this);

		return new ScrollPresenterAutomationPeer(this);
	}

	#endregion

	#region IScrollPresenter

	public CompositionPropertySet ExpressionAnimationSources
	{
		get
		{
			SetupInteractionTrackerBoundaries();
			EnsureExpressionAnimationSources();

			return m_expressionAnimationSources;
		}
	}

	public double HorizontalOffset => m_zoomedHorizontalOffset;

	public double VerticalOffset => m_zoomedVerticalOffset;

	public float ZoomFactor => m_zoomFactor;

	public double ExtentWidth => m_unzoomedExtentWidth;

	public double ExtentHeight => m_unzoomedExtentHeight;

	public double ViewportWidth => m_viewportWidth;

	public double ViewportHeight => m_viewportHeight;

	public double ScrollableWidth => Math.Max(0.0, GetZoomedExtentWidth() - ViewportWidth);

	public double ScrollableHeight => Math.Max(0.0, GetZoomedExtentHeight() - ViewportHeight);

	public IScrollController HorizontalScrollController
	{
		get => m_horizontalScrollController;
		set
		{
			// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_PTR, METH_NAME, this, value);

			if (m_horizontalScrollController is not null)
			{
				UnhookHorizontalScrollControllerEvents();

				if (m_horizontalScrollControllerPanningInfo is not null)
				{
					UnhookHorizontalScrollControllerPanningInfoEvents();

					if (m_horizontalScrollControllerExpressionAnimationSources is not null)
					{
						m_horizontalScrollControllerPanningInfo.SetPanningElementExpressionAnimationSources(
							null /*scrollControllerExpressionAnimationSources*/,
							s_minOffsetPropertyName,
							s_maxOffsetPropertyName,
							s_offsetPropertyName,
							s_multiplierPropertyName);
					}
				}
			}

			m_horizontalScrollController = value;
			m_horizontalScrollControllerPanningInfo = value is not null ? value.PanningInfo : null;

			if (m_interactionTracker is not null)
			{
				SetupScrollControllerVisualInterationSource(ScrollPresenterDimension.HorizontalScroll);
			}

			if (m_horizontalScrollController is not null)
			{
				HookHorizontalScrollControllerEvents(
					m_horizontalScrollController);

				if (m_horizontalScrollControllerPanningInfo is not null)
				{
					HookHorizontalScrollControllerPanningInfoEvents(
						m_horizontalScrollControllerPanningInfo,
						m_horizontalScrollControllerVisualInteractionSource != null /*hasInteractionSource*/);
				}

				UpdateScrollControllerValues(ScrollPresenterDimension.HorizontalScroll);
				UpdateScrollControllerIsScrollable(ScrollPresenterDimension.HorizontalScroll);

				if (m_horizontalScrollControllerPanningInfo is not null && m_horizontalScrollControllerExpressionAnimationSources is not null)
				{
					m_horizontalScrollControllerPanningInfo.SetPanningElementExpressionAnimationSources(
						m_horizontalScrollControllerExpressionAnimationSources,
						s_minOffsetPropertyName,
						s_maxOffsetPropertyName,
						s_offsetPropertyName,
						s_multiplierPropertyName);
				}
			}
		}
	}

	public IScrollController VerticalScrollController
	{
		get => m_verticalScrollController;
		set
		{
			// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_PTR, METH_NAME, this, value);

			if (m_verticalScrollController is not null)
			{
				UnhookVerticalScrollControllerEvents();
			}

			if (m_verticalScrollControllerPanningInfo is not null)
			{
				UnhookVerticalScrollControllerPanningInfoEvents();

				if (m_verticalScrollControllerExpressionAnimationSources is not null)
				{
					m_verticalScrollControllerPanningInfo.SetPanningElementExpressionAnimationSources(
						null /*scrollControllerExpressionAnimationSources*/,
						s_minOffsetPropertyName,
						s_maxOffsetPropertyName,
						s_offsetPropertyName,
						s_multiplierPropertyName);
				}
			}

			m_verticalScrollController = value;
			m_verticalScrollControllerPanningInfo = value is not null ? value.PanningInfo : null;

			if (m_interactionTracker is not null)
			{
				SetupScrollControllerVisualInterationSource(ScrollPresenterDimension.VerticalScroll);
			}

			if (m_verticalScrollController is not null)
			{
				HookVerticalScrollControllerEvents(
					m_verticalScrollController);

				if (m_verticalScrollControllerPanningInfo is not null)
				{
					HookVerticalScrollControllerPanningInfoEvents(
						m_verticalScrollControllerPanningInfo,
						m_verticalScrollControllerVisualInteractionSource != null /*hasInteractionSource*/);
				}

				UpdateScrollControllerValues(ScrollPresenterDimension.VerticalScroll);
				UpdateScrollControllerIsScrollable(ScrollPresenterDimension.VerticalScroll);

				if (m_verticalScrollControllerPanningInfo is not null && m_verticalScrollControllerExpressionAnimationSources is not null)
				{
					m_verticalScrollControllerPanningInfo.SetPanningElementExpressionAnimationSources(
						m_verticalScrollControllerExpressionAnimationSources,
						s_minOffsetPropertyName,
						s_maxOffsetPropertyName,
						s_offsetPropertyName,
						s_multiplierPropertyName);
				}
			}
		}
	}

	public ScrollingInputKinds IgnoredInputKinds
	{
		get
		{
			// Workaround for Bug 17377013: XamlCompiler codegen for Enum CreateFromString always returns boxed int which is wrong for [flags] enums (should be uint)
			// Check if the boxed IgnoredInputKinds is an IReference<int> first in which case we unbox as int.
			var boxedKind = GetValue(IgnoredInputKindsProperty);
			if (boxedKind is int boxedInt)
			{
				return (ScrollingInputKinds)((uint)boxedInt);
			}

			return (ScrollingInputKinds)boxedKind;
		}
		set => SetValue(IgnoredInputKindsProperty, value);
	}

	public ScrollingInteractionState State
	{
		get => m_state;
	}

	public IList<ScrollSnapPointBase> HorizontalSnapPoints
	{
		get
		{
			if (m_horizontalSnapPoints is null)
			{
				m_horizontalSnapPoints = new ObservableVector<ScrollSnapPointBase>();

				var horizontalSnapPointsObservableVector = m_horizontalSnapPoints as IObservableVector<ScrollSnapPointBase>;
				horizontalSnapPointsObservableVector.VectorChanged += OnHorizontalSnapPointsVectorChanged;
				// Uno TODO: Figure out where to dispose this, if needed. It currently won't be disposed because this line is hit only once on the first property access.
				// And nowhere else we do dispose the revoker.
				m_horizontalSnapPointsVectorChangedRevoker.Disposable = Disposable.Create(() => horizontalSnapPointsObservableVector.VectorChanged -= OnHorizontalSnapPointsVectorChanged);
			}
			return m_horizontalSnapPoints;
		}
	}

	public IList<ScrollSnapPointBase> VerticalSnapPoints
	{
		get
		{
			if (m_verticalSnapPoints is null)
			{
				m_verticalSnapPoints = new ObservableVector<ScrollSnapPointBase>();

				var verticalSnapPointsObservableVector = m_verticalSnapPoints as IObservableVector<ScrollSnapPointBase>;
				verticalSnapPointsObservableVector.VectorChanged += OnVerticalSnapPointsVectorChanged;
				// Uno TODO: Figure out where to dispose this, if needed. It currently won't be disposed because this line is hit only once on the first property access.
				// And nowhere else we do dispose the revoker.
				m_verticalSnapPointsVectorChangedRevoker.Disposable = Disposable.Create(() => verticalSnapPointsObservableVector.VectorChanged -= OnVerticalSnapPointsVectorChanged);
			}
			return m_verticalSnapPoints;
		}
	}

	public IList<ZoomSnapPointBase> ZoomSnapPoints
	{
		get
		{
			if (m_zoomSnapPoints is null)
			{
				m_zoomSnapPoints = new ObservableVector<ZoomSnapPointBase>();

				var zoomSnapPointsObservableVector = m_zoomSnapPoints as IObservableVector<ZoomSnapPointBase>;
				zoomSnapPointsObservableVector.VectorChanged += OnZoomSnapPointsVectorChanged;
				// Uno TODO: Figure out where to dispose this, if needed. It currently won't be disposed because this line is hit only once on the first property access.
				// And nowhere else we do dispose the revoker.
				m_zoomSnapPointsVectorChangedRevoker.Disposable = Disposable.Create(() => zoomSnapPointsObservableVector.VectorChanged -= OnZoomSnapPointsVectorChanged);
			}
			return m_zoomSnapPoints;
		}
	}

	public int ScrollTo(double horizontalOffset, double verticalOffset)
	{
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_DBL_DBL, METH_NAME, this, horizontalOffset, verticalOffset);

		return ScrollTo(horizontalOffset, verticalOffset, null /*options*/);
	}

	public int ScrollTo(double horizontalOffset, double verticalOffset, ScrollingScrollOptions options)
	{
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_DBL_DBL_STR, METH_NAME, this,
		// 	horizontalOffset, verticalOffset, TypeLogging::ScrollOptionsToString(options).c_str());

		int viewChangeCorrelationId;
		ChangeOffsetsPrivate(
			horizontalOffset /*zoomedHorizontalOffset*/,
			verticalOffset /*zoomedVerticalOffset*/,
			ScrollPresenterViewKind.Absolute,
			options,
			null /*bringIntoViewRequestedEventArgs*/,
			InteractionTrackerAsyncOperationTrigger.DirectViewChange,
			s_noOpCorrelationId /*existingViewChangeCorrelationId*/,
			out viewChangeCorrelationId);

		return viewChangeCorrelationId;
	}

	public int ScrollBy(double horizontalOffsetDelta, double verticalOffsetDelta)
	{
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_DBL_DBL, METH_NAME, this, horizontalOffsetDelta, verticalOffsetDelta);

		return ScrollBy(horizontalOffsetDelta, verticalOffsetDelta, null /*options*/);
	}

	public int ScrollBy(double horizontalOffsetDelta, double verticalOffsetDelta, ScrollingScrollOptions options)
	{
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_DBL_DBL_STR, METH_NAME, this,
		// 	horizontalOffsetDelta, verticalOffsetDelta, TypeLogging::ScrollOptionsToString(options).c_str());

		int viewChangeCorrelationId;
		ChangeOffsetsPrivate(
			horizontalOffsetDelta /*zoomedHorizontalOffset*/,
			verticalOffsetDelta /*zoomedVerticalOffset*/,
			ScrollPresenterViewKind.RelativeToCurrentView,
			options,
			null /*bringIntoViewRequestedEventArgs*/,
			InteractionTrackerAsyncOperationTrigger.DirectViewChange,
			s_noOpCorrelationId /*existingViewChangeCorrelationId*/,
			out viewChangeCorrelationId);

		return viewChangeCorrelationId;
	}

	public int AddScrollVelocity(Vector2 offsetsVelocity, Vector2? inertiaDecayRate)
	{
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_STR, METH_NAME, this,
		// 	TypeLogging::Float2ToString(offsetsVelocity).c_str(), TypeLogging::NullableFloat2ToString(inertiaDecayRate).c_str());

		int viewChangeCorrelationId;
		ChangeOffsetsWithAdditionalVelocityPrivate(
			offsetsVelocity,
			Vector2.Zero /*anticipatedOffsetsChange*/,
			inertiaDecayRate,
			InteractionTrackerAsyncOperationTrigger.DirectViewChange,
			out viewChangeCorrelationId);

		return viewChangeCorrelationId;
	}

	public int ZoomTo(float zoomFactor, Vector2? centerPoint)
	{
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_FLT, METH_NAME, this,
		// 	TypeLogging::NullableFloat2ToString(centerPoint).c_str(), zoomFactor);

		return ZoomTo(zoomFactor, centerPoint, null /*options*/);
	}

	public int ZoomTo(float zoomFactor, Vector2? centerPoint, ScrollingZoomOptions options)
	{
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_STR_FLT, METH_NAME, this,
		// 	TypeLogging::NullableFloat2ToString(centerPoint).c_str(),
		// 	TypeLogging::ZoomOptionsToString(options).c_str(),
		// 	zoomFactor);

		int viewChangeCorrelationId;
		ChangeZoomFactorPrivate(
			zoomFactor,
			centerPoint,
			ScrollPresenterViewKind.Absolute,
			options,
			out viewChangeCorrelationId);

		return viewChangeCorrelationId;
	}

	public int ZoomBy(float zoomFactorDelta, Vector2? centerPoint)
	{
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_FLT, METH_NAME, this,
		// 	TypeLogging::NullableFloat2ToString(centerPoint).c_str(),
		// 	zoomFactorDelta);

		return ZoomBy(zoomFactorDelta, centerPoint, null /*options*/);
	}

	public int ZoomBy(float zoomFactorDelta, Vector2? centerPoint, ScrollingZoomOptions options)
	{
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_STR_FLT, METH_NAME, this,
		// 	TypeLogging::NullableFloat2ToString(centerPoint).c_str(),
		// 	TypeLogging::ZoomOptionsToString(options).c_str(),
		// 	zoomFactorDelta);

		int viewChangeCorrelationId;
		ChangeZoomFactorPrivate(
			zoomFactorDelta,
			centerPoint,
			ScrollPresenterViewKind.RelativeToCurrentView,
			options,
			out viewChangeCorrelationId);

		return viewChangeCorrelationId;
	}

	public int AddZoomVelocity(float zoomFactorVelocity, Vector2? centerPoint, float? inertiaDecayRate)
	{
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_STR_FLT, METH_NAME, this,
		// 	TypeLogging::NullableFloat2ToString(centerPoint).c_str(),
		// 	TypeLogging::NullableFloatToString(inertiaDecayRate).c_str(),
		// 	zoomFactorVelocity);

		int viewChangeCorrelationId;
		ChangeZoomFactorWithAdditionalVelocityPrivate(
			zoomFactorVelocity,
			0.0f /*anticipatedZoomFactorChange*/,
			centerPoint,
			inertiaDecayRate,
			InteractionTrackerAsyncOperationTrigger.DirectViewChange,
			out viewChangeCorrelationId);

		return viewChangeCorrelationId;
	}

	#endregion

	#region IFrameworkElementOverridesHelper

	protected override Size MeasureOverride(Size availableSize)
	{
#if DEBUG
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_FLT_FLT, METH_NAME, this, L"availableSize:", availableSize.Width, availableSize.Height);
#endif

		m_availableSize = availableSize;

		Size contentDesiredSize = default;
		UIElement content = Content;

		if (content is not null)
		{
			// The content is measured with infinity in the directions in which it is not constrained, enabling this ScrollPresenter
			// to be scrollable in those directions.
			Size contentAvailableSize = new Size(
				(m_contentOrientation == ScrollingContentOrientation.Vertical || m_contentOrientation == ScrollingContentOrientation.None) ?
					availableSize.Width : float.PositiveInfinity,
				(m_contentOrientation == ScrollingContentOrientation.Horizontal || m_contentOrientation == ScrollingContentOrientation.None) ?
					availableSize.Height : float.PositiveInfinity
			);

			if (m_contentOrientation != ScrollingContentOrientation.Both)
			{
				FrameworkElement contentAsFE = content as FrameworkElement;

				if (contentAsFE is not null)
				{
					Thickness contentMargin = contentAsFE.Margin;

					if (m_contentOrientation == ScrollingContentOrientation.Vertical || m_contentOrientation == ScrollingContentOrientation.None)
					{
						// Even though the content's Width is constrained, take into account the MinWidth, Width and MaxWidth values
						// potentially set on the content so it is allowed to grow accordingly.
						contentAvailableSize.Width = (float)(GetComputedMaxWidth(availableSize.Width, contentAsFE));
					}
					if (m_contentOrientation == ScrollingContentOrientation.Horizontal || m_contentOrientation == ScrollingContentOrientation.None)
					{
						// Even though the content's Height is constrained, take into account the MinHeight, Height and MaxHeight values
						// potentially set on the content so it is allowed to grow accordingly.
						contentAvailableSize.Height = (float)(GetComputedMaxHeight(availableSize.Height, contentAsFE));
					}
				}
			}

			content.Measure(contentAvailableSize);
			contentDesiredSize = content.DesiredSize;
		}

		// The framework determines that this ScrollPresenter is scrollable when unclippedDesiredSize.Width/Height > desiredSize.Width/Height
#if DEBUG
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_FLT_FLT, METH_NAME, this, L"contentDesiredSize:", contentDesiredSize.Width, contentDesiredSize.Height);
#endif

		return contentDesiredSize;
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
#if DEBUG
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_FLT_FLT, METH_NAME, this, L"finalSize", finalSize.Width, finalSize.Height);
#endif

		UIElement content = Content;
		Rect finalContentRect;

		// Possible cases:
		// 1. m_availableSize is infinite, the ScrollPresenter is not constrained and takes its Content DesiredSize.
		//    viewport thus is finalSize.
		// 2. m_availableSize > finalSize, the ScrollPresenter is constrained and its Content is smaller than the available size.
		//    No matter the ScrollPresenter's alignment, it does not grow larger than finalSize. viewport is finalSize again.
		// 3. m_availableSize <= finalSize, the ScrollPresenter is constrained and its Content is larger than or equal to
		//    the available size. viewport is the smaller & constrained m_availableSize.
		Size viewport = new Size(
			Math.Min(finalSize.Width, m_availableSize.Width),
			Math.Min(finalSize.Height, m_availableSize.Height)
		);

		bool renderSizeChanged = false;
		double newUnzoomedExtentWidth = 0.0;
		double newUnzoomedExtentHeight = 0.0;

		if (content is not null)
		{
			float contentLayoutOffsetXDelta = 0.0f;
			float contentLayoutOffsetYDelta = 0.0f;
			bool isAnchoringElementHorizontally = false;
			bool isAnchoringElementVertically = false;
			bool isAnchoringFarEdgeHorizontally = false;
			bool isAnchoringFarEdgeVertically = false;
			Size oldRenderSize = content.RenderSize;
			Size contentArrangeSize = content.DesiredSize;

			FrameworkElement contentAsFE = content as FrameworkElement;

			Thickness contentMargin = contentAsFE is not null ? contentAsFE.Margin : default;

			bool wasContentArrangeWidthStretched = contentAsFE is not null &&
				contentAsFE.HorizontalAlignment == HorizontalAlignment.Stretch &&
				double.IsNaN(contentAsFE.Width) &&
				contentArrangeSize.Width < viewport.Width;

			bool wasContentArrangeHeightStretched = contentAsFE is not null &&
				contentAsFE.VerticalAlignment == VerticalAlignment.Stretch &&
				double.IsNaN(contentAsFE.Height) &&
				contentArrangeSize.Height < viewport.Height;

			if (wasContentArrangeWidthStretched)
			{
				// Allow the content to stretch up to the larger viewport width.
				contentArrangeSize.Width = viewport.Width;
			}

			if (wasContentArrangeHeightStretched)
			{
				// Allow the content to stretch up to the larger viewport height.
				contentArrangeSize.Height = viewport.Height;
			}

			finalContentRect = new Rect
			(
				m_contentLayoutOffsetX,
				m_contentLayoutOffsetY,
				contentArrangeSize.Width,
				contentArrangeSize.Height
			);

			IsAnchoring(out isAnchoringElementHorizontally, out isAnchoringElementVertically, out isAnchoringFarEdgeHorizontally, out isAnchoringFarEdgeVertically);

			MUX_ASSERT(!(isAnchoringElementHorizontally && isAnchoringFarEdgeHorizontally));
			MUX_ASSERT(!(isAnchoringElementVertically && isAnchoringFarEdgeVertically));

			if (isAnchoringElementHorizontally || isAnchoringElementVertically || isAnchoringFarEdgeHorizontally || isAnchoringFarEdgeVertically)
			{
				MUX_ASSERT(m_interactionTracker is not null);

				Size preArrangeViewportToElementAnchorPointsDistance = new Size(float.NaN, float.NaN);

				if (isAnchoringElementHorizontally || isAnchoringElementVertically)
				{
					EnsureAnchorElementSelection();
					preArrangeViewportToElementAnchorPointsDistance = ComputeViewportToElementAnchorPointsDistance(
						m_viewportWidth,
						m_viewportHeight,
						true /*isForPreArrange*/);
				}
				else
				{
					ResetAnchorElement();
				}

				contentArrangeSize = ArrangeContent(
					content,
					contentMargin,
					finalContentRect,
					wasContentArrangeWidthStretched,
					wasContentArrangeHeightStretched);

				if (!double.IsNaN(preArrangeViewportToElementAnchorPointsDistance.Width) || !double.IsNaN(preArrangeViewportToElementAnchorPointsDistance.Height))
				{
					// Using the new viewport sizes to handle the cases where an adjustment needs to be performed because of a ScrollPresenter size change.
					Size postArrangeViewportToElementAnchorPointsDistance = ComputeViewportToElementAnchorPointsDistance(
						viewport.Width /*viewportWidth*/,
						viewport.Height /*viewportHeight*/,
						false /*isForPreArrange*/);

					if (isAnchoringElementHorizontally &&
						!double.IsNaN(preArrangeViewportToElementAnchorPointsDistance.Width) &&
						!double.IsNaN(postArrangeViewportToElementAnchorPointsDistance.Width) &&
						preArrangeViewportToElementAnchorPointsDistance.Width != postArrangeViewportToElementAnchorPointsDistance.Width)
					{
						// Perform horizontal offset adjustment due to element anchoring
						contentLayoutOffsetXDelta = ComputeContentLayoutOffsetDelta(
							ScrollPresenterDimension.HorizontalScroll,
							// Uno docs: This cast isn't in WinUI code.
							(float)(postArrangeViewportToElementAnchorPointsDistance.Width - preArrangeViewportToElementAnchorPointsDistance.Width) /*unzoomedDelta*/);
					}

					if (isAnchoringElementVertically &&
						!double.IsNaN(preArrangeViewportToElementAnchorPointsDistance.Height) &&
						!double.IsNaN(postArrangeViewportToElementAnchorPointsDistance.Height) &&
						preArrangeViewportToElementAnchorPointsDistance.Height != postArrangeViewportToElementAnchorPointsDistance.Height)
					{
						// Perform vertical offset adjustment due to element anchoring
						contentLayoutOffsetYDelta = ComputeContentLayoutOffsetDelta(
							ScrollPresenterDimension.VerticalScroll,
							// Uno docs: This cast isn't in WinUI code.
							(float)(postArrangeViewportToElementAnchorPointsDistance.Height - preArrangeViewportToElementAnchorPointsDistance.Height) /*unzoomedDelta*/);
					}
				}
			}
			else
			{
				ResetAnchorElement();

				contentArrangeSize = ArrangeContent(
					content,
					contentMargin,
					finalContentRect,
					wasContentArrangeWidthStretched,
					wasContentArrangeHeightStretched);
			}

			newUnzoomedExtentWidth = contentArrangeSize.Width;
			newUnzoomedExtentHeight = contentArrangeSize.Height;

			double maxUnzoomedExtentWidth = double.PositiveInfinity;
			double maxUnzoomedExtentHeight = double.PositiveInfinity;

			if (contentAsFE is not null)
			{
				// Determine the maximum size directly set on the content, if any.
				maxUnzoomedExtentWidth = GetComputedMaxWidth(maxUnzoomedExtentWidth, contentAsFE);
				maxUnzoomedExtentHeight = GetComputedMaxHeight(maxUnzoomedExtentHeight, contentAsFE);
			}

			// Take into account the actual resulting rendering size, in case it's larger than the desired size.
			// But the extent must not exceed the size explicitly set on the content, if any.
			newUnzoomedExtentWidth = Math.Max(
				newUnzoomedExtentWidth,
				Math.Max(0.0, content.RenderSize.Width + contentMargin.Left + contentMargin.Right));
			newUnzoomedExtentWidth = Math.Min(
				newUnzoomedExtentWidth,
				maxUnzoomedExtentWidth);

			newUnzoomedExtentHeight = Math.Max(
				newUnzoomedExtentHeight,
				Math.Max(0.0, content.RenderSize.Height + contentMargin.Top + contentMargin.Bottom));
			newUnzoomedExtentHeight = Math.Min(
				newUnzoomedExtentHeight,
				maxUnzoomedExtentHeight);

			if (isAnchoringFarEdgeHorizontally)
			{
				float unzoomedDelta = 0.0f;

				if (newUnzoomedExtentWidth > m_unzoomedExtentWidth ||                                  // ExtentWidth grew
					m_zoomedHorizontalOffset + m_viewportWidth > m_zoomFactor * m_unzoomedExtentWidth) // ExtentWidth shrank while overpanning
				{
					// Perform horizontal offset adjustment due to edge anchoring
					unzoomedDelta = (float)(newUnzoomedExtentWidth - m_unzoomedExtentWidth);
				}

				if ((float)m_viewportWidth > viewport.Width)
				{
					// Viewport width shrank: Perform horizontal offset adjustment due to edge anchoring
					// Uno specific: In WinUI, the float cast is only applied to m_viewportWidth, but this results in a compilation error because the whole expression becomes
					//				 a double and it can't be converted to float.
					unzoomedDelta += (float)((m_viewportWidth) - viewport.Width) / m_zoomFactor;
				}

				if (unzoomedDelta != 0.0f)
				{
					MUX_ASSERT(contentLayoutOffsetXDelta == 0.0f);
					contentLayoutOffsetXDelta = ComputeContentLayoutOffsetDelta(ScrollPresenterDimension.HorizontalScroll, unzoomedDelta);
				}
			}

			if (isAnchoringFarEdgeVertically)
			{
				float unzoomedDelta = 0.0f;

				if (newUnzoomedExtentHeight > m_unzoomedExtentHeight ||                                // ExtentHeight grew
					m_zoomedVerticalOffset + m_viewportHeight > m_zoomFactor * m_unzoomedExtentHeight) // ExtentHeight shrank while overpanning
				{
					// Perform vertical offset adjustment due to edge anchoring
					unzoomedDelta = (float)(newUnzoomedExtentHeight - m_unzoomedExtentHeight);
				}

				if ((float)(m_viewportHeight) > viewport.Height)
				{
					// Viewport height shrank: Perform vertical offset adjustment due to edge anchoring
					// Uno specific: In WinUI, the float cast is only applied to m_viewportHeight, but this results in a compilation error because the whole expression becomes
					//				 a double and it can't be converted to float.
					unzoomedDelta += (float)((m_viewportHeight) - viewport.Height) / m_zoomFactor;
				}

				if (unzoomedDelta != 0.0f)
				{
					MUX_ASSERT(contentLayoutOffsetYDelta == 0.0f);
					contentLayoutOffsetYDelta = ComputeContentLayoutOffsetDelta(ScrollPresenterDimension.VerticalScroll, unzoomedDelta);
				}
			}

			if (contentLayoutOffsetXDelta != 0.0f || contentLayoutOffsetYDelta != 0.0f)
			{
				Rect contentRectWithDelta = new
				(
					m_contentLayoutOffsetX + contentLayoutOffsetXDelta,
					m_contentLayoutOffsetY + contentLayoutOffsetYDelta,
					contentArrangeSize.Width,
					contentArrangeSize.Height
				);

#if DEBUG
				// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_STR, METH_NAME, this, L"content Arrange", TypeLogging::RectToString(contentRectWithDelta).c_str());
#endif

				content.Arrange(contentRectWithDelta);

				if (contentLayoutOffsetXDelta != 0.0f)
				{
					m_contentLayoutOffsetX += contentLayoutOffsetXDelta;
					UpdateOffset(ScrollPresenterDimension.HorizontalScroll, m_zoomedHorizontalOffset - contentLayoutOffsetXDelta);
					OnContentLayoutOffsetChanged(ScrollPresenterDimension.HorizontalScroll);
				}

				if (contentLayoutOffsetYDelta != 0.0f)
				{
					m_contentLayoutOffsetY += contentLayoutOffsetYDelta;
					UpdateOffset(ScrollPresenterDimension.VerticalScroll, m_zoomedVerticalOffset - contentLayoutOffsetYDelta);
					OnContentLayoutOffsetChanged(ScrollPresenterDimension.VerticalScroll);
				}

				OnViewChanged(contentLayoutOffsetXDelta != 0.0f /*horizontalOffsetChanged*/, contentLayoutOffsetYDelta != 0.0f /*verticalOffsetChanged*/);
			}

			renderSizeChanged = content.RenderSize != oldRenderSize;
		}

		// Set a rectangular clip on this ScrollPresenter the same size as the arrange
		// rectangle so the content does not render beyond it.
		var rectangleGeometry = Clip as RectangleGeometry;

		if (rectangleGeometry is null)
		{
			// Ensure that this ScrollPresenter has a rectangular clip.
			RectangleGeometry newRectangleGeometry = new();
			Clip = newRectangleGeometry;

			rectangleGeometry = newRectangleGeometry;
		}

		Rect newClipRect = new(0.0f, 0.0f, viewport.Width, viewport.Height);
		rectangleGeometry.Rect = newClipRect;

		UpdateUnzoomedExtentAndViewport(
			renderSizeChanged,
			newUnzoomedExtentWidth  /*unzoomedExtentWidth*/,
			newUnzoomedExtentHeight /*unzoomedExtentHeight*/,
			viewport.Width          /*viewportWidth*/,
			viewport.Height         /*viewportHeight*/);

		m_isAnchorElementDirty = true;
		return viewport;
	}

	#endregion

	#region IInteractionTrackerOwner

	internal void CustomAnimationStateEntered(
		InteractionTrackerCustomAnimationStateEnteredArgs args)
	{
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_INT, METH_NAME, this, args.RequestId());

		UpdateState(ScrollingInteractionState.Animation);
	}

	internal void IdleStateEntered(
		InteractionTrackerIdleStateEnteredArgs args)
	{
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_INT, METH_NAME, this, args.RequestId());

		UpdateState(ScrollingInteractionState.Idle);

		if (m_interactionTrackerAsyncOperations.Count > 0)
		{
			int requestId = args.RequestId;

			// Complete all operations recorded through ChangeOffsetsPrivate/ChangeOffsetsWithAdditionalVelocityPrivate
			// and ChangeZoomFactorPrivate/ChangeZoomFactorWithAdditionalVelocityPrivate calls.
			if (requestId != 0)
			{
				CompleteInteractionTrackerOperations(
					requestId,
					ScrollPresenterViewChangeResult.Completed   /*operationResult*/,
					ScrollPresenterViewChangeResult.Completed   /*priorNonAnimatedOperationsResult*/,
					ScrollPresenterViewChangeResult.Interrupted /*priorAnimatedOperationsResult*/,
					true  /*completeNonAnimatedOperation*/,
					true  /*completeAnimatedOperation*/,
					true  /*completePriorNonAnimatedOperations*/,
					true  /*completePriorAnimatedOperations*/);
			}
		}

		// Check if resting position corresponds to a non-unique mandatory snap point, for the three dimensions
		// UNO TODO: Should this be passed by ref?
		UpdateSnapPointsIgnoredValue(ref m_sortedConsolidatedHorizontalSnapPoints, ScrollPresenterDimension.HorizontalScroll);
		UpdateSnapPointsIgnoredValue(ref m_sortedConsolidatedVerticalSnapPoints, ScrollPresenterDimension.VerticalScroll);
		UpdateSnapPointsIgnoredValue(ref m_sortedConsolidatedZoomSnapPoints, ScrollPresenterDimension.ZoomFactor);

		// Stop Translation and Scale animations if needed, to trigger rasterization of Content & avoid fuzzy text rendering for instance.
		StopTranslationAndZoomFactorExpressionAnimations();
	}

	internal void InertiaStateEntered(
		InteractionTrackerInertiaStateEnteredArgs args)
	{
		Vector3? modifiedRestingPosition = args.ModifiedRestingPosition;
		Vector3 naturalRestingPosition = args.NaturalRestingPosition;
		float? modifiedRestingScale = args.ModifiedRestingScale;
		float naturalRestingScale = args.NaturalRestingScale;
		// bool isTracingEnabled = IsScrollPresenterTracingEnabled() || ScrollPresenterTrace.s_IsDebugOutputEnabled || ScrollPresenterTrace.s_IsVerboseDebugOutputEnabled;
		InteractionTrackerAsyncOperation interactionTrackerAsyncOperation = GetInteractionTrackerOperationFromRequestId(args.RequestId);

		// if (isTracingEnabled)
		// {
		// 	SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_PTR_INT, METH_NAME, this, interactionTrackerAsyncOperation, args.RequestId());

		// 	const float3 positionVelocity = args.PositionVelocityInPixelsPerSecond();
		// 	const float scaleVelocity = args.ScaleVelocityInPercentPerSecond();

		// 	SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_FLT, METH_NAME, this,
		// 		TypeLogging::Float2ToString(float2{ positionVelocity.x, positionVelocity.y }).c_str(),
		// 		scaleVelocity);

		// 	SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_FLT, METH_NAME, this,
		// 		TypeLogging::Float2ToString(float2{ naturalRestingPosition.x, naturalRestingPosition.y }).c_str(),
		// 		naturalRestingScale);

		// 	if (modifiedRestingPosition)
		// 	{
		// 		const float3 endOfInertiaPosition = modifiedRestingPosition.Value();
		// 		SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR, METH_NAME, this,
		// 			TypeLogging::Float2ToString(float2{ endOfInertiaPosition.x, endOfInertiaPosition.y }).c_str());
		// 	}

		// 	if (modifiedRestingScale)
		// 	{
		// 		SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_FLT, METH_NAME, this,
		// 			modifiedRestingScale.Value());
		// 	}

		// 	SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_INT, METH_NAME, this, args.IsInertiaFromImpulse());
		// 	SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_INT, METH_NAME, this, args.IsFromBinding());
		// }

		// Record the end-of-inertia view for this inertial phase. It may be needed for
		// custom pointer wheel processing.

		if (modifiedRestingPosition.HasValue)
		{
			Vector3 endOfInertiaPosition = modifiedRestingPosition.Value;
			m_endOfInertiaPosition = new Vector2(endOfInertiaPosition.X, endOfInertiaPosition.Y);
		}
		else
		{
			m_endOfInertiaPosition = new Vector2(naturalRestingPosition.X, naturalRestingPosition.Y);
		}

		if (modifiedRestingScale.HasValue)
		{
			m_endOfInertiaZoomFactor = modifiedRestingScale.Value;
		}
		else
		{
			m_endOfInertiaZoomFactor = naturalRestingScale;
		}

		// if (isTracingEnabled)
		// {
		// 	SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_FLT, METH_NAME, this,
		// 		TypeLogging::Float2ToString(m_endOfInertiaPosition).c_str(),
		// 		m_endOfInertiaZoomFactor);
		// }

		if (interactionTrackerAsyncOperation is not null)
		{
			ViewChangeBase viewChangeBase = interactionTrackerAsyncOperation.GetViewChangeBase();

			if (viewChangeBase is not null && interactionTrackerAsyncOperation.GetOperationType() == InteractionTrackerAsyncOperationType.TryUpdatePositionWithAdditionalVelocity)
			{
				OffsetsChangeWithAdditionalVelocity offsetsChangeWithAdditionalVelocity = viewChangeBase as OffsetsChangeWithAdditionalVelocity;

				if (offsetsChangeWithAdditionalVelocity is not null)
				{
					offsetsChangeWithAdditionalVelocity.AnticipatedOffsetsChange = Vector2.Zero;
				}
			}
		}

		UpdateState(ScrollingInteractionState.Inertia);
	}

	internal void InteractingStateEntered(
		InteractionTrackerInteractingStateEnteredArgs args)
	{
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_INT, METH_NAME, this, args.RequestId());

		UpdateState(ScrollingInteractionState.Interaction);

		if (m_interactionTrackerAsyncOperations.Count > 0)
		{
			// Complete all operations recorded through ChangeOffsetsPrivate/ChangeOffsetsWithAdditionalVelocityPrivate
			// and ChangeZoomFactorPrivate/ChangeZoomFactorWithAdditionalVelocityPrivate calls.
			CompleteInteractionTrackerOperations(
				-1 /*requestId*/,
				ScrollPresenterViewChangeResult.Interrupted /*operationResult*/,
				ScrollPresenterViewChangeResult.Completed   /*priorNonAnimatedOperationsResult*/,
				ScrollPresenterViewChangeResult.Interrupted /*priorAnimatedOperationsResult*/,
				true  /*completeNonAnimatedOperation*/,
				true  /*completeAnimatedOperation*/,
				true  /*completePriorNonAnimatedOperations*/,
				true  /*completePriorAnimatedOperations*/);
		}
	}

	internal void RequestIgnored(
		InteractionTrackerRequestIgnoredArgs args)
	{
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_INT, METH_NAME, this, args.RequestId());

		if (m_interactionTrackerAsyncOperations.Count > 0)
		{
			// Complete this request alone.
			CompleteInteractionTrackerOperations(
				args.RequestId,
				ScrollPresenterViewChangeResult.Ignored /*operationResult*/,
				ScrollPresenterViewChangeResult.Ignored /*unused priorNonAnimatedOperationsResult*/,
				ScrollPresenterViewChangeResult.Ignored /*unused priorAnimatedOperationsResult*/,
				true  /*completeNonAnimatedOperation*/,
				true  /*completeAnimatedOperation*/,
				false /*completePriorNonAnimatedOperations*/,
				false /*completePriorAnimatedOperations*/);
		}
	}

	internal void ValuesChanged(
		InteractionTrackerValuesChangedArgs args)
	{
		// bool isScrollPresenterTracingEnabled = IsScrollPresenterTracingEnabled();

		// #if DEBUG
		// 		if (isScrollPresenterTracingEnabled || ScrollPresenterTrace.s_IsDebugOutputEnabled || ScrollPresenterTrace.s_IsVerboseDebugOutputEnabled)
		// 		{
		// 			SCROLLPRESENTER_TRACE_INFO_ENABLED(isScrollPresenterTracingEnabled /*includeTraceLogging*/, *this, L"%s[0x%p](RequestId: %d, View: %f, %f, %f)\n",
		// 				METH_NAME, this, args.RequestId(), args.Position().x, args.Position().y, args.Scale);
		// 		}
		// #endif

		int requestId = args.RequestId;

		InteractionTrackerAsyncOperation interactionTrackerAsyncOperation = GetInteractionTrackerOperationFromRequestId(requestId);

		double oldZoomedHorizontalOffset = m_zoomedHorizontalOffset;
		double oldZoomedVerticalOffset = m_zoomedVerticalOffset;
		float oldZoomFactor = m_zoomFactor;
		Vector2 minPosition = default;

		m_zoomFactor = args.Scale;

		ComputeMinMaxPositions(m_zoomFactor, out minPosition, out _);

		UpdateOffset(ScrollPresenterDimension.HorizontalScroll, args.Position.X - minPosition.X);
		UpdateOffset(ScrollPresenterDimension.VerticalScroll, args.Position.Y - minPosition.Y);

		if (oldZoomFactor != m_zoomFactor || oldZoomedHorizontalOffset != m_zoomedHorizontalOffset || oldZoomedVerticalOffset != m_zoomedVerticalOffset)
		{
			OnViewChanged(oldZoomedHorizontalOffset != m_zoomedHorizontalOffset /*horizontalOffsetChanged*/,
				oldZoomedVerticalOffset != m_zoomedVerticalOffset /*verticalOffsetChanged*/);
		}

		if (requestId != 0 && m_interactionTrackerAsyncOperations.Count > 0)
		{
			CompleteInteractionTrackerOperations(
				requestId,
				ScrollPresenterViewChangeResult.Completed   /*operationResult*/,
				ScrollPresenterViewChangeResult.Completed   /*priorNonAnimatedOperationsResult*/,
				ScrollPresenterViewChangeResult.Interrupted /*priorAnimatedOperationsResult*/,
				true  /*completeNonAnimatedOperation*/,
				false /*completeAnimatedOperation*/,
				true  /*completePriorNonAnimatedOperations*/,
				true  /*completePriorAnimatedOperations*/);
		}
	}

	#endregion

	// Returns the size used to arrange the provided ScrollPresenter content.
	private Size ArrangeContent(
		UIElement content,
		Thickness contentMargin,
		Rect finalContentRect,
		bool wasContentArrangeWidthStretched,
		bool wasContentArrangeHeightStretched)
	{
		MUX_ASSERT(content is not null);

		Size contentArrangeSize = new(
			finalContentRect.Width,
			finalContentRect.Height
		);

		content.Arrange(finalContentRect);

		// #if DEBUG
		// 		SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_STR, METH_NAME, this, L"content Arrange", TypeLogging::RectToString(finalContentRect).c_str());
		// 		SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"wasContentArrangeWidthStretched", wasContentArrangeWidthStretched);
		// 		SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"wasContentArrangeHeightStretched", wasContentArrangeHeightStretched);
		// 		SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_FLT_FLT, METH_NAME, this, L"content RenderSize", content.RenderSize().Width, content.RenderSize().Height);
		// #endif

		if (wasContentArrangeWidthStretched || wasContentArrangeHeightStretched)
		{
			bool reArrangeNeeded = false;
			var renderWidth = content.RenderSize.Width;
			var renderHeight = content.RenderSize.Height;
			var marginWidth = (float)(contentMargin.Left + contentMargin.Right);
			var marginHeight = (float)(contentMargin.Top + contentMargin.Bottom);
			var scaleFactorRounding = 0.5f / (float)(XamlRoot.RasterizationScale);

			if (wasContentArrangeWidthStretched &&
				renderWidth > 0.0f &&
				renderWidth + marginWidth < finalContentRect.Width * (1.0f - float.Epsilon) - scaleFactorRounding)
			{
				// Content stretched partially horizontally.
				contentArrangeSize.Width = finalContentRect.Width = renderWidth + marginWidth;
				reArrangeNeeded = true;
			}

			if (wasContentArrangeHeightStretched &&
				renderHeight > 0.0f &&
				renderHeight + marginHeight < finalContentRect.Height * (1.0f - float.Epsilon) - scaleFactorRounding)
			{
				// Content stretched partially vertically.
				contentArrangeSize.Height = finalContentRect.Height = renderHeight + marginHeight;
				reArrangeNeeded = true;
			}

			if (reArrangeNeeded)
			{
				// Re-arrange the content using the partially stretched size.
#if DEBUG
				// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_STR, METH_NAME, this, L"content re-Arrange", TypeLogging::RectToString(finalContentRect).c_str());
#endif
				content.Arrange(finalContentRect);
			}
		}

		return contentArrangeSize;
	}

	// Used to perform a flickerless change to the Content's XAML Layout Offset. The InteractionTracker's Position is unaffected, but its Min/MaxPosition expressions
	// and the ScrollPresenter HorizontalOffset/VerticalOffset property are updated accordingly once the change is incorporated into the XAML layout engine.
	private float ComputeContentLayoutOffsetDelta(ScrollPresenterDimension dimension, float unzoomedDelta)
	{
		MUX_ASSERT(dimension == ScrollPresenterDimension.HorizontalScroll || dimension == ScrollPresenterDimension.VerticalScroll);

		float zoomedDelta = unzoomedDelta * m_zoomFactor;

#if DEBUG
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR, METH_NAME, this, dimension == ScrollPresenterDimension.HorizontalScroll ? L"HorizontalScroll" : L"VerticalScroll");
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_FLT, METH_NAME, this, L"zoomedDelta", zoomedDelta);
#endif

		if (dimension == ScrollPresenterDimension.HorizontalScroll)
		{
#if DEBUG
			// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_FLT, METH_NAME, this, L"m_zoomedHorizontalOffset", m_zoomedHorizontalOffset);
#endif

			if (zoomedDelta < 0.0f && -zoomedDelta > m_zoomedHorizontalOffset)
			{
				// Do not let m_zoomedHorizontalOffset step into negative territory.
				zoomedDelta = (float)(-m_zoomedHorizontalOffset);
			}
		}
		else
		{
#if DEBUG
			// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_FLT, METH_NAME, this, L"m_zoomedVerticalOffset", m_zoomedVerticalOffset);
#endif

			if (zoomedDelta < 0.0f && -zoomedDelta > m_zoomedVerticalOffset)
			{
				// Do not let m_zoomedVerticalOffset step into negative territory.
				zoomedDelta = (float)(-m_zoomedVerticalOffset);
			}
		}

#if DEBUG
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_FLT, METH_NAME, this, L"returned value", -zoomedDelta);
#endif

		return -zoomedDelta;
	}

#if false // UNO TODO
	private float ComputeEndOfInertiaZoomFactor()
	{
		if (m_state == ScrollingInteractionState.Inertia)
		{
			return Math.Clamp(m_endOfInertiaZoomFactor, m_interactionTracker.MinScale, m_interactionTracker.MaxScale);
		}
		else
		{
			return m_zoomFactor;
		}
	}

	private Vector2 ComputeEndOfInertiaPosition()
	{
		if (m_state == ScrollingInteractionState.Inertia)
		{
			float endOfInertiaZoomFactor = ComputeEndOfInertiaZoomFactor();
			Vector2 minPosition = default;
			Vector2 maxPosition = default;
			Vector2 endOfInertiaPosition = m_endOfInertiaPosition;

			ComputeMinMaxPositions(endOfInertiaZoomFactor, out minPosition, out maxPosition);

			endOfInertiaPosition = Vector2.Max(endOfInertiaPosition, minPosition);
			endOfInertiaPosition = Vector2.Min(endOfInertiaPosition, maxPosition);

			return endOfInertiaPosition;
		}
		else
		{
			return ComputePositionFromOffsets(m_zoomedHorizontalOffset, m_zoomedVerticalOffset);
		}
	}
#endif

	// Returns zoomed vectors corresponding to InteractionTracker.MinPosition and InteractionTracker.MaxPosition
	// Determines the min and max positions of the ScrollPresenter.Content based on its size and alignment, and the ScrollPresenter size.
	private void ComputeMinMaxPositions(float zoomFactor, out Vector2 minPosition, out Vector2 maxPosition)
	{
		//MUX_ASSERT(minPosition || maxPosition);

		minPosition = Vector2.Zero;

		maxPosition = Vector2.Zero;

		UIElement content = Content;

		if (content is null)
		{
			return;
		}

		FrameworkElement contentAsFE = content as FrameworkElement;

		if (contentAsFE is null)
		{
			return;
		}

		Visual scrollPresenterVisual = ElementCompositionPreview.GetElementVisual(this);
		float minPosX = 0.0f;
		float minPosY = 0.0f;
		float maxPosX = 0.0f;
		float maxPosY = 0.0f;
		float extentWidth = (float)m_unzoomedExtentWidth;
		float extentHeight = (float)m_unzoomedExtentHeight;

		if (contentAsFE.HorizontalAlignment == HorizontalAlignment.Center ||
			contentAsFE.HorizontalAlignment == HorizontalAlignment.Stretch)
		{
			float scrollableWidth = extentWidth * zoomFactor - scrollPresenterVisual.Size.X;

			// When the zoomed content is smaller than the viewport, scrollableWidth < 0, minPosX is scrollableWidth / 2 so it is centered at idle.
			// When the zoomed content is larger than the viewport, scrollableWidth > 0, minPosX is 0.
			minPosX = Math.Min(0.0f, scrollableWidth / 2.0f);

			// When the zoomed content is smaller than the viewport, scrollableWidth < 0, maxPosX is scrollableWidth / 2 so it is centered at idle.
			// When the zoomed content is larger than the viewport, scrollableWidth > 0, maxPosX is scrollableWidth.
			maxPosX = scrollableWidth;
			if (maxPosX < 0.0f)
			{
				maxPosX /= 2.0f;
			}
		}
		else if (contentAsFE.HorizontalAlignment == HorizontalAlignment.Right)
		{
			float scrollableWidth = extentWidth * zoomFactor - scrollPresenterVisual.Size.X;

			// When the zoomed content is smaller than the viewport, scrollableWidth < 0, minPosX is scrollableWidth so it is right-aligned at idle.
			// When the zoomed content is larger than the viewport, scrollableWidth > 0, minPosX is 0.
			minPosX = Math.Min(0.0f, scrollableWidth);

			// When the zoomed content is smaller than the viewport, scrollableWidth < 0, maxPosX is -scrollableWidth so it is right-aligned at idle.
			// When the zoomed content is larger than the viewport, scrollableWidth > 0, maxPosX is scrollableWidth.
			maxPosX = scrollableWidth;
			if (maxPosX < 0.0f)
			{
				maxPosX *= -1.0f;
			}
		}

		if (contentAsFE.VerticalAlignment == VerticalAlignment.Center ||
			contentAsFE.VerticalAlignment == VerticalAlignment.Stretch)
		{
			float scrollableHeight = extentHeight * zoomFactor - scrollPresenterVisual.Size.Y;

			// When the zoomed content is smaller than the viewport, scrollableHeight < 0, minPosY is scrollableHeight / 2 so it is centered at idle.
			// When the zoomed content is larger than the viewport, scrollableHeight > 0, minPosY is 0.
			minPosY = Math.Min(0.0f, scrollableHeight / 2.0f);

			// When the zoomed content is smaller than the viewport, scrollableHeight < 0, maxPosY is scrollableHeight / 2 so it is centered at idle.
			// When the zoomed content is larger than the viewport, scrollableHeight > 0, maxPosY is scrollableHeight.
			maxPosY = scrollableHeight;
			if (maxPosY < 0.0f)
			{
				maxPosY /= 2.0f;
			}
		}
		else if (contentAsFE.VerticalAlignment == VerticalAlignment.Bottom)
		{
			float scrollableHeight = extentHeight * zoomFactor - scrollPresenterVisual.Size.Y;

			// When the zoomed content is smaller than the viewport, scrollableHeight < 0, minPosY is scrollableHeight so it is bottom-aligned at idle.
			// When the zoomed content is larger than the viewport, scrollableHeight > 0, minPosY is 0.
			minPosY = Math.Min(0.0f, scrollableHeight);

			// When the zoomed content is smaller than the viewport, scrollableHeight < 0, maxPosY is -scrollableHeight so it is bottom-aligned at idle.
			// When the zoomed content is larger than the viewport, scrollableHeight > 0, maxPosY is scrollableHeight.
			maxPosY = scrollableHeight;
			if (maxPosY < 0.0f)
			{
				maxPosY *= -1.0f;
			}
		}

		minPosition = new Vector2(minPosX + m_contentLayoutOffsetX, minPosY + m_contentLayoutOffsetY);

		maxPosition = new Vector2(maxPosX + m_contentLayoutOffsetX, maxPosY + m_contentLayoutOffsetY);
	}

	// Returns an InteractionTracker Position based on the provided offsets
	private Vector2 ComputePositionFromOffsets(double zoomedHorizontalOffset, double zoomedVerticalOffset)
	{
		Vector2 minPosition = default;

		ComputeMinMaxPositions(m_zoomFactor, out minPosition, out _);

		return new Vector2((float)(zoomedHorizontalOffset + minPosition.X), (float)(zoomedVerticalOffset + minPosition.Y));
	}

	// Evaluate what the value will be once the snap points have been applied.
	private double ComputeValueAfterSnapPoints<T>(
		double value,
		SortedSet<SnapPointWrapper<T>> snapPointsSet) where T : SnapPointBase
	{
		foreach (SnapPointWrapper<T> snapPointWrapper in snapPointsSet)
		{
			if (snapPointWrapper.ActualApplicableZone().Item1 <= value &&
				snapPointWrapper.ActualApplicableZone().Item2 >= value)
			{
				return snapPointWrapper.Evaluate((float)value);
			}
		}
		return value;
	}

	// Called by ScrollPresenter::OnBringIntoViewRequestedHandler to compute the target bring-into-view offsets
	// based on the provided BringIntoViewRequestedEventArgs instance.
	// The resulting offsets are later updated by ScrollPresenter::ComputeBringIntoViewUpdatedTargetOffsets below.
	private void ComputeBringIntoViewTargetOffsetsFromRequestEventArgs(
		UIElement content,
		ScrollingSnapPointsMode snapPointsMode,
		BringIntoViewRequestedEventArgs requestEventArgs,
		out double targetZoomedHorizontalOffset,
		out double targetZoomedVerticalOffset,
		out double appliedOffsetX,
		out double appliedOffsetY,
		out Rect targetRect)
	{
#if DEBUG
		// SCROLLPRESENTER_TRACE_INFO(*this, L"%s[0x%p](H/V AlignmentRatio:%lf,%lf, H/V Offset:%f,%f, ElementRect:%s, Element:0x%p)\n",
		// 	METH_NAME, this,
		// 	requestEventArgs.HorizontalAlignmentRatio(), requestEventArgs.VerticalAlignmentRatio(),
		// 	requestEventArgs.HorizontalOffset(), requestEventArgs.VerticalOffset(),
		// 	TypeLogging::RectToString(requestEventArgs.TargetRect()).c_str(), requestEventArgs.TargetElement());
#endif

		ComputeBringIntoViewTargetOffsets(
			content,
			requestEventArgs.TargetElement,
			requestEventArgs.TargetRect,
			snapPointsMode,
			requestEventArgs.HorizontalAlignmentRatio,
			requestEventArgs.VerticalAlignmentRatio,
			requestEventArgs.HorizontalOffset,
			requestEventArgs.VerticalOffset,
			out targetZoomedHorizontalOffset,
			out targetZoomedVerticalOffset,
			out appliedOffsetX,
			out appliedOffsetY,
			out targetRect);
	}

	// Called by ScrollPresenter::ProcessOffsetsChange to potentially update the target bring-into-view offsets
	// just before invoking the InteractionTracker's TryUpdatePosition.
	private void ComputeBringIntoViewUpdatedTargetOffsets(
		UIElement content,
		UIElement element,
		Rect elementRect,
		ScrollingSnapPointsMode snapPointsMode,
		double horizontalAlignmentRatio,
		double verticalAlignmentRatio,
		double horizontalOffset,
		double verticalOffset,
		out double targetZoomedHorizontalOffset,
		out double targetZoomedVerticalOffset)
	{
#if DEBUG
		// SCROLLPRESENTER_TRACE_INFO(*this, L"%s[0x%p](H/V AlignmentRatio:%lf,%lf, H/V Offset:%f,%f, ElementRect:%s, Element:0x%p)\n",
		// 	METH_NAME, this,
		// 	horizontalAlignmentRatio, verticalAlignmentRatio,
		// 	horizontalOffset, verticalOffset,
		// 	TypeLogging::RectToString(elementRect).c_str(), element);
#endif

		ComputeBringIntoViewTargetOffsets(
			content,
			element,
			elementRect,
			snapPointsMode,
			horizontalAlignmentRatio,
			verticalAlignmentRatio,
			horizontalOffset,
			verticalOffset,
			out targetZoomedHorizontalOffset,
			out targetZoomedVerticalOffset,
			out _ /*appliedOffsetX*/,
			out _ /*appliedOffsetY*/,
			out _ /*targetRect*/);
	}

	private void ComputeBringIntoViewTargetOffsets(
		UIElement content,
		UIElement element,
		Rect elementRect,
		ScrollingSnapPointsMode snapPointsMode,
		double horizontalAlignmentRatio,
		double verticalAlignmentRatio,
		double horizontalOffset,
		double verticalOffset,
		out double targetZoomedHorizontalOffset,
		out double targetZoomedVerticalOffset,
		out double appliedOffsetX,
		out double appliedOffsetY,
		out Rect targetRect)
	{
		MUX_ASSERT(content is not null);
		MUX_ASSERT(element is not null);

		targetZoomedHorizontalOffset = 0.0;
		targetZoomedVerticalOffset = 0.0;

		appliedOffsetX = 0.0;

		appliedOffsetY = 0.0;

		targetRect = default;

		Rect transformedRect = GetDescendantBounds(content, element, elementRect);

		double targetX = transformedRect.X;
		double targetWidth = transformedRect.Width;
		double targetY = transformedRect.Y;
		double targetHeight = transformedRect.Height;

		if (!double.IsNaN(horizontalAlignmentRatio))
		{
			// Account for the horizontal alignment ratio
			MUX_ASSERT(horizontalAlignmentRatio >= 0.0 && horizontalAlignmentRatio <= 1.0);

			targetX += (targetWidth - m_viewportWidth / m_zoomFactor) * horizontalAlignmentRatio;
			targetWidth = m_viewportWidth / m_zoomFactor;
		}

		if (!double.IsNaN(verticalAlignmentRatio))
		{
			// Account for the vertical alignment ratio
			MUX_ASSERT(verticalAlignmentRatio >= 0.0 && verticalAlignmentRatio <= 1.0);

			targetY += (targetHeight - m_viewportHeight / m_zoomFactor) * verticalAlignmentRatio;
			targetHeight = m_viewportHeight / m_zoomFactor;
		}

		double targetZoomedHorizontalOffsetTmp = ComputeZoomedOffsetWithMinimalChange(
			m_zoomedHorizontalOffset,
			m_zoomedHorizontalOffset + m_viewportWidth,
			targetX * m_zoomFactor,
			(targetX + targetWidth) * m_zoomFactor);
		double targetZoomedVerticalOffsetTmp = ComputeZoomedOffsetWithMinimalChange(
			m_zoomedVerticalOffset,
			m_zoomedVerticalOffset + m_viewportHeight,
			targetY * m_zoomFactor,
			(targetY + targetHeight) * m_zoomFactor);

		double scrollableWidth = ScrollableWidth;
		double scrollableHeight = ScrollableHeight;

		targetZoomedHorizontalOffsetTmp = Math.Clamp(targetZoomedHorizontalOffsetTmp, 0.0, scrollableWidth);
		targetZoomedVerticalOffsetTmp = Math.Clamp(targetZoomedVerticalOffsetTmp, 0.0, scrollableHeight);

		double offsetX = horizontalOffset;
		double offsetY = verticalOffset;
		double appliedOffsetXTmp = 0.0;
		double appliedOffsetYTmp = 0.0;

		// If the target offset is within bounds and an offset was provided, apply as much of it as possible while remaining within bounds.
		if (offsetX != 0.0 && targetZoomedHorizontalOffsetTmp >= 0.0)
		{
			if (targetZoomedHorizontalOffsetTmp <= scrollableWidth)
			{
				if (offsetX > 0.0)
				{
					appliedOffsetXTmp = Math.Min(targetZoomedHorizontalOffsetTmp, offsetX);
				}
				else
				{
					appliedOffsetXTmp = -Math.Min(scrollableWidth - targetZoomedHorizontalOffsetTmp, -offsetX);
				}
				targetZoomedHorizontalOffsetTmp -= appliedOffsetXTmp;
			}
		}

		if (offsetY != 0.0 && targetZoomedVerticalOffsetTmp >= 0.0)
		{
			if (targetZoomedVerticalOffsetTmp <= scrollableHeight)
			{
				if (offsetY > 0.0)
				{
					appliedOffsetYTmp = Math.Min(targetZoomedVerticalOffsetTmp, offsetY);
				}
				else
				{
					appliedOffsetYTmp = -Math.Min(scrollableHeight - targetZoomedVerticalOffsetTmp, -offsetY);
				}
				targetZoomedVerticalOffsetTmp -= appliedOffsetYTmp;
			}
		}

		MUX_ASSERT(targetZoomedHorizontalOffsetTmp >= 0.0);
		MUX_ASSERT(targetZoomedVerticalOffsetTmp >= 0.0);
		MUX_ASSERT(targetZoomedHorizontalOffsetTmp <= scrollableWidth);
		MUX_ASSERT(targetZoomedVerticalOffsetTmp <= scrollableHeight);

		if (snapPointsMode == ScrollingSnapPointsMode.Default)
		{
			// Finally adjust the target offsets based on snap points
			targetZoomedHorizontalOffsetTmp = ComputeValueAfterSnapPoints<ScrollSnapPointBase>(
				targetZoomedHorizontalOffsetTmp, m_sortedConsolidatedHorizontalSnapPoints);
			targetZoomedVerticalOffsetTmp = ComputeValueAfterSnapPoints<ScrollSnapPointBase>(
				targetZoomedVerticalOffsetTmp, m_sortedConsolidatedVerticalSnapPoints);

			// Make sure the target offsets are within the scrollable boundaries
			targetZoomedHorizontalOffsetTmp = Math.Clamp(targetZoomedHorizontalOffsetTmp, 0.0, scrollableWidth);
			targetZoomedVerticalOffsetTmp = Math.Clamp(targetZoomedVerticalOffsetTmp, 0.0, scrollableHeight);

			MUX_ASSERT(targetZoomedHorizontalOffsetTmp >= 0.0);
			MUX_ASSERT(targetZoomedVerticalOffsetTmp >= 0.0);
			MUX_ASSERT(targetZoomedHorizontalOffsetTmp <= scrollableWidth);
			MUX_ASSERT(targetZoomedVerticalOffsetTmp <= scrollableHeight);
		}

#if DEBUG
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, L"targetZoomedHorizontalOffset", targetZoomedHorizontalOffsetTmp);
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, L"targetZoomedVerticalOffset", targetZoomedVerticalOffsetTmp);
#endif

		targetZoomedHorizontalOffset = targetZoomedHorizontalOffsetTmp;
		targetZoomedVerticalOffset = targetZoomedVerticalOffsetTmp;

		appliedOffsetX = appliedOffsetXTmp;

		appliedOffsetY = appliedOffsetYTmp;

		targetRect = new Rect(
			(float)targetX,
			(float)targetY,
			(float)targetWidth,
			(float)targetHeight
		);
	}

	private void EnsureExpressionAnimationSources()
	{
		if (m_expressionAnimationSources is null)
		{
			// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

			Compositor compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;

			m_expressionAnimationSources = compositor.CreatePropertySet();
			m_expressionAnimationSources.InsertVector2(s_extentSourcePropertyName, new Vector2(0.0f, 0.0f));
			m_expressionAnimationSources.InsertVector2(s_viewportSourcePropertyName, new Vector2(0.0f, 0.0f));
			m_expressionAnimationSources.InsertVector2(s_offsetSourcePropertyName, new Vector2(m_contentLayoutOffsetX, m_contentLayoutOffsetY));
			m_expressionAnimationSources.InsertVector2(s_positionSourcePropertyName, new Vector2(0.0f, 0.0f));
			m_expressionAnimationSources.InsertVector2(s_minPositionSourcePropertyName, new Vector2(0.0f, 0.0f));
			m_expressionAnimationSources.InsertVector2(s_maxPositionSourcePropertyName, new Vector2(0.0f, 0.0f));
			m_expressionAnimationSources.InsertScalar(s_zoomFactorSourcePropertyName, 0.0f);

			MUX_ASSERT(m_interactionTracker is not null);
			MUX_ASSERT(m_positionSourceExpressionAnimation is null);
			MUX_ASSERT(m_minPositionSourceExpressionAnimation is null);
			MUX_ASSERT(m_maxPositionSourceExpressionAnimation is null);
			MUX_ASSERT(m_zoomFactorSourceExpressionAnimation is null);

			m_positionSourceExpressionAnimation = compositor.CreateExpressionAnimation("Vector2(it.Position.X, it.Position.Y)");
			m_positionSourceExpressionAnimation.SetReferenceParameter("it", m_interactionTracker);

			m_minPositionSourceExpressionAnimation = compositor.CreateExpressionAnimation("Vector2(it.MinPosition.X, it.MinPosition.Y)");
			m_minPositionSourceExpressionAnimation.SetReferenceParameter("it", m_interactionTracker);

			m_maxPositionSourceExpressionAnimation = compositor.CreateExpressionAnimation("Vector2(it.MaxPosition.X, it.MaxPosition.Y)");
			m_maxPositionSourceExpressionAnimation.SetReferenceParameter("it", m_interactionTracker);

			m_zoomFactorSourceExpressionAnimation = compositor.CreateExpressionAnimation("it.Scale");
			m_zoomFactorSourceExpressionAnimation.SetReferenceParameter("it", m_interactionTracker);

			StartExpressionAnimationSourcesAnimations();
			UpdateExpressionAnimationSources();
		}
	}

	private void EnsureInteractionTracker()
	{
		if (m_interactionTracker is null)
		{
			// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

			MUX_ASSERT(m_interactionTrackerOwner is null);
			m_interactionTrackerOwner = new InteractionTrackerOwner(this) as IInteractionTrackerOwner;

			Compositor compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
			m_interactionTracker = InteractionTracker.CreateWithOwner(compositor, m_interactionTrackerOwner);
		}
	}

	private void EnsureScrollPresenterVisualInteractionSource()
	{
		if (m_scrollPresenterVisualInteractionSource is null)
		{
			// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

			EnsureInteractionTracker();

			Visual scrollPresenterVisual = ElementCompositionPreview.GetElementVisual(this);
			VisualInteractionSource scrollPresenterVisualInteractionSource = VisualInteractionSource.Create(scrollPresenterVisual);
			m_interactionTracker.InteractionSources.Add(scrollPresenterVisualInteractionSource);
			m_scrollPresenterVisualInteractionSource = scrollPresenterVisualInteractionSource;
			UpdateManipulationRedirectionMode();
			RaiseInteractionSourcesChanged();
		}
	}

	private void EnsureScrollControllerVisualInteractionSource(
		Visual panningElementAncestorVisual,
		ScrollPresenterDimension dimension)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_PTR_INT, METH_NAME, this, panningElementAncestorVisual, dimension);

		MUX_ASSERT(dimension == ScrollPresenterDimension.HorizontalScroll || dimension == ScrollPresenterDimension.VerticalScroll);
		MUX_ASSERT(m_interactionTracker is not null);

		VisualInteractionSource scrollControllerVisualInteractionSource = VisualInteractionSource.Create(panningElementAncestorVisual);
		scrollControllerVisualInteractionSource.ManipulationRedirectionMode = VisualInteractionSourceRedirectionMode.CapableTouchpadOnly;
		scrollControllerVisualInteractionSource.PositionXChainingMode = InteractionChainingMode.Never;
		scrollControllerVisualInteractionSource.PositionYChainingMode = InteractionChainingMode.Never;
		scrollControllerVisualInteractionSource.ScaleChainingMode = InteractionChainingMode.Never;
		scrollControllerVisualInteractionSource.ScaleSourceMode = InteractionSourceMode.Disabled;
		m_interactionTracker.InteractionSources.Add(scrollControllerVisualInteractionSource);

		if (dimension == ScrollPresenterDimension.HorizontalScroll)
		{
			MUX_ASSERT(m_horizontalScrollController is not null);
			MUX_ASSERT(m_horizontalScrollControllerPanningInfo is not null);
			MUX_ASSERT(m_horizontalScrollControllerVisualInteractionSource is null);
			m_horizontalScrollControllerVisualInteractionSource = scrollControllerVisualInteractionSource;

			HookHorizontalScrollControllerInteractionSourceEvents(m_horizontalScrollControllerPanningInfo);
		}
		else
		{
			MUX_ASSERT(m_verticalScrollController is not null);
			MUX_ASSERT(m_verticalScrollControllerPanningInfo is not null);
			MUX_ASSERT(m_verticalScrollControllerVisualInteractionSource is null);
			m_verticalScrollControllerVisualInteractionSource = scrollControllerVisualInteractionSource;

			HookVerticalScrollControllerInteractionSourceEvents(m_verticalScrollControllerPanningInfo);
		}

		RaiseInteractionSourcesChanged();
	}

	private void EnsureScrollControllerExpressionAnimationSources(
		ScrollPresenterDimension dimension)
	{
		MUX_ASSERT(dimension == ScrollPresenterDimension.HorizontalScroll || dimension == ScrollPresenterDimension.VerticalScroll);
		MUX_ASSERT(m_interactionTracker is not null);

		Compositor compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
		CompositionPropertySet scrollControllerExpressionAnimationSources = null;

		if (dimension == ScrollPresenterDimension.HorizontalScroll)
		{
			if (m_horizontalScrollControllerExpressionAnimationSources is not null)
			{
				return;
			}

			m_horizontalScrollControllerExpressionAnimationSources = scrollControllerExpressionAnimationSources = compositor.CreatePropertySet();
		}
		else
		{
			if (m_verticalScrollControllerExpressionAnimationSources is not null)
			{
				return;
			}

			m_verticalScrollControllerExpressionAnimationSources = scrollControllerExpressionAnimationSources = compositor.CreatePropertySet();
		}

		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_INT, METH_NAME, this, dimension);

		scrollControllerExpressionAnimationSources.InsertScalar(s_minOffsetPropertyName, 0.0f);
		scrollControllerExpressionAnimationSources.InsertScalar(s_maxOffsetPropertyName, 0.0f);
		scrollControllerExpressionAnimationSources.InsertScalar(s_offsetPropertyName, 0.0f);
		scrollControllerExpressionAnimationSources.InsertScalar(s_multiplierPropertyName, 1.0f);

		if (dimension == ScrollPresenterDimension.HorizontalScroll)
		{
			MUX_ASSERT(m_horizontalScrollControllerOffsetExpressionAnimation is null);
			MUX_ASSERT(m_horizontalScrollControllerMaxOffsetExpressionAnimation is null);

			m_horizontalScrollControllerOffsetExpressionAnimation = compositor.CreateExpressionAnimation("it.Position.X - it.MinPosition.X");
			m_horizontalScrollControllerOffsetExpressionAnimation.SetReferenceParameter("it", m_interactionTracker);
			m_horizontalScrollControllerMaxOffsetExpressionAnimation = compositor.CreateExpressionAnimation("it.MaxPosition.X - it.MinPosition.X");
			m_horizontalScrollControllerMaxOffsetExpressionAnimation.SetReferenceParameter("it", m_interactionTracker);
		}
		else
		{
			MUX_ASSERT(m_verticalScrollControllerOffsetExpressionAnimation is null);
			MUX_ASSERT(m_verticalScrollControllerMaxOffsetExpressionAnimation is null);

			m_verticalScrollControllerOffsetExpressionAnimation = compositor.CreateExpressionAnimation("it.Position.Y - it.MinPosition.Y");
			m_verticalScrollControllerOffsetExpressionAnimation.SetReferenceParameter("it", m_interactionTracker);
			m_verticalScrollControllerMaxOffsetExpressionAnimation = compositor.CreateExpressionAnimation("it.MaxPosition.Y - it.MinPosition.Y");
			m_verticalScrollControllerMaxOffsetExpressionAnimation.SetReferenceParameter("it", m_interactionTracker);
		}
	}

	private void EnsurePositionBoundariesExpressionAnimations()
	{
		if (m_minPositionExpressionAnimation is null || m_maxPositionExpressionAnimation is null)
		{
			// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

			Compositor compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;

			if (m_minPositionExpressionAnimation is null)
			{
				m_minPositionExpressionAnimation = compositor.CreateExpressionAnimation();
			}
			if (m_maxPositionExpressionAnimation is null)
			{
				m_maxPositionExpressionAnimation = compositor.CreateExpressionAnimation();
			}
		}
	}

	private void EnsureTransformExpressionAnimations()
	{
		if (m_translationExpressionAnimation is null || m_zoomFactorExpressionAnimation is null)
		{
			// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

			Compositor compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;

			if (m_translationExpressionAnimation is null)
			{
				m_translationExpressionAnimation = compositor.CreateExpressionAnimation();
			}

			if (m_zoomFactorExpressionAnimation is null)
			{
				m_zoomFactorExpressionAnimation = compositor.CreateExpressionAnimation();
			}
		}
	}

	private void SetupSnapPoints<T>(
		ref SortedSet<SnapPointWrapper<T>> snapPointsSet,
		ScrollPresenterDimension dimension) where T : SnapPointBase
	{
		MUX_ASSERT(snapPointsSet is not null);

		if (m_interactionTracker is null)
		{
			EnsureInteractionTracker();
		}

		if (m_state == ScrollingInteractionState.Idle)
		{
			double ignoredValue = dimension switch
			{
				ScrollPresenterDimension.VerticalScroll => m_zoomedVerticalOffset / m_zoomFactor,
				ScrollPresenterDimension.HorizontalScroll => m_zoomedHorizontalOffset / m_zoomFactor,
				ScrollPresenterDimension.ZoomFactor => (double)m_zoomFactor,
				_ => throw new Exception("This is not expected to happen."),
			};

			// When snap points are changed while in the Idle State, update
			// ignored snapping values for any potential start of an impulse inertia.
			UpdateSnapPointsIgnoredValue(ref snapPointsSet, ignoredValue);
		}

		// Update the regular and impulse actual applicable ranges.
		UpdateSnapPointsRanges(ref snapPointsSet, false /*forImpulseOnly*/);

		Compositor compositor = m_interactionTracker.Compositor;
		IList<InteractionTrackerInertiaModifier> modifiers = new List<InteractionTrackerInertiaModifier>();

		string target = "";
		string scale = "";

		switch (dimension)
		{
			case ScrollPresenterDimension.HorizontalZoomFactor:
			case ScrollPresenterDimension.VerticalZoomFactor:
			case ScrollPresenterDimension.Scroll:
				//these ScrollPresenterDimensions are not expected
				MUX_ASSERT(false);
				break;
			case ScrollPresenterDimension.HorizontalScroll:
				target = s_naturalRestingPositionXPropertyName;
				scale = s_targetScalePropertyName;
				break;
			case ScrollPresenterDimension.VerticalScroll:
				target = s_naturalRestingPositionYPropertyName;
				scale = s_targetScalePropertyName;
				break;
			case ScrollPresenterDimension.ZoomFactor:
				target = s_naturalRestingScalePropertyName;
				scale = "1.0";
				break;
			default:
				throw new ArgumentException();
				//MUX_ASSERT(false);
		}

		// For older versions of windows the interaction tracker cannot accept empty collections of inertia modifiers
		if (snapPointsSet.Count == 0)
		{
			InteractionTrackerInertiaRestingValue modifier = InteractionTrackerInertiaRestingValue.Create(compositor);
			ExpressionAnimation conditionExpressionAnimation = compositor.CreateExpressionAnimation("false");
			ExpressionAnimation restingPointExpressionAnimation = compositor.CreateExpressionAnimation("this.Target." + target);

			modifier.Condition = conditionExpressionAnimation;
			modifier.RestingValue = restingPointExpressionAnimation;

			modifiers.Add(modifier);
		}
		else
		{
			foreach (var snapPointWrapper in snapPointsSet)
			{
				InteractionTrackerInertiaRestingValue modifier = GetInertiaRestingValue(
					snapPointWrapper,
					compositor,
					target,
					scale);

				modifiers.Add(modifier);
			}
		}

		switch (dimension)
		{
			case ScrollPresenterDimension.HorizontalZoomFactor:
			case ScrollPresenterDimension.VerticalZoomFactor:
			case ScrollPresenterDimension.Scroll:
				//these ScrollPresenterDimensions are not expected
				MUX_ASSERT(false);
				break;
			case ScrollPresenterDimension.HorizontalScroll:
				m_interactionTracker.ConfigurePositionXInertiaModifiers(modifiers);
				break;
			case ScrollPresenterDimension.VerticalScroll:
				m_interactionTracker.ConfigurePositionYInertiaModifiers(modifiers);
				break;
			case ScrollPresenterDimension.ZoomFactor:
				m_interactionTracker.ConfigureScaleInertiaModifiers(modifiers);
				break;
			default:
				throw new ArgumentException();
				//MUX_ASSERT(false);
		}
	}

	//Snap points which have ApplicableRangeType = Optional are optional snap points, and their ActualApplicableRange should never be expanded beyond their ApplicableRange
	//and will only shrink to accommodate other snap points which are positioned such that the midpoint between them is within the specified ApplicableRange.
	//Snap points which have ApplicableRangeType = Mandatory are mandatory snap points and their ActualApplicableRange will expand or shrink to ensure that there is no
	//space between it and its neighbors. If the neighbors are also mandatory, this point will be the midpoint between them. If the neighbors are optional then this
	//point will fall on the midpoint or on the Optional neighbor's edge of ApplicableRange, whichever is furthest.
	private void UpdateSnapPointsRanges<T>(
		ref SortedSet<SnapPointWrapper<T>> snapPointsSet,
		bool forImpulseOnly) where T : SnapPointBase
	{
		MUX_ASSERT(snapPointsSet is not null);

		SnapPointWrapper<T> currentSnapPointWrapper = null;
		SnapPointWrapper<T> previousSnapPointWrapper = null;
		SnapPointWrapper<T> nextSnapPointWrapper = null;

		foreach (var snapPointWrapper in snapPointsSet)
		{
			previousSnapPointWrapper = currentSnapPointWrapper;
			currentSnapPointWrapper = nextSnapPointWrapper;
			nextSnapPointWrapper = snapPointWrapper;

			if (currentSnapPointWrapper is not null)
			{
				currentSnapPointWrapper.DetermineActualApplicableZone(
					previousSnapPointWrapper,
					nextSnapPointWrapper,
					forImpulseOnly);
			}
		}

		if (nextSnapPointWrapper is not null)
		{
			nextSnapPointWrapper.DetermineActualApplicableZone(
				currentSnapPointWrapper,
				null,
				forImpulseOnly);
		}
	}

	private void UpdateSnapPointsIgnoredValue<T>(
		ref SortedSet<SnapPointWrapper<T>> snapPointsSet,
		ScrollPresenterDimension dimension) where T : SnapPointBase
	{
		double newIgnoredValue = dimension switch
		{
			ScrollPresenterDimension.VerticalScroll => m_zoomedVerticalOffset / m_zoomFactor,
			ScrollPresenterDimension.HorizontalScroll => m_zoomedHorizontalOffset / m_zoomFactor,
			ScrollPresenterDimension.ZoomFactor => (double)m_zoomFactor,
			_ => throw new Exception("This is not expected to happen."),
		};

		if (UpdateSnapPointsIgnoredValue(ref snapPointsSet, newIgnoredValue))
		{
			// The ignored snap point value has changed.
			UpdateSnapPointsRanges(ref snapPointsSet, true /*forImpulseOnly*/);

			Compositor compositor = m_interactionTracker.Compositor;
			IList<InteractionTrackerInertiaModifier> modifiers = new List<InteractionTrackerInertiaModifier>();

			foreach (var snapPointWrapper in snapPointsSet)
			{
				var modifier = InteractionTrackerInertiaRestingValue.Create(compositor);
				var (conditionExpressionAnimation, restingValueExpressionAnimation) = snapPointWrapper.GetUpdatedExpressionAnimationsForImpulse();

				modifier.Condition = conditionExpressionAnimation;
				modifier.RestingValue = restingValueExpressionAnimation;

				modifiers.Add(modifier);
			}

			switch (dimension)
			{
				case ScrollPresenterDimension.VerticalScroll:
					m_interactionTracker.ConfigurePositionYInertiaModifiers(modifiers);
					break;
				case ScrollPresenterDimension.HorizontalScroll:
					m_interactionTracker.ConfigurePositionXInertiaModifiers(modifiers);
					break;
				case ScrollPresenterDimension.ZoomFactor:
					m_interactionTracker.ConfigureScaleInertiaModifiers(modifiers);
					break;
			}
		}
	}

	// Updates the ignored snapping value of the provided snap points set when inertia is caused by an impulse.
	// Returns True when an old ignored value was reset or a new ignored value was set.
	private bool UpdateSnapPointsIgnoredValue<T>(
		ref SortedSet<SnapPointWrapper<T>> snapPointsSet,
		double newIgnoredValue) where T : SnapPointBase
	{
		bool ignoredValueUpdated = false;

		foreach (var snapPointWrapper in snapPointsSet)
		{
			if (snapPointWrapper.ResetIgnoredValue())
			{
				ignoredValueUpdated = true;
				break;
			}
		}

		int snapCount = 0;

		foreach (var snapPointWrapper in snapPointsSet)
		{
			SnapPointBase snapPoint = SnapPointWrapper<T>.GetSnapPointFromWrapper(snapPointWrapper);

			snapCount += snapPoint.SnapCount();

			if (snapCount > 1)
			{
				break;
			}
		}

		if (snapCount > 1)
		{
			foreach (var snapPointWrapper in snapPointsSet)
			{
				if (snapPointWrapper.SnapsAt(newIgnoredValue))
				{
					snapPointWrapper.SetIgnoredValue(newIgnoredValue);
					ignoredValueUpdated = true;
					break;
				}
			}
		}

		return ignoredValueUpdated;
	}

	private void SetupInteractionTrackerBoundaries()
	{
		if (m_interactionTracker is null)
		{
			EnsureInteractionTracker();
			SetupInteractionTrackerZoomFactorBoundaries(
				MinZoomFactor,
				MaxZoomFactor);
		}

		UIElement content = Content;

		if (content is not null && (m_minPositionExpressionAnimation is null || m_maxPositionExpressionAnimation is null))
		{
			EnsurePositionBoundariesExpressionAnimations();
			SetupPositionBoundariesExpressionAnimations(content);
		}
	}

	private void SetupInteractionTrackerZoomFactorBoundaries(
		double minZoomFactor, double maxZoomFactor)
	{
		MUX_ASSERT(m_interactionTracker is not null);

#if DEBUG
		//float oldMinZoomFactorDbg = m_interactionTracker.MinScale;
#endif //DBG
		float oldMaxZoomFactor = m_interactionTracker.MaxScale;

		minZoomFactor = Math.Max(0.0, minZoomFactor);
		maxZoomFactor = Math.Max(minZoomFactor, maxZoomFactor);

		float newMinZoomFactor = (float)minZoomFactor;
		float newMaxZoomFactor = (float)maxZoomFactor;

		if (newMinZoomFactor > oldMaxZoomFactor)
		{
			m_interactionTracker.MaxScale = newMaxZoomFactor;
			m_interactionTracker.MinScale = newMinZoomFactor;
		}
		else
		{
			m_interactionTracker.MinScale = newMinZoomFactor;
			m_interactionTracker.MaxScale = newMaxZoomFactor;
		}
	}

	// Configures the VisualInteractionSource instance associated with ScrollPresenter's Visual.
	private void SetupScrollPresenterVisualInteractionSource()
	{
		MUX_ASSERT(m_scrollPresenterVisualInteractionSource is not null);

		SetupVisualInteractionSourceRailingMode(
			m_scrollPresenterVisualInteractionSource,
			ScrollPresenterDimension.HorizontalScroll,
			HorizontalScrollRailMode);

		SetupVisualInteractionSourceRailingMode(
			m_scrollPresenterVisualInteractionSource,
			ScrollPresenterDimension.VerticalScroll,
			VerticalScrollRailMode);

		SetupVisualInteractionSourceChainingMode(
			m_scrollPresenterVisualInteractionSource,
			ScrollPresenterDimension.HorizontalScroll,
			HorizontalScrollChainMode);

		SetupVisualInteractionSourceChainingMode(
			m_scrollPresenterVisualInteractionSource,
			ScrollPresenterDimension.VerticalScroll,
			VerticalScrollChainMode);

		SetupVisualInteractionSourceChainingMode(
			m_scrollPresenterVisualInteractionSource,
			ScrollPresenterDimension.ZoomFactor,
			ZoomChainMode);

		UpdateVisualInteractionSourceMode(
			ScrollPresenterDimension.HorizontalScroll);

		UpdateVisualInteractionSourceMode(
			ScrollPresenterDimension.VerticalScroll);

		SetupVisualInteractionSourceMode(
			m_scrollPresenterVisualInteractionSource,
			ZoomMode);

#if IsMouseWheelZoomDisabled // UNO TODO: Should this be true or false?
		SetupVisualInteractionSourcePointerWheelConfig(
			m_scrollPresenterVisualInteractionSource,
			GetMouseWheelZoomMode());
#endif
	}

	// Configures the VisualInteractionSource instance associated with the Visual handed in
	// through IScrollControllerPanningInfo::PanningElementAncestor.
	private void SetupScrollControllerVisualInterationSource(
		ScrollPresenterDimension dimension)
	{
		MUX_ASSERT(m_interactionTracker is not null);
		MUX_ASSERT(dimension == ScrollPresenterDimension.HorizontalScroll || dimension == ScrollPresenterDimension.VerticalScroll);

		VisualInteractionSource scrollControllerVisualInteractionSource = null;
		Visual panningElementAncestorVisual = null;

		if (dimension == ScrollPresenterDimension.HorizontalScroll)
		{
			scrollControllerVisualInteractionSource = m_horizontalScrollControllerVisualInteractionSource;
			if (m_horizontalScrollControllerPanningInfo is not null)
			{
				UIElement panningElementAncestor = m_horizontalScrollControllerPanningInfo.PanningElementAncestor;

				if (panningElementAncestor is not null)
				{
					panningElementAncestorVisual = ElementCompositionPreview.GetElementVisual(panningElementAncestor);
				}
			}
		}
		else
		{
			scrollControllerVisualInteractionSource = m_verticalScrollControllerVisualInteractionSource;
			if (m_verticalScrollControllerPanningInfo is not null)
			{
				UIElement panningElementAncestor = m_verticalScrollControllerPanningInfo.PanningElementAncestor;

				if (panningElementAncestor is not null)
				{
					panningElementAncestorVisual = ElementCompositionPreview.GetElementVisual(panningElementAncestor);
				}
			}
		}

		if (panningElementAncestorVisual is null && scrollControllerVisualInteractionSource is not null)
		{
			// The IScrollController no longer uses a Visual.
			VisualInteractionSource otherScrollControllerVisualInteractionSource =
				dimension == ScrollPresenterDimension.HorizontalScroll ? m_verticalScrollControllerVisualInteractionSource : m_horizontalScrollControllerVisualInteractionSource;

			if (otherScrollControllerVisualInteractionSource != scrollControllerVisualInteractionSource)
			{
				// The horizontal and vertical IScrollController implementations are not using the same Visual,
				// so the old VisualInteractionSource can be discarded.
				m_interactionTracker.InteractionSources.Remove(scrollControllerVisualInteractionSource);
				StopScrollControllerExpressionAnimationSourcesAnimations(dimension);
				if (dimension == ScrollPresenterDimension.HorizontalScroll)
				{
					m_horizontalScrollControllerVisualInteractionSource = null;
					m_horizontalScrollControllerExpressionAnimationSources = null;
					m_horizontalScrollControllerOffsetExpressionAnimation = null;
					m_horizontalScrollControllerMaxOffsetExpressionAnimation = null;
				}
				else
				{
					m_verticalScrollControllerVisualInteractionSource = null;
					m_verticalScrollControllerExpressionAnimationSources = null;
					m_verticalScrollControllerOffsetExpressionAnimation = null;
					m_verticalScrollControllerMaxOffsetExpressionAnimation = null;
				}

				RaiseInteractionSourcesChanged();
			}
			else
			{
				// The horizontal and vertical IScrollController implementations were using the same Visual,
				// so the old VisualInteractionSource cannot be discarded.
				if (dimension == ScrollPresenterDimension.HorizontalScroll)
				{
					scrollControllerVisualInteractionSource.PositionXSourceMode = InteractionSourceMode.Disabled;
					scrollControllerVisualInteractionSource.IsPositionXRailsEnabled = false;
				}
				else
				{
					scrollControllerVisualInteractionSource.PositionYSourceMode = InteractionSourceMode.Disabled;
					scrollControllerVisualInteractionSource.IsPositionYRailsEnabled = false;
				}
			}
			return;
		}
		else if (panningElementAncestorVisual is not null)
		{
			if (scrollControllerVisualInteractionSource is null)
			{
				// The IScrollController now uses a Visual.
				VisualInteractionSource otherScrollControllerVisualInteractionSource =
					dimension == ScrollPresenterDimension.HorizontalScroll ? m_verticalScrollControllerVisualInteractionSource : m_horizontalScrollControllerVisualInteractionSource;

				if (otherScrollControllerVisualInteractionSource is null || otherScrollControllerVisualInteractionSource.Source != panningElementAncestorVisual)
				{
					// That Visual is not shared with the other dimension, so create a new VisualInteractionSource for it.
					EnsureScrollControllerVisualInteractionSource(panningElementAncestorVisual, dimension);
				}
				else
				{
					// That Visual is shared with the other dimension, so share the existing VisualInteractionSource as well.
					if (dimension == ScrollPresenterDimension.HorizontalScroll)
					{
						m_horizontalScrollControllerVisualInteractionSource = otherScrollControllerVisualInteractionSource;
					}
					else
					{
						m_verticalScrollControllerVisualInteractionSource = otherScrollControllerVisualInteractionSource;
					}
				}
				EnsureScrollControllerExpressionAnimationSources(dimension);
				StartScrollControllerExpressionAnimationSourcesAnimations(dimension);
			}

			Orientation orientation;
			bool isRailEnabled;

			// Setup the VisualInteractionSource instance.
			if (dimension == ScrollPresenterDimension.HorizontalScroll)
			{
				orientation = m_horizontalScrollControllerPanningInfo.PanOrientation;
				isRailEnabled = m_horizontalScrollControllerPanningInfo.IsRailEnabled;

				if (orientation == Orientation.Horizontal)
				{
					m_horizontalScrollControllerVisualInteractionSource.PositionXSourceMode = InteractionSourceMode.EnabledWithoutInertia;
					m_horizontalScrollControllerVisualInteractionSource.IsPositionXRailsEnabled = isRailEnabled;
				}
				else
				{
					m_horizontalScrollControllerVisualInteractionSource.PositionYSourceMode = InteractionSourceMode.EnabledWithoutInertia;
					m_horizontalScrollControllerVisualInteractionSource.IsPositionYRailsEnabled = isRailEnabled;
				}
			}
			else
			{
				orientation = m_verticalScrollControllerPanningInfo.PanOrientation;
				isRailEnabled = m_verticalScrollControllerPanningInfo.IsRailEnabled;

				if (orientation == Orientation.Horizontal)
				{
					m_verticalScrollControllerVisualInteractionSource.PositionXSourceMode = InteractionSourceMode.EnabledWithoutInertia;
					m_verticalScrollControllerVisualInteractionSource.IsPositionXRailsEnabled = isRailEnabled;
				}
				else
				{
					m_verticalScrollControllerVisualInteractionSource.PositionYSourceMode = InteractionSourceMode.EnabledWithoutInertia;
					m_verticalScrollControllerVisualInteractionSource.IsPositionYRailsEnabled = isRailEnabled;
				}
			}

			if (scrollControllerVisualInteractionSource is null)
			{
				SetupScrollControllerVisualInterationSourcePositionModifiers(
					dimension,
					orientation);
			}
		}
	}

	// Configures the Position input modifiers of the VisualInteractionSource associated
	// with an IScrollController Visual.  The scalar called Multiplier from the CompositionPropertySet
	// used in IScrollControllerPanningInfo::SetPanningElementExpressionAnimationSources determines the relative
	// speed of IScrollControllerPanningInfo panning element compared to the ScrollPresenter.Content element.
	// The panning element is clamped based on the Interaction's MinPosition and MaxPosition values.
	// Four CompositionConditionalValue instances cover all scenarios:
	//  - the Position is moved closer to InteractionTracker.MinPosition while the multiplier is negative.
	//  - the Position is moved closer to InteractionTracker.MinPosition while the multiplier is positive.
	//  - the Position is moved closer to InteractionTracker.MaxPosition while the multiplier is negative.
	//  - the Position is moved closer to InteractionTracker.MaxPosition while the multiplier is positive.
	private void SetupScrollControllerVisualInterationSourcePositionModifiers(
		ScrollPresenterDimension dimension,            // Direction of the ScrollPresenter.Content Visual movement.
		Orientation orientation)  // Direction of the IScrollController's Visual movement.
	{
		MUX_ASSERT(dimension == ScrollPresenterDimension.HorizontalScroll || dimension == ScrollPresenterDimension.VerticalScroll);
		MUX_ASSERT(m_interactionTracker is not null);

		VisualInteractionSource scrollControllerVisualInteractionSource = dimension == ScrollPresenterDimension.HorizontalScroll ?
			m_horizontalScrollControllerVisualInteractionSource : m_verticalScrollControllerVisualInteractionSource;
		CompositionPropertySet scrollControllerExpressionAnimationSources = dimension == ScrollPresenterDimension.HorizontalScroll ?
			m_horizontalScrollControllerExpressionAnimationSources : m_verticalScrollControllerExpressionAnimationSources;

		MUX_ASSERT(scrollControllerVisualInteractionSource is not null);
		MUX_ASSERT(scrollControllerExpressionAnimationSources is not null);

		Compositor compositor = scrollControllerVisualInteractionSource.Compositor;
		CompositionConditionalValue[] ccvs = new[] { CompositionConditionalValue.Create(compositor), CompositionConditionalValue.Create(compositor), CompositionConditionalValue.Create(compositor), CompositionConditionalValue.Create(compositor) };
		ExpressionAnimation[] conditions = new[] { compositor.CreateExpressionAnimation(), compositor.CreateExpressionAnimation(), compositor.CreateExpressionAnimation(), compositor.CreateExpressionAnimation() };
		ExpressionAnimation[] values = new[] { compositor.CreateExpressionAnimation(), compositor.CreateExpressionAnimation(), compositor.CreateExpressionAnimation(), compositor.CreateExpressionAnimation() };
		for (int index = 0; index < 4; index++)
		{
			ccvs[index].Condition = conditions[index];
			ccvs[index].Value = values[index];

			values[index].SetReferenceParameter("sceas", scrollControllerExpressionAnimationSources);
			values[index].SetReferenceParameter("scvis", scrollControllerVisualInteractionSource);
			values[index].SetReferenceParameter("it", m_interactionTracker);
		}

		for (int index = 0; index < 3; index++)
		{
			conditions[index].SetReferenceParameter("scvis", scrollControllerVisualInteractionSource);
			conditions[index].SetReferenceParameter("sceas", scrollControllerExpressionAnimationSources);
		}
		conditions[3].Expression = "true";

		var modifiersVector = new List<CompositionConditionalValue>();

		for (int index = 0; index < 4; index++)
		{
			modifiersVector.Add(ccvs[index]);
		}

		if (orientation == Orientation.Horizontal)
		{
			conditions[0].Expression = "scvis.DeltaPosition.X < 0.0f && sceas.Multiplier < 0.0f";
			conditions[1].Expression = "scvis.DeltaPosition.X < 0.0f && sceas.Multiplier >= 0.0f";
			conditions[2].Expression = "scvis.DeltaPosition.X >= 0.0f && sceas.Multiplier < 0.0f";
			// Case #4 <==> scvis.DeltaPosition.X >= 0.0f && sceas.Multiplier > 0.0f, uses conditions[3].Expression(L"true").
			if (dimension == ScrollPresenterDimension.HorizontalScroll)
			{
				var expressionClampToMinPosition = "min(sceas.Multiplier * scvis.DeltaPosition.X, it.Position.X - it.MinPosition.X)";
				var expressionClampToMaxPosition = "max(sceas.Multiplier * scvis.DeltaPosition.X, it.Position.X - it.MaxPosition.X)";

				values[0].Expression = expressionClampToMinPosition;
				values[1].Expression = expressionClampToMaxPosition;
				values[2].Expression = expressionClampToMaxPosition;
				values[3].Expression = expressionClampToMinPosition;
				scrollControllerVisualInteractionSource.ConfigureDeltaPositionXModifiers(modifiersVector);
			}
			else
			{
				var expressionClampToMinPosition = "min(sceas.Multiplier * scvis.DeltaPosition.X, it.Position.Y - it.MinPosition.Y)";
				var expressionClampToMaxPosition = "max(sceas.Multiplier * scvis.DeltaPosition.X, it.Position.Y - it.MaxPosition.Y)";

				values[0].Expression = expressionClampToMinPosition;
				values[1].Expression = expressionClampToMaxPosition;
				values[2].Expression = expressionClampToMaxPosition;
				values[3].Expression = expressionClampToMinPosition;
				scrollControllerVisualInteractionSource.ConfigureDeltaPositionYModifiers(modifiersVector);

				// When the IScrollController's Visual moves horizontally and controls the vertical ScrollPresenter.Content movement, make sure that the
				// vertical finger movements do not affect the ScrollPresenter.Content vertically. The vertical component of the finger movement is filtered out.
				CompositionConditionalValue ccvOrtho = CompositionConditionalValue.Create(compositor);
				ExpressionAnimation conditionOrtho = compositor.CreateExpressionAnimation("true");
				ExpressionAnimation valueOrtho = compositor.CreateExpressionAnimation("0");
				ccvOrtho.Condition = conditionOrtho;
				ccvOrtho.Value = valueOrtho;

				var modifiersVectorOrtho = new List<CompositionConditionalValue>();
				modifiersVectorOrtho.Add(ccvOrtho);

				scrollControllerVisualInteractionSource.ConfigureDeltaPositionXModifiers(modifiersVectorOrtho);
			}
		}
		else
		{
			conditions[0].Expression = "scvis.DeltaPosition.Y < 0.0f && sceas.Multiplier < 0.0f";
			conditions[1].Expression = "scvis.DeltaPosition.Y < 0.0f && sceas.Multiplier >= 0.0f";
			conditions[2].Expression = "scvis.DeltaPosition.Y >= 0.0f && sceas.Multiplier < 0.0f";
			// Case #4 <==> scvis.DeltaPosition.Y >= 0.0f && sceas.Multiplier > 0.0f, uses conditions[3].Expression(L"true").
			if (dimension == ScrollPresenterDimension.HorizontalScroll)
			{
				var expressionClampToMinPosition = "min(sceas.Multiplier * scvis.DeltaPosition.Y, it.Position.X - it.MinPosition.X)";
				var expressionClampToMaxPosition = "max(sceas.Multiplier * scvis.DeltaPosition.Y, it.Position.X - it.MaxPosition.X)";

				values[0].Expression = expressionClampToMinPosition;
				values[1].Expression = expressionClampToMaxPosition;
				values[2].Expression = expressionClampToMaxPosition;
				values[3].Expression = expressionClampToMinPosition;
				scrollControllerVisualInteractionSource.ConfigureDeltaPositionXModifiers(modifiersVector);

				// When the IScrollController's Visual moves vertically and controls the horizontal ScrollPresenter.Content movement, make sure that the
				// horizontal finger movements do not affect the ScrollPresenter.Content horizontally. The horizontal component of the finger movement is filtered out.
				CompositionConditionalValue ccvOrtho = CompositionConditionalValue.Create(compositor);
				ExpressionAnimation conditionOrtho = compositor.CreateExpressionAnimation("true");
				ExpressionAnimation valueOrtho = compositor.CreateExpressionAnimation("0");
				ccvOrtho.Condition = conditionOrtho;
				ccvOrtho.Value = valueOrtho;

				var modifiersVectorOrtho = new List<CompositionConditionalValue>();
				modifiersVectorOrtho.Add(ccvOrtho);

				scrollControllerVisualInteractionSource.ConfigureDeltaPositionYModifiers(modifiersVectorOrtho);
			}
			else
			{
				const string expressionClampToMinPosition = "min(sceas.Multiplier * scvis.DeltaPosition.Y, it.Position.Y - it.MinPosition.Y)";
				const string expressionClampToMaxPosition = "max(sceas.Multiplier * scvis.DeltaPosition.Y, it.Position.Y - it.MaxPosition.Y)";

				values[0].Expression = expressionClampToMinPosition;
				values[1].Expression = expressionClampToMaxPosition;
				values[2].Expression = expressionClampToMaxPosition;
				values[3].Expression = expressionClampToMinPosition;
				scrollControllerVisualInteractionSource.ConfigureDeltaPositionYModifiers(modifiersVector);
			}
		}
	}

	private void SetupVisualInteractionSourceRailingMode(
		VisualInteractionSource visualInteractionSource,
		ScrollPresenterDimension dimension,
		ScrollingRailMode railingMode)
	{
		MUX_ASSERT(visualInteractionSource is not null);
		MUX_ASSERT(dimension == ScrollPresenterDimension.HorizontalScroll || dimension == ScrollPresenterDimension.VerticalScroll);

		if (dimension == ScrollPresenterDimension.HorizontalScroll)
		{
			visualInteractionSource.IsPositionXRailsEnabled = railingMode == ScrollingRailMode.Enabled;
		}
		else
		{
			visualInteractionSource.IsPositionYRailsEnabled = railingMode == ScrollingRailMode.Enabled;
		}
	}

	private void SetupVisualInteractionSourceChainingMode(
		VisualInteractionSource visualInteractionSource,
		ScrollPresenterDimension dimension,
		ScrollingChainMode chainingMode)
	{
		MUX_ASSERT(visualInteractionSource is not null);

		InteractionChainingMode interactionChainingMode = InteractionChainingModeFromChainingMode(chainingMode);

		switch (dimension)
		{
			case ScrollPresenterDimension.HorizontalScroll:
				visualInteractionSource.PositionXChainingMode = interactionChainingMode;
				break;
			case ScrollPresenterDimension.VerticalScroll:
				visualInteractionSource.PositionYChainingMode = interactionChainingMode;
				break;
			case ScrollPresenterDimension.ZoomFactor:
				visualInteractionSource.ScaleChainingMode = interactionChainingMode;
				break;
			default:
				MUX_ASSERT(false);
				break;
		}
	}

	private void SetupVisualInteractionSourceMode(
		VisualInteractionSource visualInteractionSource,
		ScrollPresenterDimension dimension,
		ScrollingScrollMode scrollMode)
	{
		MUX_ASSERT(visualInteractionSource is not null);
		MUX_ASSERT(scrollMode == ScrollingScrollMode.Enabled || scrollMode == ScrollingScrollMode.Disabled);

		InteractionSourceMode interactionSourceMode = InteractionSourceModeFromScrollMode(scrollMode);

		switch (dimension)
		{
			case ScrollPresenterDimension.HorizontalScroll:
				visualInteractionSource.PositionXSourceMode = interactionSourceMode;
				break;
			case ScrollPresenterDimension.VerticalScroll:
				visualInteractionSource.PositionYSourceMode = interactionSourceMode;
				break;
			default:
				MUX_ASSERT(false);
				break;
		}
	}

	private void SetupVisualInteractionSourceMode(
		VisualInteractionSource visualInteractionSource,
		ScrollingZoomMode zoomMode)
	{
		MUX_ASSERT(visualInteractionSource is not null);

		visualInteractionSource.ScaleSourceMode = InteractionSourceModeFromZoomMode(zoomMode);
	}

#if IsMouseWheelScrollDisabled // UNO TODO: Should this be defined?
	private void SetupVisualInteractionSourcePointerWheelConfig(
		VisualInteractionSource visualInteractionSource,
		ScrollPresenterDimension dimension,
		ScrollingScrollMode scrollMode)
	{
		MUX_ASSERT(visualInteractionSource);
		MUX_ASSERT(scrollMode == ScrollingScrollMode.Enabled || scrollMode == ScrollingScrollMode.Disabled);

		InteractionSourceRedirectionMode interactionSourceRedirectionMode = InteractionSourceRedirectionModeFromScrollMode(scrollMode);

		switch (dimension)
		{
			case ScrollPresenterDimension.HorizontalScroll:
				visualInteractionSource.PointerWheelConfig().PositionXSourceMode(interactionSourceRedirectionMode);
				break;
			case ScrollPresenterDimension.VerticalScroll:
				visualInteractionSource.PointerWheelConfig().PositionYSourceMode(interactionSourceRedirectionMode);
				break;
			default:
				MUX_ASSERT(false);
		}
	}
#endif

#if IsMouseWheelZoomDisabled // UNO TODO:
	private void SetupVisualInteractionSourcePointerWheelConfig(
		VisualInteractionSource visualInteractionSource,
		ScrollingZoomMode zoomMode)
	{
		MUX_ASSERT(visualInteractionSource);

		visualInteractionSource.PointerWheelConfig().ScaleSourceMode(InteractionSourceRedirectionModeFromZoomMode(zoomMode));
	}
#endif

	private void SetupVisualInteractionSourceRedirectionMode(
		VisualInteractionSource visualInteractionSource)
	{
		MUX_ASSERT(visualInteractionSource is not null);

		VisualInteractionSourceRedirectionMode redirectionMode = VisualInteractionSourceRedirectionMode.CapableTouchpadOnly;

		if (!IsInputKindIgnored(ScrollingInputKinds.MouseWheel))
		{
			redirectionMode = VisualInteractionSourceRedirectionMode.CapableTouchpadAndPointerWheel;
		}

		visualInteractionSource.ManipulationRedirectionMode = redirectionMode;
	}

#if false // UNO TODO
	private void SetupVisualInteractionSourceCenterPointModifier(
		VisualInteractionSource visualInteractionSource,
		ScrollPresenterDimension dimension)
	{
		MUX_ASSERT(visualInteractionSource is not null);
		MUX_ASSERT(dimension == ScrollPresenterDimension.HorizontalScroll || dimension == ScrollPresenterDimension.VerticalScroll);
		MUX_ASSERT(m_interactionTracker is not null);

		float xamlLayoutOffset = dimension == ScrollPresenterDimension.HorizontalScroll ? m_contentLayoutOffsetX : m_contentLayoutOffsetY;

		if (xamlLayoutOffset == 0.0f)
		{
			if (dimension == ScrollPresenterDimension.HorizontalScroll)
			{
				visualInteractionSource.ConfigureCenterPointXModifiers(null);
				m_interactionTracker.ConfigureCenterPointXInertiaModifiers(null);
			}
			else
			{
				visualInteractionSource.ConfigureCenterPointYModifiers(null);
				m_interactionTracker.ConfigureCenterPointYInertiaModifiers(null);
			}
		}
		else
		{
			Compositor compositor = visualInteractionSource.Compositor;
			ExpressionAnimation conditionCenterPointModifier = compositor.CreateExpressionAnimation("true");
			CompositionConditionalValue conditionValueCenterPointModifier = CompositionConditionalValue.Create(compositor);
			ExpressionAnimation valueCenterPointModifier = compositor.CreateExpressionAnimation(
				dimension == ScrollPresenterDimension.HorizontalScroll ?
				"visualInteractionSource.CenterPoint.X - xamlLayoutOffset" :
				"visualInteractionSource.CenterPoint.Y - xamlLayoutOffset");

			valueCenterPointModifier.SetReferenceParameter("visualInteractionSource", visualInteractionSource);
			valueCenterPointModifier.SetScalarParameter("xamlLayoutOffset", xamlLayoutOffset);

			conditionValueCenterPointModifier.Condition = conditionCenterPointModifier;
			conditionValueCenterPointModifier.Value = valueCenterPointModifier;

			var centerPointModifiers = new List<CompositionConditionalValue>();
			centerPointModifiers.Add(conditionValueCenterPointModifier);

			if (dimension == ScrollPresenterDimension.HorizontalScroll)
			{
				visualInteractionSource.ConfigureCenterPointXModifiers(centerPointModifiers);
				m_interactionTracker.ConfigureCenterPointXInertiaModifiers(centerPointModifiers);
			}
			else
			{
				visualInteractionSource.ConfigureCenterPointYModifiers(centerPointModifiers);
				m_interactionTracker.ConfigureCenterPointYInertiaModifiers(centerPointModifiers);
			}
		}
	}
#endif

	private ScrollingScrollMode GetComputedScrollMode(ScrollPresenterDimension dimension, bool ignoreZoomMode = false)
	{
		ScrollingScrollMode oldComputedScrollMode;
		ScrollingScrollMode newComputedScrollMode;

		if (dimension == ScrollPresenterDimension.HorizontalScroll)
		{
			oldComputedScrollMode = ComputedHorizontalScrollMode;
			newComputedScrollMode = HorizontalScrollMode;
		}
		else
		{
			MUX_ASSERT(dimension == ScrollPresenterDimension.VerticalScroll);
			oldComputedScrollMode = ComputedVerticalScrollMode;
			newComputedScrollMode = VerticalScrollMode;
		}

		if (newComputedScrollMode == ScrollingScrollMode.Auto)
		{
			if (!ignoreZoomMode && ZoomMode == ScrollingZoomMode.Enabled)
			{
				// Allow scrolling when zooming is turned on so that the Content does not get stuck in the given dimension
				// when it becomes smaller than the viewport.
				newComputedScrollMode = ScrollingScrollMode.Enabled;
			}
			else
			{
				if (dimension == ScrollPresenterDimension.HorizontalScroll)
				{
					// Enable horizontal scrolling only when the Content's width is larger than the ScrollPresenter's width
					newComputedScrollMode = ScrollableWidth > 0.0 ? ScrollingScrollMode.Enabled : ScrollingScrollMode.Disabled;
				}
				else
				{
					// Enable vertical scrolling only when the Content's height is larger than the ScrollPresenter's height
					newComputedScrollMode = ScrollableHeight > 0.0 ? ScrollingScrollMode.Enabled : ScrollingScrollMode.Disabled;
				}
			}
		}

		if (oldComputedScrollMode != newComputedScrollMode)
		{
			if (dimension == ScrollPresenterDimension.HorizontalScroll)
			{
				SetValue(ComputedHorizontalScrollModeProperty, newComputedScrollMode);
			}
			else
			{
				SetValue(ComputedVerticalScrollModeProperty, newComputedScrollMode);
			}
		}

		return newComputedScrollMode;
	}

#if IsMouseWheelScrollDisabled // UNO TODO: Should this be defined?
	private ScrollingScrollMode GetComputedMouseWheelScrollMode(ScrollPresenterDimension dimension)
	{
		// TODO: c.f. Task 18569498 - Consider public IsMouseWheelHorizontalScrollDisabled/IsMouseWheelVerticalScrollDisabled properties
		return GetComputedScrollMode(dimension);
	}
#endif

#if IsMouseWheelZoomDisabled // UNO TODO: Should this be defined?
	private ScrollingZoomMode GetMouseWheelZoomMode()
	{
		// TODO: c.f. Task 18569498 - Consider public IsMouseWheelZoomDisabled properties
		return ZoomMode();
	}
#endif

	private double GetComputedMaxWidth(
		double defaultMaxWidth,
		FrameworkElement content)
	{
		MUX_ASSERT(content is not null);

		Thickness contentMargin = content.Margin;
		double marginWidth = contentMargin.Left + contentMargin.Right;
		double computedMaxWidth = defaultMaxWidth;
		double width = content.Width;
		double minWidth = content.MinWidth;
		double maxWidth = content.MaxWidth;

		if (!double.IsNaN(width))
		{
			width = Math.Max(0.0, width + marginWidth);
			computedMaxWidth = width;
		}
		if (!double.IsNaN(minWidth))
		{
			minWidth = Math.Max(0.0, minWidth + marginWidth);
			computedMaxWidth = Math.Max(computedMaxWidth, minWidth);
		}
		if (!double.IsNaN(maxWidth))
		{
			maxWidth = Math.Max(0.0, maxWidth + marginWidth);
			computedMaxWidth = Math.Min(computedMaxWidth, maxWidth);
		}

		return computedMaxWidth;
	}

	private double GetComputedMaxHeight(
		double defaultMaxHeight,
		FrameworkElement content)
	{
		MUX_ASSERT(content is not null);

		Thickness contentMargin = content.Margin;
		double marginHeight = contentMargin.Top + contentMargin.Bottom;
		double computedMaxHeight = defaultMaxHeight;
		double height = content.Height;
		double minHeight = content.MinHeight;
		double maxHeight = content.MaxHeight;

		if (!double.IsNaN(height))
		{
			height = Math.Max(0.0, height + marginHeight);
			computedMaxHeight = height;
		}
		if (!double.IsNaN(minHeight))
		{
			minHeight = Math.Max(0.0, minHeight + marginHeight);
			computedMaxHeight = Math.Max(computedMaxHeight, minHeight);
		}
		if (!double.IsNaN(maxHeight))
		{
			maxHeight = Math.Max(0.0, maxHeight + marginHeight);
			computedMaxHeight = Math.Min(computedMaxHeight, maxHeight);
		}

		return computedMaxHeight;
	}

	// Computes the content's layout offsets at zoomFactor 1 coming from the Margin property and the difference between the extent and render sizes.
	internal Vector2 GetArrangeRenderSizesDelta(
		UIElement content)
	{
		MUX_ASSERT(content is not null);

#if DEBUG
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, L"m_unzoomedExtentWidth", m_unzoomedExtentWidth);
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, L"m_unzoomedExtentHeight", m_unzoomedExtentHeight);
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_FLT, METH_NAME, this, L"content.RenderSize().Width", content.RenderSize().Width);
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_FLT, METH_NAME, this, L"content.RenderSize().Height", content.RenderSize().Height);
#endif

		double deltaX = m_unzoomedExtentWidth - content.RenderSize.Width;
		double deltaY = m_unzoomedExtentHeight - content.RenderSize.Height;

		FrameworkElement contentAsFE = content as FrameworkElement;

		if (contentAsFE is not null)
		{
			HorizontalAlignment horizontalAlignment = contentAsFE.HorizontalAlignment;
			VerticalAlignment verticalAlignment = contentAsFE.VerticalAlignment;
			Thickness contentMargin = contentAsFE.Margin;

#if DEBUG
			// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"horizontalAlignment", horizontalAlignment);
			// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"verticalAlignment", verticalAlignment);
			// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, L"contentMargin.Left", contentMargin.Left);
			// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, L"contentMargin.Right", contentMargin.Right);
			// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, L"contentMargin.Top", contentMargin.Top);
			// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, L"contentMargin.Bottom", contentMargin.Bottom);
#endif

			if (horizontalAlignment == HorizontalAlignment.Left)
			{
				deltaX = 0.0f;
			}
			else
			{
				deltaX -= contentMargin.Left + contentMargin.Right;
			}

			if (verticalAlignment == VerticalAlignment.Top)
			{
				deltaY = 0.0f;
			}
			else
			{
				deltaY -= contentMargin.Top + contentMargin.Bottom;
			}

			if (horizontalAlignment == HorizontalAlignment.Center ||
				horizontalAlignment == HorizontalAlignment.Stretch)
			{
				deltaX /= 2.0f;
			}

			if (verticalAlignment == VerticalAlignment.Center ||
				verticalAlignment == VerticalAlignment.Stretch)
			{
				deltaY /= 2.0f;
			}

			deltaX += contentMargin.Left;
			deltaY += contentMargin.Top;
		}

#if DEBUG
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, L"deltaX", deltaX);
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, L"deltaY", deltaY);
#endif

		return new Vector2((float)deltaX, (float)deltaY);
	}

	// Returns the expression for the m_minPositionExpressionAnimation animation based on the Content.HorizontalAlignment,
	// Content.VerticalAlignment, InteractionTracker.Scale, Content arrange size (which takes Content.Margin into account) and
	// ScrollPresenterVisual.Size properties.
	private string GetMinPositionExpression(
		UIElement content)
	{
		return StringUtil.FormatString("Vector3(%1!s!, %2!s!, 0.0f)", GetMinPositionXExpression(content), GetMinPositionYExpression(content));
	}

	private string GetMinPositionXExpression(
		UIElement content)
	{
		MUX_ASSERT(content is not null);

		FrameworkElement contentAsFE = content as FrameworkElement;

		if (contentAsFE is not null)
		{
			string maxOffset = "contentSizeX * it.Scale - scrollPresenterVisual.Size.X";

			if (contentAsFE.HorizontalAlignment == HorizontalAlignment.Center ||
				contentAsFE.HorizontalAlignment == HorizontalAlignment.Stretch)
			{
				return StringUtil.FormatString("Min(0.0f, (%1!s!) / 2.0f) + contentLayoutOffsetX", maxOffset);
			}
			else if (contentAsFE.HorizontalAlignment == HorizontalAlignment.Right)
			{
				return StringUtil.FormatString("Min(0.0f, %1!s!) + contentLayoutOffsetX", maxOffset);
			}
		}

		return "contentLayoutOffsetX";
	}

	private string GetMinPositionYExpression(
		UIElement content)
	{
		MUX_ASSERT(content is not null);

		FrameworkElement contentAsFE = content as FrameworkElement;

		if (contentAsFE is not null)
		{
			string maxOffset = "contentSizeY * it.Scale - scrollPresenterVisual.Size.Y";

			if (contentAsFE.VerticalAlignment == VerticalAlignment.Center ||
				contentAsFE.VerticalAlignment == VerticalAlignment.Stretch)
			{
				return StringUtil.FormatString("Min(0.0f, (%1!s!) / 2.0f) + contentLayoutOffsetY", maxOffset);
			}
			else if (contentAsFE.VerticalAlignment == VerticalAlignment.Bottom)
			{
				return StringUtil.FormatString("Min(0.0f, %1!s!) + contentLayoutOffsetY", maxOffset);
			}
		}

		return "contentLayoutOffsetY";
	}

	// Returns the expression for the m_maxPositionExpressionAnimation animation based on the Content.HorizontalAlignment,
	// Content.VerticalAlignment, InteractionTracker.Scale, Content arrange size (which takes Content.Margin into account) and
	// ScrollPresenterVisual.Size properties.
	private string GetMaxPositionExpression(
		UIElement content)
	{
		return StringUtil.FormatString("Vector3(%1!s!, %2!s!, 0.0f)", GetMaxPositionXExpression(content), GetMaxPositionYExpression(content));
	}

	private string GetMaxPositionXExpression(
		UIElement content)
	{
		MUX_ASSERT(content is not null);

		FrameworkElement contentAsFE = content as FrameworkElement;

		if (contentAsFE is not null)
		{
			string maxOffset = "(contentSizeX * it.Scale - scrollPresenterVisual.Size.X)";

			if (contentAsFE.HorizontalAlignment == HorizontalAlignment.Center ||
				contentAsFE.HorizontalAlignment == HorizontalAlignment.Stretch)
			{
				return StringUtil.FormatString("%1!s! >= 0 ? %1!s! + contentLayoutOffsetX : %1!s! / 2.0f + contentLayoutOffsetX", maxOffset);
			}
			else if (contentAsFE.HorizontalAlignment == HorizontalAlignment.Right)
			{
				return StringUtil.FormatString("%1!s! + contentLayoutOffsetX", maxOffset);
			}
		}

		return "Max(0.0f, contentSizeX * it.Scale - scrollPresenterVisual.Size.X) + contentLayoutOffsetX";
	}

	private string GetMaxPositionYExpression(
		UIElement content)
	{
		MUX_ASSERT(content is not null);

		FrameworkElement contentAsFE = content as FrameworkElement;

		if (contentAsFE is not null)
		{
			string maxOffset = "(contentSizeY * it.Scale - scrollPresenterVisual.Size.Y)";

			if (contentAsFE.VerticalAlignment == VerticalAlignment.Center ||
				contentAsFE.VerticalAlignment == VerticalAlignment.Stretch)
			{
				return StringUtil.FormatString("%1!s! >= 0 ? %1!s! + contentLayoutOffsetY : %1!s! / 2.0f + contentLayoutOffsetY", maxOffset);
			}
			else if (contentAsFE.VerticalAlignment == VerticalAlignment.Bottom)
			{
				return StringUtil.FormatString("%1!s! + contentLayoutOffsetY", maxOffset);
			}
		}

		return "Max(0.0f, contentSizeY * it.Scale - scrollPresenterVisual.Size.Y) + contentLayoutOffsetY";
	}

	private CompositionAnimation GetPositionAnimation(
		double zoomedHorizontalOffset,
		double zoomedVerticalOffset,
		InteractionTrackerAsyncOperationTrigger operationTrigger,
		int offsetsChangeCorrelationId)
	{
		MUX_ASSERT(m_interactionTracker is not null);

		long minDuration = s_offsetsChangeMinMs;
		long maxDuration = s_offsetsChangeMaxMs;
		long unitDuration = s_offsetsChangeMsPerUnit;
		bool isHorizontalScrollControllerRequest = (operationTrigger & InteractionTrackerAsyncOperationTrigger.HorizontalScrollControllerRequest) != 0;
		bool isVerticalScrollControllerRequest = (operationTrigger & InteractionTrackerAsyncOperationTrigger.VerticalScrollControllerRequest) != 0;
		long distance = (long)(Math.Sqrt(Math.Pow(zoomedHorizontalOffset - m_zoomedHorizontalOffset, 2.0) + Math.Pow(zoomedVerticalOffset - m_zoomedVerticalOffset, 2.0)));
		Compositor compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
		Vector3KeyFrameAnimation positionAnimation = compositor.CreateVector3KeyFrameAnimation();
		ScrollPresenterTestHooks globalTestHooks = ScrollPresenterTestHooks.GetGlobalTestHooks();

		if (globalTestHooks is not null)
		{
			int unitDurationTestOverride;
			int minDurationTestOverride;
			int maxDurationTestOverride;

			ScrollPresenterTestHooks.GetOffsetsChangeVelocityParameters(out unitDurationTestOverride, out minDurationTestOverride, out maxDurationTestOverride);

			minDuration = minDurationTestOverride;
			maxDuration = maxDurationTestOverride;
			unitDuration = unitDurationTestOverride;
		}

		Vector2 endPosition = ComputePositionFromOffsets(zoomedHorizontalOffset, zoomedVerticalOffset);

		positionAnimation.InsertKeyFrame(1.0f, new Vector3(endPosition, 0.0f));
		positionAnimation.Duration = new TimeSpan(ticks: Math.Clamp(distance * unitDuration, minDuration, maxDuration) * 10000);

		Vector2 startPosition = new Vector2(m_interactionTracker.Position.X, m_interactionTracker.Position.Y);

		if (isHorizontalScrollControllerRequest || isVerticalScrollControllerRequest)
		{
			CompositionAnimation customAnimation = null;

			if (isHorizontalScrollControllerRequest && m_horizontalScrollController is not null)
			{
				customAnimation = m_horizontalScrollController.GetScrollAnimation(
					offsetsChangeCorrelationId,
					startPosition,
					endPosition,
					positionAnimation);
			}
			if (isVerticalScrollControllerRequest && m_verticalScrollController is not null)
			{
				customAnimation = m_verticalScrollController.GetScrollAnimation(
					offsetsChangeCorrelationId,
					startPosition,
					endPosition,
					customAnimation is not null ? customAnimation : positionAnimation);
			}
			return customAnimation is not null ? customAnimation : positionAnimation;
		}

		return RaiseScrollAnimationStarting(positionAnimation, startPosition, endPosition, offsetsChangeCorrelationId);
	}

	CompositionAnimation GetZoomFactorAnimation(
		float zoomFactor,
		Vector2 centerPoint,
		int zoomFactorChangeCorrelationId)
	{
		long minDuration = s_zoomFactorChangeMinMs;
		long maxDuration = s_zoomFactorChangeMaxMs;
		long unitDuration = s_zoomFactorChangeMsPerUnit;
		long distance = (long)(Math.Abs(zoomFactor - m_zoomFactor));
		Compositor compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
		ScalarKeyFrameAnimation zoomFactorAnimation = compositor.CreateScalarKeyFrameAnimation();
		ScrollPresenterTestHooks globalTestHooks = ScrollPresenterTestHooks.GetGlobalTestHooks();

		if (globalTestHooks is not null)
		{
			int unitDurationTestOverride;
			int minDurationTestOverride;
			int maxDurationTestOverride;

			ScrollPresenterTestHooks.GetZoomFactorChangeVelocityParameters(out unitDurationTestOverride, out minDurationTestOverride, out maxDurationTestOverride);

			minDuration = minDurationTestOverride;
			maxDuration = maxDurationTestOverride;
			unitDuration = unitDurationTestOverride;
		}

		zoomFactorAnimation.InsertKeyFrame(1.0f, zoomFactor);
		zoomFactorAnimation.Duration = new TimeSpan(ticks: Math.Clamp(distance * unitDuration, minDuration, maxDuration) * 10000);

		return RaiseZoomAnimationStarting(zoomFactorAnimation, zoomFactor, centerPoint, zoomFactorChangeCorrelationId);
	}

	private int GetNextViewChangeCorrelationId()
	{
		return (m_latestViewChangeCorrelationId == int.MaxValue) ? 0 : m_latestViewChangeCorrelationId + 1;
	}

	void SetupPositionBoundariesExpressionAnimations(
		UIElement content)
	{
		MUX_ASSERT(content is not null);
		MUX_ASSERT(m_minPositionExpressionAnimation is not null);
		MUX_ASSERT(m_maxPositionExpressionAnimation is not null);
		MUX_ASSERT(m_interactionTracker is not null);

		Visual scrollPresenterVisual = ElementCompositionPreview.GetElementVisual(this);

		string s = m_minPositionExpressionAnimation.Expression;

		if (s.Length == 0)
		{
			m_minPositionExpressionAnimation.SetReferenceParameter("it", m_interactionTracker);
			m_minPositionExpressionAnimation.SetReferenceParameter("scrollPresenterVisual", scrollPresenterVisual);
		}

		m_minPositionExpressionAnimation.Expression = GetMinPositionExpression(content);

		s = m_maxPositionExpressionAnimation.Expression;

		if (s.Length == 0)
		{
			m_maxPositionExpressionAnimation.SetReferenceParameter("it", m_interactionTracker);
			m_maxPositionExpressionAnimation.SetReferenceParameter("scrollPresenterVisual", scrollPresenterVisual);
		}

		m_maxPositionExpressionAnimation.Expression = GetMaxPositionExpression(content);

		UpdatePositionBoundaries(content);
	}

	void SetupTransformExpressionAnimations(
		UIElement content)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		MUX_ASSERT(content is not null);
		MUX_ASSERT(m_translationExpressionAnimation is not null);
		MUX_ASSERT(m_zoomFactorExpressionAnimation is not null);
		MUX_ASSERT(m_interactionTracker is not null);

		Vector2 arrangeRenderSizesDelta = GetArrangeRenderSizesDelta(content);

		m_translationExpressionAnimation.Expression =
			"Vector3(-it.Position.X + (it.Scale - 1.0f) * adjustment.X, -it.Position.Y + (it.Scale - 1.0f) * adjustment.Y, 0.0f)";
		m_translationExpressionAnimation.SetReferenceParameter("it", m_interactionTracker);
		m_translationExpressionAnimation.SetVector2Parameter("adjustment", arrangeRenderSizesDelta);

		m_zoomFactorExpressionAnimation.Expression = "Vector3(it.Scale, it.Scale, 1.0f)";
		m_zoomFactorExpressionAnimation.SetReferenceParameter("it", m_interactionTracker);

		StartTransformExpressionAnimations(content, false /*forAnimationsInterruption*/);
	}

	private void StartTransformExpressionAnimations(
		UIElement content,
		bool forAnimationsInterruption)
	{
		if (content is not null)
		{
			var zoomFactorPropertyName = GetVisualTargetedPropertyName(ScrollPresenterDimension.ZoomFactor);
			var scrollPropertyName = GetVisualTargetedPropertyName(ScrollPresenterDimension.Scroll);

			m_translationExpressionAnimation.Target = scrollPropertyName;
			m_zoomFactorExpressionAnimation.Target = zoomFactorPropertyName;

			content.StartAnimation(m_translationExpressionAnimation);
			RaiseExpressionAnimationStatusChanged(true /*isExpressionAnimationStarted*/, scrollPropertyName /*propertyName*/);

			content.StartAnimation(m_zoomFactorExpressionAnimation);
			RaiseExpressionAnimationStatusChanged(true /*isExpressionAnimationStarted*/, zoomFactorPropertyName /*propertyName*/);
		}
	}

	private void StopTransformExpressionAnimations(
		UIElement content,
		bool forAnimationsInterruption)
	{
		if (content is not null)
		{
			var scrollPropertyName = GetVisualTargetedPropertyName(ScrollPresenterDimension.Scroll);

			content.StopAnimation(m_translationExpressionAnimation);
			RaiseExpressionAnimationStatusChanged(false /*isExpressionAnimationStarted*/, scrollPropertyName /*propertyName*/);

			var zoomFactorPropertyName = GetVisualTargetedPropertyName(ScrollPresenterDimension.ZoomFactor);

			content.StopAnimation(m_zoomFactorExpressionAnimation);
			RaiseExpressionAnimationStatusChanged(false /*isExpressionAnimationStarted*/, zoomFactorPropertyName /*propertyName*/);
		}
	}

	// Returns True when ScrollPresenter::OnCompositionTargetRendering calls are not needed for restarting the Translation and Scale animations.
	private bool StartTranslationAndZoomFactorExpressionAnimations(bool interruptCountdown = false)
	{
		if (m_translationAndZoomFactorAnimationsRestartTicksCountdown > 0)
		{
			// A Translation and Scale animations restart is pending after the Idle State was reached or a zoom factor change operation completed.
			m_translationAndZoomFactorAnimationsRestartTicksCountdown--;

			if (m_translationAndZoomFactorAnimationsRestartTicksCountdown == 0 || interruptCountdown)
			{
				// Countdown is over or state is no longer Idle, restart the Translation and Scale animations.
				MUX_ASSERT(m_interactionTracker is not null);

				// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_FLT_FLT, METH_NAME, this, m_animationRestartZoomFactor, m_zoomFactor);

				if (m_translationAndZoomFactorAnimationsRestartTicksCountdown > 0)
				{
					MUX_ASSERT(interruptCountdown);

					// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_INT, METH_NAME, this, m_translationAndZoomFactorAnimationsRestartTicksCountdown);
					m_translationAndZoomFactorAnimationsRestartTicksCountdown = 0;
				}

				StartTransformExpressionAnimations(Content, true /*forAnimationsInterruption*/);
			}
			else
			{
				// Countdown needs to continue.
				return false;
			}
		}

		return true;
	}

	private void StopTranslationAndZoomFactorExpressionAnimations()
	{
		if (m_zoomFactorExpressionAnimation is not null && m_animationRestartZoomFactor != m_zoomFactor)
		{
			// The zoom factor has changed since the last restart of the Translation and Scale animations.
			UIElement content = Content;

			if (m_translationAndZoomFactorAnimationsRestartTicksCountdown == 0)
			{
				//SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_FLT_FLT, METH_NAME, this, m_animationRestartZoomFactor, m_zoomFactor);

				// Stop Translation and Scale animations to trigger rasterization of Content, to avoid fuzzy text rendering for instance.
				StopTransformExpressionAnimations(content, true /*forAnimationsInterruption*/);

				// Trigger ScrollPresenter::OnCompositionTargetRendering calls in order to re-establish the Translation and Scale animations
				// after the Content rasterization was triggered within a few ticks.
				HookCompositionTargetRendering();
			}

			m_animationRestartZoomFactor = m_zoomFactor;
			m_translationAndZoomFactorAnimationsRestartTicksCountdown = s_translationAndZoomFactorAnimationsRestartTicks;
		}
	}

	private void StartExpressionAnimationSourcesAnimations()
	{
		MUX_ASSERT(m_interactionTracker is not null);
		MUX_ASSERT(m_expressionAnimationSources is not null);
		MUX_ASSERT(m_positionSourceExpressionAnimation is not null);
		MUX_ASSERT(m_minPositionSourceExpressionAnimation is not null);
		MUX_ASSERT(m_maxPositionSourceExpressionAnimation is not null);
		MUX_ASSERT(m_zoomFactorSourceExpressionAnimation is not null);

		m_expressionAnimationSources.StartAnimation(s_positionSourcePropertyName, m_positionSourceExpressionAnimation);
		RaiseExpressionAnimationStatusChanged(true /*isExpressionAnimationStarted*/, s_positionSourcePropertyName /*propertyName*/);

		m_expressionAnimationSources.StartAnimation(s_minPositionSourcePropertyName, m_minPositionSourceExpressionAnimation);
		RaiseExpressionAnimationStatusChanged(true /*isExpressionAnimationStarted*/, s_minPositionSourcePropertyName /*propertyName*/);

		m_expressionAnimationSources.StartAnimation(s_maxPositionSourcePropertyName, m_maxPositionSourceExpressionAnimation);
		RaiseExpressionAnimationStatusChanged(true /*isExpressionAnimationStarted*/, s_maxPositionSourcePropertyName /*propertyName*/);

		m_expressionAnimationSources.StartAnimation(s_zoomFactorSourcePropertyName, m_zoomFactorSourceExpressionAnimation);
		RaiseExpressionAnimationStatusChanged(true /*isExpressionAnimationStarted*/, s_zoomFactorSourcePropertyName /*propertyName*/);
	}

	private void StartScrollControllerExpressionAnimationSourcesAnimations(
		ScrollPresenterDimension dimension)
	{
		MUX_ASSERT(dimension == ScrollPresenterDimension.HorizontalScroll || dimension == ScrollPresenterDimension.VerticalScroll);

		if (dimension == ScrollPresenterDimension.HorizontalScroll)
		{
			MUX_ASSERT(m_horizontalScrollControllerExpressionAnimationSources is not null);
			MUX_ASSERT(m_horizontalScrollControllerOffsetExpressionAnimation is not null);
			MUX_ASSERT(m_horizontalScrollControllerMaxOffsetExpressionAnimation is not null);

			m_horizontalScrollControllerExpressionAnimationSources.StartAnimation(s_offsetPropertyName, m_horizontalScrollControllerOffsetExpressionAnimation);
			RaiseExpressionAnimationStatusChanged(true /*isExpressionAnimationStarted*/, s_offsetPropertyName /*propertyName*/);

			m_horizontalScrollControllerExpressionAnimationSources.StartAnimation(s_maxOffsetPropertyName, m_horizontalScrollControllerMaxOffsetExpressionAnimation);
			RaiseExpressionAnimationStatusChanged(true /*isExpressionAnimationStarted*/, s_maxOffsetPropertyName /*propertyName*/);
		}
		else
		{
			MUX_ASSERT(m_verticalScrollControllerExpressionAnimationSources is not null);
			MUX_ASSERT(m_verticalScrollControllerOffsetExpressionAnimation is not null);
			MUX_ASSERT(m_verticalScrollControllerMaxOffsetExpressionAnimation is not null);

			m_verticalScrollControllerExpressionAnimationSources.StartAnimation(s_offsetPropertyName, m_verticalScrollControllerOffsetExpressionAnimation);
			RaiseExpressionAnimationStatusChanged(true /*isExpressionAnimationStarted*/, s_offsetPropertyName /*propertyName*/);

			m_verticalScrollControllerExpressionAnimationSources.StartAnimation(s_maxOffsetPropertyName, m_verticalScrollControllerMaxOffsetExpressionAnimation);
			RaiseExpressionAnimationStatusChanged(true /*isExpressionAnimationStarted*/, s_maxOffsetPropertyName /*propertyName*/);
		}
	}

	private void StopScrollControllerExpressionAnimationSourcesAnimations(
		ScrollPresenterDimension dimension)
	{
		MUX_ASSERT(dimension == ScrollPresenterDimension.HorizontalScroll || dimension == ScrollPresenterDimension.VerticalScroll);

		if (dimension == ScrollPresenterDimension.HorizontalScroll)
		{
			MUX_ASSERT(m_horizontalScrollControllerExpressionAnimationSources is not null);

			m_horizontalScrollControllerExpressionAnimationSources.StopAnimation(s_offsetPropertyName);
			RaiseExpressionAnimationStatusChanged(false /*isExpressionAnimationStarted*/, s_offsetPropertyName /*propertyName*/);

			m_horizontalScrollControllerExpressionAnimationSources.StopAnimation(s_maxOffsetPropertyName);
			RaiseExpressionAnimationStatusChanged(false /*isExpressionAnimationStarted*/, s_maxOffsetPropertyName /*propertyName*/);
		}
		else
		{
			MUX_ASSERT(m_verticalScrollControllerExpressionAnimationSources is not null);

			m_verticalScrollControllerExpressionAnimationSources.StopAnimation(s_offsetPropertyName);
			RaiseExpressionAnimationStatusChanged(false /*isExpressionAnimationStarted*/, s_offsetPropertyName /*propertyName*/);

			m_verticalScrollControllerExpressionAnimationSources.StopAnimation(s_maxOffsetPropertyName);
			RaiseExpressionAnimationStatusChanged(false /*isExpressionAnimationStarted*/, s_maxOffsetPropertyName /*propertyName*/);
		}
	}

	private static InteractionChainingMode InteractionChainingModeFromChainingMode(
		ScrollingChainMode chainingMode)
	{
		switch (chainingMode)
		{
			case ScrollingChainMode.Always:
				return InteractionChainingMode.Always;
			case ScrollingChainMode.Auto:
				return InteractionChainingMode.Auto;
			default:
				return InteractionChainingMode.Never;
		}
	}

#if IsMouseWheelScrollDisabled // UNO TODO:
	InteractionSourceRedirectionMode InteractionSourceRedirectionModeFromScrollMode(
		const ScrollingScrollMode& scrollMode)
	{
		MUX_ASSERT(scrollMode == ScrollingScrollMode.Enabled || scrollMode == ScrollingScrollMode.Disabled);

		return scrollMode == ScrollingScrollMode.Enabled ? InteractionSourceRedirectionMode.Enabled : InteractionSourceRedirectionMode.Disabled;
	}
#endif

#if IsMouseWheelZoomDisabled // UNO TODO:
	InteractionSourceRedirectionMode InteractionSourceRedirectionModeFromZoomMode(
		ScrollingZoomMode zoomMode)
	{
		return zoomMode == ScrollingZoomMode.Enabled ? InteractionSourceRedirectionMode.Enabled : InteractionSourceRedirectionMode.Disabled;
	}
#endif

	private static InteractionSourceMode InteractionSourceModeFromScrollMode(
		ScrollingScrollMode scrollMode)
	{
		return scrollMode == ScrollingScrollMode.Enabled ? InteractionSourceMode.EnabledWithInertia : InteractionSourceMode.Disabled;
	}

	private static InteractionSourceMode InteractionSourceModeFromZoomMode(
		ScrollingZoomMode zoomMode)
	{
		return zoomMode == ScrollingZoomMode.Enabled ? InteractionSourceMode.EnabledWithInertia : InteractionSourceMode.Disabled;
	}

	private static double ComputeZoomedOffsetWithMinimalChange(
		double viewportStart,
		double viewportEnd,
		double childStart,
		double childEnd)
	{
		bool above = childStart < viewportStart && childEnd < viewportEnd;
		bool below = childEnd > viewportEnd && childStart > viewportStart;
		bool larger = (childEnd - childStart) > (viewportEnd - viewportStart);

		// # CHILD POSITION   CHILD SIZE   SCROLL   REMEDY
		// 1 Above viewport   <= viewport  Down     Align top edge of content & viewport
		// 2 Above viewport   >  viewport  Down     Align bottom edge of content & viewport
		// 3 Below viewport   <= viewport  Up       Align bottom edge of content & viewport
		// 4 Below viewport   >  viewport  Up       Align top edge of content & viewport
		// 5 Entirely within viewport      NA       No change
		// 6 Spanning viewport             NA       No change
		if ((above && !larger) || (below && larger))
		{
			// Cases 1 & 4
			return childStart;
		}
		else if (above || below)
		{
			// Cases 2 & 3
			return childEnd - viewportEnd + viewportStart;
		}

		// cases 5 & 6
		return viewportStart;
	}

	private static Rect GetDescendantBounds(
		UIElement content,
		UIElement descendant,
		Rect descendantRect)
	{
		MUX_ASSERT(content is not null);

		FrameworkElement contentAsFE = content as FrameworkElement;
		GeneralTransform transform = descendant.TransformToVisual(content);
		Thickness contentMargin = default;

		if (contentAsFE is not null)
		{
			contentMargin = contentAsFE.Margin;
		}

		return transform.TransformBounds(new Rect(
			(float)(contentMargin.Left + descendantRect.X),
			(float)(contentMargin.Top + descendantRect.Y),
			descendantRect.Width,
			descendantRect.Height));
	}

	private static ScrollingAnimationMode GetComputedAnimationMode(
		ScrollingAnimationMode animationMode)
	{
		if (animationMode == ScrollingAnimationMode.Auto)
		{
			bool isAnimationsEnabled;

			var globalTestHooks = ScrollPresenterTestHooks.GetGlobalTestHooks();

			if (globalTestHooks is not null && ScrollPresenterTestHooks.IsAnimationsEnabledOverride is not null)
			{
				isAnimationsEnabled = ScrollPresenterTestHooks.IsAnimationsEnabledOverride.Value;
			}
			else
			{
				isAnimationsEnabled = SharedHelpers.IsAnimationsEnabled();
			}

			return isAnimationsEnabled ? ScrollingAnimationMode.Enabled : ScrollingAnimationMode.Disabled;
		}

		return animationMode;
	}

	static bool IsZoomFactorBoundaryValid(
		double value)
	{
		return !double.IsNaN(value) && double.IsFinite(value);
	}

	internal static void ValidateZoomFactoryBoundary(double value)
	{
		if (!IsZoomFactorBoundaryValid(value))
		{
			throw new ArgumentException();
		}
	}

	// Returns the target property path, according to the availability of the ElementCompositionPreview::SetIsTranslationEnabled method,
	// and the provided dimension.
	string GetVisualTargetedPropertyName(ScrollPresenterDimension dimension)
	{
		switch (dimension)
		{
			case ScrollPresenterDimension.Scroll:
				return s_translationPropertyName;
			default:
				MUX_ASSERT(dimension == ScrollPresenterDimension.ZoomFactor);
				return s_scalePropertyName;
		}
	}

	// Invoked by both ScrollPresenter and ScrollViewer controls
	static bool IsAnchorRatioValid(
		double value)
	{
		return double.IsNaN(value) || (double.IsFinite(value) && value >= 0.0 && value <= 1.0);
	}

	internal static void ValidateAnchorRatio(double value)
	{
		if (!IsAnchorRatioValid(value))
		{
			throw new ArgumentException();
		}
	}

	internal bool IsElementValidAnchor(
		UIElement element)
	{
		return IsElementValidAnchor(element, Content);
	}

	// Invoked by ScrollPresenterTestHooks
	internal void SetContentLayoutOffsetX(float contentLayoutOffsetX)
	{
		//SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_FLT_FLT, METH_NAME, this, contentLayoutOffsetX, m_contentLayoutOffsetX);

		if (m_contentLayoutOffsetX != contentLayoutOffsetX)
		{
			UpdateOffset(ScrollPresenterDimension.HorizontalScroll, m_zoomedHorizontalOffset + contentLayoutOffsetX - m_contentLayoutOffsetX);
			m_contentLayoutOffsetX = contentLayoutOffsetX;
			InvalidateArrange();
			OnContentLayoutOffsetChanged(ScrollPresenterDimension.HorizontalScroll);
			OnViewChanged(true /*horizontalOffsetChanged*/, false /*verticalOffsetChanged*/);
		}
	}

	internal void SetContentLayoutOffsetY(float contentLayoutOffsetY)
	{
		//SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_FLT_FLT, METH_NAME, this, contentLayoutOffsetY, m_contentLayoutOffsetY);

		if (m_contentLayoutOffsetY != contentLayoutOffsetY)
		{
			UpdateOffset(ScrollPresenterDimension.VerticalScroll, m_zoomedVerticalOffset + contentLayoutOffsetY - m_contentLayoutOffsetY);
			m_contentLayoutOffsetY = contentLayoutOffsetY;
			InvalidateArrange();
			OnContentLayoutOffsetChanged(ScrollPresenterDimension.VerticalScroll);
			OnViewChanged(false /*horizontalOffsetChanged*/, true /*verticalOffsetChanged*/);
		}
	}

	internal Vector2 GetArrangeRenderSizesDelta()
	{
		Vector2 arrangeRenderSizesDelta = default;
		UIElement content = Content;

		if (content is not null)
		{
			arrangeRenderSizesDelta = GetArrangeRenderSizesDelta(content);
		}

		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_FLT_FLT, METH_NAME, this, arrangeRenderSizesDelta.x, arrangeRenderSizesDelta.y);

		return arrangeRenderSizesDelta;
	}

	internal Vector2 GetMinPosition()
	{
		Vector2 minPosition;

		ComputeMinMaxPositions(m_zoomFactor, out minPosition, out _);

		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_FLT_FLT, METH_NAME, this, minPosition.x, minPosition.y);

		return minPosition;
	}

	internal Vector2 GetMaxPosition()
	{
		Vector2 maxPosition;

		ComputeMinMaxPositions(m_zoomFactor, out _, out maxPosition);

		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_FLT_FLT, METH_NAME, this, maxPosition.x, maxPosition.y);

		return maxPosition;
	}

	private IList<ScrollSnapPointBase> GetConsolidatedScrollSnapPoints(ScrollPresenterDimension dimension)
	{
		IList<ScrollSnapPointBase> snapPoints = new List<ScrollSnapPointBase>();
		SortedSet<SnapPointWrapper<ScrollSnapPointBase>> snapPointsSet;

		switch (dimension)
		{
			case ScrollPresenterDimension.VerticalScroll:
				snapPointsSet = m_sortedConsolidatedVerticalSnapPoints;
				break;
			case ScrollPresenterDimension.HorizontalScroll:
				snapPointsSet = m_sortedConsolidatedHorizontalSnapPoints;
				break;
			default:
				//MUX_ASSERT(false);
				throw new ArgumentException();
		}

		foreach (SnapPointWrapper<ScrollSnapPointBase> snapPointWrapper in snapPointsSet)
		{
			snapPoints.Add(snapPointWrapper.SnapPoint);
		}
		return snapPoints;
	}

	internal IList<ZoomSnapPointBase> GetConsolidatedZoomSnapPoints()
	{
		IList<ZoomSnapPointBase> snapPoints = new List<ZoomSnapPointBase>();

		foreach (SnapPointWrapper<ZoomSnapPointBase> snapPointWrapper in m_sortedConsolidatedZoomSnapPoints)
		{
			snapPoints.Add(snapPointWrapper.SnapPoint);
		}
		return snapPoints;
	}

	SnapPointWrapper<ScrollSnapPointBase> GetScrollSnapPointWrapper(ScrollPresenterDimension dimension, ScrollSnapPointBase scrollSnapPoint)
	{
		var snapPointsSet = dimension switch
		{
			ScrollPresenterDimension.VerticalScroll => m_sortedConsolidatedVerticalSnapPoints,
			ScrollPresenterDimension.HorizontalScroll => m_sortedConsolidatedHorizontalSnapPoints,
			_ => throw new Exception("This is not expected to happen."),
		};

		foreach (SnapPointWrapper<ScrollSnapPointBase> snapPointWrapper in snapPointsSet)
		{
			ScrollSnapPointBase winrtScrollSnapPoint = snapPointWrapper.SnapPoint as ScrollSnapPointBase;

			if (winrtScrollSnapPoint == scrollSnapPoint)
			{
				return snapPointWrapper;
			}
		}

		return null;
	}

	internal SnapPointWrapper<ZoomSnapPointBase> GetZoomSnapPointWrapper(ZoomSnapPointBase zoomSnapPoint)
	{
		foreach (SnapPointWrapper<ZoomSnapPointBase> snapPointWrapper in m_sortedConsolidatedZoomSnapPoints)
		{
			ZoomSnapPointBase winrtZoomSnapPoint = snapPointWrapper.SnapPoint as ZoomSnapPointBase;

			if (winrtZoomSnapPoint == zoomSnapPoint)
			{
				return snapPointWrapper;
			}
		}

		return null;
	}

	// Invoked when a dependency property of this ScrollPresenter has changed.
	void OnPropertyChanged(
		DependencyPropertyChangedEventArgs args)
	{
		var dependencyProperty = args.Property;

#if DEBUG
		//SCROLLPRESENTER_TRACE_VERBOSE(null, "%s(property: %s)\n", METH_NAME, DependencyPropertyToString(dependencyProperty));
#endif

		if (dependencyProperty == ContentProperty)
		{
			object oldContent = args.OldValue;
			object newContent = args.NewValue;
			UpdateContent(oldContent as UIElement, newContent as UIElement);
		}
		else if (dependencyProperty == BackgroundProperty)
		{
			// Uno specific: On WinUI, ScrollPresenter inherits both from Panel (internally) and FrameworkElement (publicly)
			// So, they can just cast ScrollPresenter to Panel and set the Background.
			// In our case, we can't do that as there is no multi inheritance in C#.
			// We use BorderLayerRenderer to draw the background.
#if UNO_HAS_BORDER_VISUAL
			this.UpdateBackground();
#else
			_borderRenderer.Update();
#endif

			//Panel thisAsPanel = this;

			//thisAsPanel.Background = args.NewValue as Brush;
		}
		else if (dependencyProperty == MinZoomFactorProperty || dependencyProperty == MaxZoomFactorProperty)
		{
			MUX_ASSERT(IsZoomFactorBoundaryValid((double)(args.OldValue)));

			if (m_interactionTracker is not null)
			{
				SetupInteractionTrackerZoomFactorBoundaries(
					MinZoomFactor,
					MaxZoomFactor);
			}
		}
		else if (dependencyProperty == ContentOrientationProperty)
		{
			m_contentOrientation = ContentOrientation;

			InvalidateMeasure();
		}
		else if (dependencyProperty == HorizontalAnchorRatioProperty ||
			dependencyProperty == VerticalAnchorRatioProperty)
		{
			MUX_ASSERT(IsAnchorRatioValid((double)(args.OldValue)));

			m_isAnchorElementDirty = true;
		}
		else if (m_scrollPresenterVisualInteractionSource is not null)
		{
			if (dependencyProperty == HorizontalScrollChainModeProperty)
			{
				SetupVisualInteractionSourceChainingMode(
					m_scrollPresenterVisualInteractionSource,
					ScrollPresenterDimension.HorizontalScroll,
					HorizontalScrollChainMode);
			}
			else if (dependencyProperty == VerticalScrollChainModeProperty)
			{
				SetupVisualInteractionSourceChainingMode(
					m_scrollPresenterVisualInteractionSource,
					ScrollPresenterDimension.VerticalScroll,
					VerticalScrollChainMode);
			}
			else if (dependencyProperty == ZoomChainModeProperty)
			{
				SetupVisualInteractionSourceChainingMode(
					m_scrollPresenterVisualInteractionSource,
					ScrollPresenterDimension.ZoomFactor,
					ZoomChainMode);
			}
			else if (dependencyProperty == HorizontalScrollRailModeProperty)
			{
				SetupVisualInteractionSourceRailingMode(
					m_scrollPresenterVisualInteractionSource,
					ScrollPresenterDimension.HorizontalScroll,
					HorizontalScrollRailMode);
			}
			else if (dependencyProperty == VerticalScrollRailModeProperty)
			{
				SetupVisualInteractionSourceRailingMode(
					m_scrollPresenterVisualInteractionSource,
					ScrollPresenterDimension.VerticalScroll,
					VerticalScrollRailMode);
			}
			else if (dependencyProperty == HorizontalScrollModeProperty)
			{
				UpdateVisualInteractionSourceMode(
					ScrollPresenterDimension.HorizontalScroll);
			}
			else if (dependencyProperty == VerticalScrollModeProperty)
			{
				UpdateVisualInteractionSourceMode(
					ScrollPresenterDimension.VerticalScroll);
			}
			else if (dependencyProperty == ZoomModeProperty)
			{
				// Updating the horizontal and vertical scroll modes because GetComputedScrollMode is function of ZoomMode.
				UpdateVisualInteractionSourceMode(
					ScrollPresenterDimension.HorizontalScroll);
				UpdateVisualInteractionSourceMode(
					ScrollPresenterDimension.VerticalScroll);

				SetupVisualInteractionSourceMode(
					m_scrollPresenterVisualInteractionSource,
					ZoomMode);

#if IsMouseWheelZoomDisabled
				SetupVisualInteractionSourcePointerWheelConfig(
					m_scrollPresenterVisualInteractionSource,
					GetMouseWheelZoomMode());
#endif
			}
			else if (dependencyProperty == IgnoredInputKindsProperty)
			{
				UpdateManipulationRedirectionMode();
			}
		}
	}

	void OnContentPropertyChanged(DependencyObject sender, DependencyProperty args)
	{
		//SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		UIElement content = Content;

		if (content is not null)
		{
			if (args == FrameworkElement.HorizontalAlignmentProperty ||
				args == FrameworkElement.VerticalAlignmentProperty)
			{
				// The ExtentWidth and ExtentHeight may have to be updated because of this alignment change.
				InvalidateMeasure();

				if (m_interactionTracker is not null)
				{
					if (m_minPositionExpressionAnimation is not null && m_maxPositionExpressionAnimation is not null)
					{
						SetupPositionBoundariesExpressionAnimations(content);
					}

					if (m_translationExpressionAnimation is not null && m_zoomFactorExpressionAnimation is not null)
					{
						SetupTransformExpressionAnimations(content);
					}
				}
			}
			else if (args == FrameworkElement.MinWidthProperty ||
				args == FrameworkElement.WidthProperty ||
				args == FrameworkElement.MaxWidthProperty ||
				args == FrameworkElement.MinHeightProperty ||
				args == FrameworkElement.HeightProperty ||
				args == FrameworkElement.MaxHeightProperty)
			{
				InvalidateMeasure();
			}
		}
	}

	void OnCompositionTargetRendering(object sender, object args)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		bool unhookCompositionTargetRendering = StartTranslationAndZoomFactorExpressionAnimations();

		if (m_interactionTrackerAsyncOperations.Count > 0 && IsLoaded)
		{
			bool delayProcessingViewChanges = false;

			// Uno docs: Calling ToArray to avoid modifying the collection while enumerating it.
			foreach (var interactionTrackerAsyncOperation in m_interactionTrackerAsyncOperations.ToArray())
			{
				if (interactionTrackerAsyncOperation.IsDelayed())
				{
					interactionTrackerAsyncOperation.SetIsDelayed(false);
					unhookCompositionTargetRendering = false;
					MUX_ASSERT(interactionTrackerAsyncOperation.IsQueued());
				}
				else if (interactionTrackerAsyncOperation.IsQueued())
				{
					if (!delayProcessingViewChanges && interactionTrackerAsyncOperation.GetTicksCountdown() == 1)
					{
						// Evaluate whether all remaining queued operations need to be delayed until the completion of a prior required operation.
						InteractionTrackerAsyncOperation requiredInteractionTrackerAsyncOperation = interactionTrackerAsyncOperation.GetRequiredOperation();

						if (requiredInteractionTrackerAsyncOperation is not null)
						{
							if (!requiredInteractionTrackerAsyncOperation.IsCanceled() && !requiredInteractionTrackerAsyncOperation.IsCompleted())
							{
								// Prior required operation is not canceled or completed yet. All subsequent operations need to be delayed.
								delayProcessingViewChanges = true;
							}
							else
							{
								// Previously set required operation is now canceled or completed. Check if it needs to be replaced with an older one.
								requiredInteractionTrackerAsyncOperation = GetLastNonAnimatedInteractionTrackerOperation(interactionTrackerAsyncOperation);
								interactionTrackerAsyncOperation.SetRequiredOperation(requiredInteractionTrackerAsyncOperation);
								if (requiredInteractionTrackerAsyncOperation is not null)
								{
									// An older operation is now required. All subsequent operations need to be delayed.
									delayProcessingViewChanges = true;
								}
							}
						}
					}

					if (delayProcessingViewChanges)
					{
						if (interactionTrackerAsyncOperation.GetTicksCountdown() > 1)
						{
							// Ticking the queued operation without processing it.
							interactionTrackerAsyncOperation.TickQueuedOperation();
						}
						unhookCompositionTargetRendering = false;
					}
					else if (interactionTrackerAsyncOperation.TickQueuedOperation())
					{
						// InteractionTracker is ready for the operation's processing.
						ProcessDequeuedViewChange(interactionTrackerAsyncOperation);
						if (!interactionTrackerAsyncOperation.IsAnimated())
						{
							unhookCompositionTargetRendering = false;
						}
					}
					else
					{
						unhookCompositionTargetRendering = false;
					}
				}
				else if (!interactionTrackerAsyncOperation.IsAnimated())
				{
					if (interactionTrackerAsyncOperation.TickNonAnimatedOperation())
					{
						// The non-animated view change request did not result in a status change or ValuesChanged notification. Consider it completed.
						CompleteViewChange(interactionTrackerAsyncOperation, ScrollPresenterViewChangeResult.Completed);
						if (m_translationAndZoomFactorAnimationsRestartTicksCountdown > 0)
						{
							// Do not unhook the Rendering event when there is a pending restart of the Translation and Scale animations. 
							unhookCompositionTargetRendering = false;
						}
						m_interactionTrackerAsyncOperations.Remove(interactionTrackerAsyncOperation);
					}
					else
					{
						unhookCompositionTargetRendering = false;
					}
				}
			}
		}

		if (unhookCompositionTargetRendering)
		{
			UnhookCompositionTargetRendering();
		}
	}

	void OnLoaded(
		object sender,
		object args)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		SetupInteractionTrackerBoundaries();

		EnsureScrollPresenterVisualInteractionSource();
		SetupScrollPresenterVisualInteractionSource();
		SetupScrollControllerVisualInterationSource(ScrollPresenterDimension.HorizontalScroll);
		SetupScrollControllerVisualInterationSource(ScrollPresenterDimension.VerticalScroll);

		if (m_horizontalScrollControllerExpressionAnimationSources is not null)
		{
			MUX_ASSERT(m_horizontalScrollControllerPanningInfo is not null);

			m_horizontalScrollControllerPanningInfo.SetPanningElementExpressionAnimationSources(
				m_horizontalScrollControllerExpressionAnimationSources,
				s_minOffsetPropertyName,
				s_maxOffsetPropertyName,
				s_offsetPropertyName,
				s_multiplierPropertyName);
		}
		if (m_verticalScrollControllerExpressionAnimationSources is not null)
		{
			MUX_ASSERT(m_verticalScrollControllerPanningInfo is not null);

			m_verticalScrollControllerPanningInfo.SetPanningElementExpressionAnimationSources(
				m_verticalScrollControllerExpressionAnimationSources,
				s_minOffsetPropertyName,
				s_maxOffsetPropertyName,
				s_offsetPropertyName,
				s_multiplierPropertyName);
		}

		UIElement content = Content;

		if (content is not null)
		{
			if (m_translationExpressionAnimation is null || m_zoomFactorExpressionAnimation is null)
			{
				EnsureTransformExpressionAnimations();
				SetupTransformExpressionAnimations(content);
			}

			// Process the potentially delayed operation in the OnCompositionTargetRendering handler.
			HookCompositionTargetRendering();
		}
	}

	void OnUnloaded(
		object sender,
		RoutedEventArgs args)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		if (!IsLoaded)
		{
			// Uno docs: Commenting out the assert. This is possibly linked to https://github.com/unoplatform/uno/issues/13299
			//MUX_ASSERT(RenderSize.Width == 0.0);
			//MUX_ASSERT(RenderSize.Height == 0.0);

			// All potential pending operations are interrupted when the ScrollPresenter unloads.
			CompleteInteractionTrackerOperations(
				-1 /*requestId*/,
				ScrollPresenterViewChangeResult.Interrupted /*operationResult*/,
				ScrollPresenterViewChangeResult.Ignored     /*unused priorNonAnimatedOperationsResult*/,
				ScrollPresenterViewChangeResult.Ignored     /*unused priorAnimatedOperationsResult*/,
				true  /*completeNonAnimatedOperation*/,
				true  /*completeAnimatedOperation*/,
				false /*completePriorNonAnimatedOperations*/,
				false /*completePriorAnimatedOperations*/);

			// Unhook the potential OnCompositionTargetRendering handler since there are no pending operations.
			UnhookCompositionTargetRendering();

			UIElement content = Content;

			UpdateUnzoomedExtentAndViewport(
				false /*renderSizeChanged*/,
				content is not null ? m_unzoomedExtentWidth : 0.0,
				content is not null ? m_unzoomedExtentHeight : 0.0,
				0.0 /*viewportWidth*/,
				0.0 /*viewportHeight*/);
		}
	}

	// UIElement.BringIntoViewRequested event handler to bring an element into the viewport.
	void OnBringIntoViewRequestedHandler(
		object sender,
		BringIntoViewRequestedEventArgs args)
	{
#if DEBUG
		// SCROLLPRESENTER_TRACE_INFO(*this, L"%s[0x%p](AnimationDesired:%d, Handled:%d, H/V AlignmentRatio:%lf,%lf, H/V Offset:%f,%f, TargetRect:%s, TargetElement:0x%p)\n",
		// 	METH_NAME, this,
		// 	args.AnimationDesired(), args.Handled(),
		// 	args.HorizontalAlignmentRatio(), args.VerticalAlignmentRatio(),
		// 	args.HorizontalOffset(), args.VerticalOffset(),
		// 	TypeLogging::RectToString(args.TargetRect()).c_str(), args.TargetElement());
#endif

		UIElement content = Content;

		if (args.Handled ||
			args.TargetElement == this ||
			(args.TargetElement == content && content.Visibility == Visibility.Collapsed) ||
			(args.TargetElement != content && !SharedHelpers.IsAncestor(args.TargetElement, content, true /*checkVisibility*/)))
		{
			// Ignore the request when:
			// - There is no InteractionTracker to fulfill it.
			// - It was handled already.
			// - The target element is this ScrollPresenter itself. A parent scrollPresenter may fulfill the request instead then.
			// - The target element is effectively collapsed within the ScrollPresenter.
			return;
		}

		Rect targetRect = default;
		int offsetsChangeCorrelationId = s_noOpCorrelationId;
		double targetZoomedHorizontalOffset = 0.0;
		double targetZoomedVerticalOffset = 0.0;
		double appliedOffsetX = 0.0;
		double appliedOffsetY = 0.0;
		ScrollingSnapPointsMode snapPointsMode = ScrollingSnapPointsMode.Ignore;

		// Compute the target offsets based on the provided BringIntoViewRequestedEventArgs.
		ComputeBringIntoViewTargetOffsetsFromRequestEventArgs(
			content,
			snapPointsMode,
			args,
			out targetZoomedHorizontalOffset,
			out targetZoomedVerticalOffset,
			out appliedOffsetX,
			out appliedOffsetY,
			out targetRect);

		if (HasBringingIntoViewListener())
		{
			// Raise the ScrollPresenter.BringingIntoView event to give the listeners a chance to adjust the operation.

			offsetsChangeCorrelationId = m_latestViewChangeCorrelationId = GetNextViewChangeCorrelationId();

			if (!RaiseBringingIntoView(
				targetZoomedHorizontalOffset,
				targetZoomedVerticalOffset,
				args,
				offsetsChangeCorrelationId,
				ref snapPointsMode))
			{
				// A listener canceled the operation in the ScrollPresenter.BringingIntoView event handler before any scrolling was attempted.
				RaiseViewChangeCompleted(true /*isForScroll*/, ScrollPresenterViewChangeResult.Completed, offsetsChangeCorrelationId);
				return;
			}

			content = Content;

			if (content is null ||
				args.Handled ||
				args.TargetElement == this ||
				(args.TargetElement == content && content.Visibility == Visibility.Collapsed) ||
				(args.TargetElement != content && !SharedHelpers.IsAncestor(args.TargetElement, content, true /*checkVisibility*/)))
			{
				// Again, ignore the request when:
				// - There is no Content anymore.
				// - The request was handled already.
				// - The target element is this ScrollPresenter itself. A parent scrollPresenter may fulfill the request instead then.
				// - The target element is effectively collapsed within the ScrollPresenter.
				return;
			}

			// Re-evaluate the target offsets based on the potentially modified BringIntoViewRequestedEventArgs.
			// Take into account potential SnapPointsMode == Default so that parents contribute accordingly.
			ComputeBringIntoViewTargetOffsetsFromRequestEventArgs(
				content,
				snapPointsMode,
				args,
				out targetZoomedHorizontalOffset,
				out targetZoomedVerticalOffset,
				out appliedOffsetX,
				out appliedOffsetY,
				out targetRect);
		}

		// Do not include the applied offsets so that potential parent bring-into-view contributors ignore that shift.
		Rect nextTargetRect = new Rect(
			(float)(targetRect.X * m_zoomFactor - targetZoomedHorizontalOffset - appliedOffsetX),
			(float)(targetRect.Y * m_zoomFactor - targetZoomedVerticalOffset - appliedOffsetY),
			Math.Min(targetRect.Width * m_zoomFactor, (float)m_viewportWidth),
			Math.Min(targetRect.Height * m_zoomFactor, (float)m_viewportHeight)
		);

		Rect viewportRect = new Rect(
			0.0f,
			0.0f,
			(float)m_viewportWidth,
			(float)m_viewportHeight
		);

		if (targetZoomedHorizontalOffset != m_zoomedHorizontalOffset ||
			targetZoomedVerticalOffset != m_zoomedVerticalOffset)
		{
			ScrollingScrollOptions options =
				new ScrollingScrollOptions(
					args.AnimationDesired ? ScrollingAnimationMode.Auto : ScrollingAnimationMode.Disabled,
					snapPointsMode);

			ChangeOffsetsPrivate(
				targetZoomedHorizontalOffset /*zoomedHorizontalOffset*/,
				targetZoomedVerticalOffset /*zoomedVerticalOffset*/,
				ScrollPresenterViewKind.Absolute,
				options,
				args /*bringIntoViewRequestedEventArgs*/,
				InteractionTrackerAsyncOperationTrigger.BringIntoViewRequest,
				offsetsChangeCorrelationId /*existingViewChangeCorrelationId*/,
				out _ /*viewChangeCorrelationId*/);
		}
		else
		{
			// No offset change was triggered because the target offsets are the same as the current ones. Mark the operation as completed immediately.
			RaiseViewChangeCompleted(true /*isForScroll*/, ScrollPresenterViewChangeResult.Completed, offsetsChangeCorrelationId);
		}

		if (SharedHelpers.DoRectsIntersect(nextTargetRect, viewportRect))
		{
			// Next bring a portion of this ScrollPresenter into view.
			args.TargetRect = nextTargetRect;
			args.TargetElement = this;
			args.HorizontalOffset = args.HorizontalOffset - appliedOffsetX;
			args.VerticalOffset = args.VerticalOffset - appliedOffsetY;
		}
		else
		{
			// This ScrollPresenter did not even partially bring the TargetRect into its viewport.
			// Mark the operation as handled since no portion of this ScrollPresenter needs to be brought into view.
			args.Handled = true;
		}
	}

	void OnPointerPressed(
		object sender,
		PointerRoutedEventArgs args)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		MUX_ASSERT(m_interactionTracker is not null);
		MUX_ASSERT(m_scrollPresenterVisualInteractionSource is not null);

		if (m_horizontalScrollController is not null && m_horizontalScrollController.IsScrollingWithMouse)
		{
			return;
		}

		if (m_verticalScrollController is not null && m_verticalScrollController.IsScrollingWithMouse)
		{
			return;
		}

		UIElement content = Content;
		ScrollingScrollMode horizontalScrollMode = GetComputedScrollMode(ScrollPresenterDimension.HorizontalScroll);
		ScrollingScrollMode verticalScrollMode = GetComputedScrollMode(ScrollPresenterDimension.VerticalScroll);

		if (content is null ||
			(horizontalScrollMode == ScrollingScrollMode.Disabled &&
				verticalScrollMode == ScrollingScrollMode.Disabled &&
				ZoomMode == ScrollingZoomMode.Disabled))
		{
			return;
		}

		switch (args.Pointer.PointerDeviceType)
		{
			case PointerDeviceType.Touch:
				if (IsInputKindIgnored(ScrollingInputKinds.Touch))
					return;
				break;
			case PointerDeviceType.Pen:
				if (IsInputKindIgnored(ScrollingInputKinds.Pen))
					return;
				break;
			default:
				return;
		}

		// All UIElement instances between the touched one and the ScrollPresenter must include ManipulationModes.System in their
		// ManipulationMode property in order to trigger a manipulation. This allows to turn off touch interactions in particular.
		object source = args.OriginalSource;
		MUX_ASSERT(source is not null);

		DependencyObject sourceAsDO = source as DependencyObject;

		UIElement thisAsUIElement = this; // Need to have exactly the same interface as we're comparing below for object equality

		while (sourceAsDO is not null)
		{
			UIElement sourceAsUIE = sourceAsDO as UIElement;
			if (sourceAsUIE is not null)
			{
				ManipulationModes mm = sourceAsUIE.ManipulationMode;

				if ((mm & ManipulationModes.System) == ManipulationModes.None)
				{
					return;
				}

				if (sourceAsUIE == thisAsUIElement)
				{
					break;
				}
			}

			sourceAsDO = VisualTreeHelper.GetParent(sourceAsDO);
		}

#if DEBUG
		DumpMinMaxPositions();

		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_METH_STR, METH_NAME, this, L"TryRedirectForManipulation", TypeLogging::PointerPointToString(args.GetCurrentPoint(nullptr)).c_str());
#endif // DBG

		// try
		//{
		m_scrollPresenterVisualInteractionSource.TryRedirectForManipulation(args.GetCurrentPoint(null));
		//}
		// catch (const hresult_error & e)
		// {
		// 	// Swallowing Access Denied error because of InteractionTracker bug 17434718 which has been
		// 	// causing crashes at least in RS3, RS4 and RS5.
		// 	// TODO - Stop eating the error in future OS versions that include a fix for 17434718 if any.
		// 	if (e.to_abi() != E_ACCESSDENIED)
		// 	{
		// 		throw;
		// 	}
		// }
	}

	// Invoked by an IScrollControllerPanningInfo implementation when a call to InteractionTracker::TryRedirectForManipulation
	// is required to track a finger.
	void OnScrollControllerPanningInfoPanRequested(
		IScrollControllerPanningInfo sender,
		ScrollControllerPanRequestedEventArgs args)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_PTR, METH_NAME, this, sender);

		MUX_ASSERT(sender == m_horizontalScrollControllerPanningInfo || sender == m_verticalScrollControllerPanningInfo);

		if (args.Handled)
		{
			return;
		}

		VisualInteractionSource scrollControllerVisualInteractionSource = null;

		if (sender == m_horizontalScrollControllerPanningInfo)
		{
			scrollControllerVisualInteractionSource = m_horizontalScrollControllerVisualInteractionSource;
		}
		else
		{
			scrollControllerVisualInteractionSource = m_verticalScrollControllerVisualInteractionSource;
		}

		if (scrollControllerVisualInteractionSource is not null)
		{
			//try
			//{
			scrollControllerVisualInteractionSource.TryRedirectForManipulation(args.PointerPoint);
			//}
			// catch (const hresult_error & e)
			// {
			// 	// Swallowing Access Denied error because of InteractionTracker bug 17434718 which has been
			// 	// causing crashes at least in RS3, RS4 and RS5.
			// 	// TODO - Stop eating the error in future OS versions that include a fix for 17434718 if any.
			// 	if (e.to_abi() == E_ACCESSDENIED)
			// 	{
			// 		// Do not set the Handled flag. The request is simply ignored.
			// 		return;
			// 	}
			// 	else
			// 	{
			// 		throw;
			// 	}
			// }
			args.Handled = true;
		}
	}

	// Invoked by an IScrollControllerPanningInfo implementation when one or more of its characteristics has changed:
	// PanningElementAncestor, PanOrientation or IsRailEnabled.
	void OnScrollControllerPanningInfoChanged(
		IScrollControllerPanningInfo sender,
		object args)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_PTR, METH_NAME, this, sender);

		MUX_ASSERT(sender == m_horizontalScrollControllerPanningInfo || sender == m_verticalScrollControllerPanningInfo);

		if (m_interactionTracker is null)
		{
			return;
		}

		bool isFromHorizontalScrollController = sender == m_horizontalScrollControllerPanningInfo;

		CompositionPropertySet scrollControllerExpressionAnimationSources =
			isFromHorizontalScrollController ? m_horizontalScrollControllerExpressionAnimationSources : m_verticalScrollControllerExpressionAnimationSources;

		SetupScrollControllerVisualInterationSource(isFromHorizontalScrollController ? ScrollPresenterDimension.HorizontalScroll : ScrollPresenterDimension.VerticalScroll);

		if (isFromHorizontalScrollController)
		{
			if (scrollControllerExpressionAnimationSources != m_horizontalScrollControllerExpressionAnimationSources)
			{
				MUX_ASSERT(m_horizontalScrollControllerPanningInfo is not null);

				m_horizontalScrollControllerPanningInfo.SetPanningElementExpressionAnimationSources(
					m_horizontalScrollControllerExpressionAnimationSources,
					s_minOffsetPropertyName,
					s_maxOffsetPropertyName,
					s_offsetPropertyName,
					s_multiplierPropertyName);
			}
		}
		else
		{
			if (scrollControllerExpressionAnimationSources != m_verticalScrollControllerExpressionAnimationSources)
			{
				MUX_ASSERT(m_verticalScrollControllerPanningInfo is not null);

				m_verticalScrollControllerPanningInfo.SetPanningElementExpressionAnimationSources(
					m_verticalScrollControllerExpressionAnimationSources,
					s_minOffsetPropertyName,
					s_maxOffsetPropertyName,
					s_offsetPropertyName,
					s_multiplierPropertyName);
			}
		}
	}

	// Invoked when a IScrollController::ScrollToRequested event is raised in order to perform the
	// equivalent of a ScrollPresenter::ScrollTo operation.
	void OnScrollControllerScrollToRequested(
		IScrollController sender,
		ScrollControllerScrollToRequestedEventArgs args)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_PTR, METH_NAME, this, sender);
		MUX_ASSERT(sender == m_horizontalScrollController || sender == m_verticalScrollController);

		bool isFromHorizontalScrollController = sender == m_horizontalScrollController;
		int viewChangeCorrelationId = s_noOpCorrelationId;

		// Attempt to find an offset change request from an IScrollController with the same ScrollPresenterViewKind,
		// the same ScrollingScrollOptions settings and same tick.
		InteractionTrackerAsyncOperation interactionTrackerAsyncOperation = GetInteractionTrackerOperationFromKinds(
			true /*isOperationTypeForOffsetsChange*/,
			(InteractionTrackerAsyncOperationTrigger)((int)InteractionTrackerAsyncOperationTrigger.HorizontalScrollControllerRequest + (int)InteractionTrackerAsyncOperationTrigger.VerticalScrollControllerRequest),
			ScrollPresenterViewKind.Absolute,
			args.Options);

		if (interactionTrackerAsyncOperation is null)
		{
			ChangeOffsetsPrivate(
				isFromHorizontalScrollController ? args.Offset : m_zoomedHorizontalOffset,
				isFromHorizontalScrollController ? m_zoomedVerticalOffset : args.Offset,
				ScrollPresenterViewKind.Absolute,
				args.Options,
				null /*bringIntoViewRequestedEventArgs*/,
				isFromHorizontalScrollController ? InteractionTrackerAsyncOperationTrigger.HorizontalScrollControllerRequest : InteractionTrackerAsyncOperationTrigger.VerticalScrollControllerRequest,
				s_noOpCorrelationId /*existingViewChangeCorrelationId*/,
				out viewChangeCorrelationId);
		}
		else
		{
			// Coalesce requests
			int existingViewChangeCorrelationId = interactionTrackerAsyncOperation.GetViewChangeCorrelationId();
			ViewChangeBase viewChangeBase = interactionTrackerAsyncOperation.GetViewChangeBase();
			OffsetsChange offsetsChange = (OffsetsChange)viewChangeBase;

			interactionTrackerAsyncOperation.SetIsScrollControllerRequest(isFromHorizontalScrollController);

			if (isFromHorizontalScrollController)
			{
				offsetsChange.ZoomedHorizontalOffset = args.Offset;
			}
			else
			{
				offsetsChange.ZoomedVerticalOffset = args.Offset;
			}

			viewChangeCorrelationId = existingViewChangeCorrelationId;
		}

		if (viewChangeCorrelationId != s_noOpCorrelationId)
		{
			args.CorrelationId = viewChangeCorrelationId;
		}
	}

	// Invoked when a IScrollController::ScrollByRequested event is raised in order to perform the
	// equivalent of a ScrollPresenter::ScrollBy operation.
	void OnScrollControllerScrollByRequested(
		IScrollController sender,
		ScrollControllerScrollByRequestedEventArgs args)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_PTR, METH_NAME, this, sender);
		MUX_ASSERT(sender == m_horizontalScrollController || sender == m_verticalScrollController);

		bool isFromHorizontalScrollController = sender == m_horizontalScrollController;
		int viewChangeCorrelationId = s_noOpCorrelationId;

		// Attempt to find an offset change request from an IScrollController with the same ScrollPresenterViewKind,
		// the same ScrollingScrollOptions settings and same tick.
		InteractionTrackerAsyncOperation interactionTrackerAsyncOperation = GetInteractionTrackerOperationFromKinds(
			true /*isOperationTypeForOffsetsChange*/,
			(InteractionTrackerAsyncOperationTrigger)((int)InteractionTrackerAsyncOperationTrigger.HorizontalScrollControllerRequest + (int)InteractionTrackerAsyncOperationTrigger.VerticalScrollControllerRequest),
			ScrollPresenterViewKind.RelativeToCurrentView,
			args.Options);

		if (interactionTrackerAsyncOperation is null)
		{
			ChangeOffsetsPrivate(
				isFromHorizontalScrollController ? args.OffsetDelta : 0.0 /*zoomedHorizontalOffset*/,
				isFromHorizontalScrollController ? 0.0 : args.OffsetDelta /*zoomedVerticalOffset*/,
				ScrollPresenterViewKind.RelativeToCurrentView,
				args.Options,
				null /*bringIntoViewRequestedEventArgs*/,
				isFromHorizontalScrollController ? InteractionTrackerAsyncOperationTrigger.HorizontalScrollControllerRequest : InteractionTrackerAsyncOperationTrigger.VerticalScrollControllerRequest,
				s_noOpCorrelationId /*existingViewChangeCorrelationId*/,
				out viewChangeCorrelationId);
		}
		else
		{
			// Coalesce requests
			int existingViewChangeCorrelationId = interactionTrackerAsyncOperation.GetViewChangeCorrelationId();
			ViewChangeBase viewChangeBase = interactionTrackerAsyncOperation.GetViewChangeBase();
			OffsetsChange offsetsChange = (OffsetsChange)viewChangeBase;

			interactionTrackerAsyncOperation.SetIsScrollControllerRequest(isFromHorizontalScrollController);

			if (isFromHorizontalScrollController)
			{
				offsetsChange.ZoomedHorizontalOffset = offsetsChange.ZoomedHorizontalOffset + args.OffsetDelta;
			}
			else
			{
				offsetsChange.ZoomedVerticalOffset = offsetsChange.ZoomedVerticalOffset + args.OffsetDelta;
			}

			viewChangeCorrelationId = existingViewChangeCorrelationId;
		}

		if (viewChangeCorrelationId != s_noOpCorrelationId)
		{
			args.CorrelationId = viewChangeCorrelationId;
		}
	}

	// Invoked when a IScrollController::AddScrollVelocityRequested event is raised in order to perform the
	// equivalent of a ScrollPresenter::AddScrollVelocity operation.
	void OnScrollControllerAddScrollVelocityRequested(
		IScrollController sender,
		ScrollControllerAddScrollVelocityRequestedEventArgs args)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_PTR, METH_NAME, this, sender);
		MUX_ASSERT(sender == m_horizontalScrollController || sender == m_verticalScrollController);

		bool isFromHorizontalScrollController = sender == m_horizontalScrollController;
		int viewChangeCorrelationId = s_noOpCorrelationId;
		float? horizontalInertiaDecayRate = null;
		float? verticalInertiaDecayRate = null;

		// Attempt to find an offset change with velocity request from an IScrollController and this same tick.
		InteractionTrackerAsyncOperation interactionTrackerAsyncOperation = GetInteractionTrackerOperationWithAdditionalVelocity(
			true /*isOperationTypeForOffsetsChange*/,
			(InteractionTrackerAsyncOperationTrigger)((int)InteractionTrackerAsyncOperationTrigger.HorizontalScrollControllerRequest + (int)InteractionTrackerAsyncOperationTrigger.VerticalScrollControllerRequest));

		if (interactionTrackerAsyncOperation is null)
		{
			Vector2? inertiaDecayRate = null;
			Vector2 offsetsVelocity = default;

			if (isFromHorizontalScrollController)
			{
				offsetsVelocity.X = args.OffsetVelocity;
				horizontalInertiaDecayRate = args.InertiaDecayRate;
			}
			else
			{
				offsetsVelocity.Y = args.OffsetVelocity;
				verticalInertiaDecayRate = args.InertiaDecayRate;
			}

			if (horizontalInertiaDecayRate is not null || verticalInertiaDecayRate is not null)
			{
				object inertiaDecayRateAsInsp = null;

				if (horizontalInertiaDecayRate.HasValue)
				{
					inertiaDecayRateAsInsp = new Vector2(horizontalInertiaDecayRate.Value, c_scrollPresenterDefaultInertiaDecayRate);
				}
				else
				{
					inertiaDecayRateAsInsp = new Vector2(c_scrollPresenterDefaultInertiaDecayRate, verticalInertiaDecayRate.Value);
				}

				inertiaDecayRate = inertiaDecayRateAsInsp as Vector2?;
			}

			ChangeOffsetsWithAdditionalVelocityPrivate(
				offsetsVelocity,
				Vector2.Zero /*anticipatedOffsetsChange*/,
				inertiaDecayRate,
				isFromHorizontalScrollController ? InteractionTrackerAsyncOperationTrigger.HorizontalScrollControllerRequest : InteractionTrackerAsyncOperationTrigger.VerticalScrollControllerRequest,
				out viewChangeCorrelationId);
		}
		else
		{
			// Coalesce requests
			int existingViewChangeCorrelationId = interactionTrackerAsyncOperation.GetViewChangeCorrelationId();
			ViewChangeBase viewChangeBase = interactionTrackerAsyncOperation.GetViewChangeBase();
			OffsetsChangeWithAdditionalVelocity offsetsChangeWithAdditionalVelocity = (OffsetsChangeWithAdditionalVelocity)viewChangeBase;

			Vector2 offsetsVelocity = offsetsChangeWithAdditionalVelocity.OffsetsVelocity;
			Vector2? inertiaDecayRate = offsetsChangeWithAdditionalVelocity.InertiaDecayRate;

			interactionTrackerAsyncOperation.SetIsScrollControllerRequest(isFromHorizontalScrollController);

			if (isFromHorizontalScrollController)
			{
				offsetsVelocity.X = args.OffsetVelocity;
				horizontalInertiaDecayRate = args.InertiaDecayRate;

				if (horizontalInertiaDecayRate is null)
				{
					if (inertiaDecayRate is not null)
					{
						if (inertiaDecayRate.Value.Y == c_scrollPresenterDefaultInertiaDecayRate)
						{
							offsetsChangeWithAdditionalVelocity.InertiaDecayRate = null;
						}
						else
						{
							object newInertiaDecayRateAsInsp = new Vector2(c_scrollPresenterDefaultInertiaDecayRate, inertiaDecayRate.Value.Y);
							Vector2? newInertiaDecayRate = newInertiaDecayRateAsInsp as Vector2?;

							offsetsChangeWithAdditionalVelocity.InertiaDecayRate = newInertiaDecayRate;
						}
					}
				}
				else
				{
					object newInertiaDecayRateAsInsp = null;

					if (inertiaDecayRate is null)
					{
						newInertiaDecayRateAsInsp =
							new Vector2(horizontalInertiaDecayRate.Value, c_scrollPresenterDefaultInertiaDecayRate);
					}
					else
					{
						newInertiaDecayRateAsInsp =
							new Vector2(horizontalInertiaDecayRate.Value, inertiaDecayRate.Value.Y);
					}

					Vector2? newInertiaDecayRate = newInertiaDecayRateAsInsp as Vector2?;

					offsetsChangeWithAdditionalVelocity.InertiaDecayRate = newInertiaDecayRate;
				}
			}
			else
			{
				offsetsVelocity.Y = args.OffsetVelocity;
				verticalInertiaDecayRate = args.InertiaDecayRate;

				if (verticalInertiaDecayRate is null)
				{
					if (inertiaDecayRate is not null)
					{
						if (inertiaDecayRate.Value.X == c_scrollPresenterDefaultInertiaDecayRate)
						{
							offsetsChangeWithAdditionalVelocity.InertiaDecayRate = null;
						}
						else
						{
							object newInertiaDecayRateAsInsp =
								new Vector2(inertiaDecayRate.Value.X, c_scrollPresenterDefaultInertiaDecayRate);
							Vector2? newInertiaDecayRate =
								newInertiaDecayRateAsInsp as Vector2?;

							offsetsChangeWithAdditionalVelocity.InertiaDecayRate = newInertiaDecayRate;
						}
					}
				}
				else
				{
					object newInertiaDecayRateAsInsp = null;

					if (inertiaDecayRate is null)
					{
						newInertiaDecayRateAsInsp =
							new Vector2(c_scrollPresenterDefaultInertiaDecayRate, verticalInertiaDecayRate.Value);
					}
					else
					{
						newInertiaDecayRateAsInsp =
							new Vector2(inertiaDecayRate.Value.X, verticalInertiaDecayRate.Value);
					}

					Vector2? newInertiaDecayRate = newInertiaDecayRateAsInsp as Vector2?;

					offsetsChangeWithAdditionalVelocity.InertiaDecayRate = newInertiaDecayRate;
				}
			}

			offsetsChangeWithAdditionalVelocity.OffsetsVelocity = offsetsVelocity;

			viewChangeCorrelationId = existingViewChangeCorrelationId;
		}

		if (viewChangeCorrelationId != s_noOpCorrelationId)
		{
			args.CorrelationId = viewChangeCorrelationId;
		}
	}

	void OnHorizontalSnapPointsVectorChanged(IObservableVector<ScrollSnapPointBase> sender, IVectorChangedEventArgs args)
	{
		SnapPointsVectorChangedHelper(sender, args, ref m_sortedConsolidatedHorizontalSnapPoints, ScrollPresenterDimension.HorizontalScroll);
	}

	void OnVerticalSnapPointsVectorChanged(IObservableVector<ScrollSnapPointBase> sender, IVectorChangedEventArgs args)
	{
		SnapPointsVectorChangedHelper(sender, args, ref m_sortedConsolidatedVerticalSnapPoints, ScrollPresenterDimension.VerticalScroll);
	}

	void OnZoomSnapPointsVectorChanged(IObservableVector<ZoomSnapPointBase> sender, IVectorChangedEventArgs args)
	{
		SnapPointsVectorChangedHelper(sender, args, ref m_sortedConsolidatedZoomSnapPoints, ScrollPresenterDimension.ZoomFactor);
	}

	bool SnapPointsViewportChangedHelper<T>(
		IObservableVector<T> snapPoints,
		double viewport)
	{
		bool snapPointsNeedViewportUpdates = false;

		foreach (T snapPoint in snapPoints)
		{
			SnapPointBase winrtSnapPointBase = snapPoint as SnapPointBase;
			SnapPointBase snapPointBase = winrtSnapPointBase;

			snapPointsNeedViewportUpdates |= snapPointBase.OnUpdateViewport(viewport);
		}

		return snapPointsNeedViewportUpdates;
	}

	void SnapPointsVectorChangedHelper<T>(
		IObservableVector<T> snapPoints,
		IVectorChangedEventArgs args,
		ref SortedSet<SnapPointWrapper<T>> snapPointsSet,
		ScrollPresenterDimension dimension) where T : SnapPointBase
	{
		MUX_ASSERT(snapPoints is not null);
		MUX_ASSERT(snapPointsSet is not null);

		T insertedItem = null;
		CollectionChange collectionChange = args.CollectionChange;

		if (dimension != ScrollPresenterDimension.ZoomFactor)
		{
			double viewportSize = dimension == ScrollPresenterDimension.HorizontalScroll ? m_viewportWidth : m_viewportHeight;

			if (collectionChange == CollectionChange.ItemInserted)
			{
				insertedItem = snapPoints[(int)args.Index];

				SnapPointBase winrtSnapPointBase = insertedItem as SnapPointBase;
				SnapPointBase snapPointBase = winrtSnapPointBase;

				// Newly inserted scroll snap point is provided the viewport size, for the case it's not near-aligned.
				bool snapPointNeedsViewportUpdates = snapPointBase.OnUpdateViewport(viewportSize);

				// When snapPointNeedsViewportUpdates is True, this newly inserted scroll snap point may be the first one
				// that requires viewport updates.
				if (dimension == ScrollPresenterDimension.HorizontalScroll)
				{
					m_horizontalSnapPointsNeedViewportUpdates |= snapPointNeedsViewportUpdates;
				}
				else
				{
					m_verticalSnapPointsNeedViewportUpdates |= snapPointNeedsViewportUpdates;
				}
			}
			else if (collectionChange == CollectionChange.Reset ||
				collectionChange == CollectionChange.ItemChanged)
			{
				// Globally reevaluate the need for viewport updates even for CollectionChange::ItemChanged since
				// the old item may or may not have been the sole snap point requiring viewport updates.
				bool snapPointsNeedViewportUpdates = SnapPointsViewportChangedHelper(snapPoints, viewportSize);

				if (dimension == ScrollPresenterDimension.HorizontalScroll)
				{
					m_horizontalSnapPointsNeedViewportUpdates = snapPointsNeedViewportUpdates;
				}
				else
				{
					m_verticalSnapPointsNeedViewportUpdates = snapPointsNeedViewportUpdates;
				}
			}
		}

		switch (collectionChange)
		{
			case CollectionChange.ItemInserted:
				{
					if (insertedItem is null)
					{
						insertedItem = snapPoints[(int)args.Index];
					}

					SnapPointWrapper<T> insertedSnapPointWrapper =
						new SnapPointWrapper<T>(insertedItem);

					SnapPointsVectorItemInsertedHelper(insertedSnapPointWrapper, ref snapPointsSet);
					break;
				}
			case CollectionChange.Reset:
			case CollectionChange.ItemRemoved:
			case CollectionChange.ItemChanged:
				{
					RegenerateSnapPointsSet(snapPoints, ref snapPointsSet);
					break;
				}
			default:
				throw new ArgumentException();
				//MUX_ASSERT(false);
		}

		SetupSnapPoints(ref snapPointsSet, dimension);
	}

	void SnapPointsVectorItemInsertedHelper<T>(
		SnapPointWrapper<T> insertedItem,
		ref SortedSet<SnapPointWrapper<T>> snapPointsSet) where T : SnapPointBase
	{
		if (snapPointsSet.Count == 0)
		{
			snapPointsSet.Add(insertedItem);
			return;
		}

		SnapPointBase winrtInsertedItem = insertedItem.SnapPoint as SnapPointBase;
		var snapPointsSetEnumerator = snapPointsSet.GetEnumerator();
		bool found = false;
		while (snapPointsSetEnumerator.MoveNext())
		{
			if (snapPointsSet.Comparer.Compare(snapPointsSetEnumerator.Current, insertedItem) >= 0)
			{
				found = true;
				break;
			}
		}

		var lowerBound = found ? snapPointsSetEnumerator.Current : null;

		if (lowerBound is not null)
		{
			SnapPointBase lowerSnapPoint = lowerBound.SnapPoint as SnapPointBase;

			if (lowerSnapPoint == winrtInsertedItem)
			{
				lowerBound.Combine(insertedItem);
				return;
			}
		}
		if (found && snapPointsSetEnumerator.MoveNext())
		{
			SnapPointBase upperSnapPoint = snapPointsSetEnumerator.Current.SnapPoint as SnapPointBase;

			if (upperSnapPoint == winrtInsertedItem)
			{
				lowerBound.Combine(insertedItem);
				return;
			}
		}
		snapPointsSet.Add(insertedItem);
	}

	void RegenerateSnapPointsSet<T>(
		IObservableVector<T> userVector,
		ref SortedSet<SnapPointWrapper<T>> internalSet) where T : SnapPointBase
	{
		MUX_ASSERT(internalSet is not null);

		internalSet.Clear();
		foreach (T snapPoint in userVector)
		{
			SnapPointWrapper<T> snapPointWrapper =
				new SnapPointWrapper<T>(snapPoint);

			SnapPointsVectorItemInsertedHelper(snapPointWrapper, ref internalSet);
		}
	}

	void UpdateContent(
		UIElement oldContent,
		UIElement newContent)
	{
		Children.Clear();

		UnhookContentPropertyChanged(oldContent);

		if (newContent is not null)
		{
			Children.Add(newContent);

			if (m_minPositionExpressionAnimation is not null && m_maxPositionExpressionAnimation is not null)
			{
				UpdatePositionBoundaries(newContent);
			}
			else if (m_interactionTracker is not null)
			{
				EnsurePositionBoundariesExpressionAnimations();
				SetupPositionBoundariesExpressionAnimations(newContent);
			}

			if (m_translationExpressionAnimation is not null && m_zoomFactorExpressionAnimation is not null)
			{
				UpdateTransformSource(oldContent, newContent);
			}
			else if (m_interactionTracker is not null)
			{
				EnsureTransformExpressionAnimations();
				SetupTransformExpressionAnimations(newContent);
			}

			HookContentPropertyChanged(newContent);
		}
		else
		{
			if (m_contentLayoutOffsetX != 0.0f)
			{
				m_contentLayoutOffsetX = 0.0f;
				OnContentLayoutOffsetChanged(ScrollPresenterDimension.HorizontalScroll);
			}

			if (m_contentLayoutOffsetY != 0.0f)
			{
				m_contentLayoutOffsetY = 0.0f;
				OnContentLayoutOffsetChanged(ScrollPresenterDimension.VerticalScroll);
			}

			if (m_interactionTracker is null || (m_zoomedHorizontalOffset == 0.0 && m_zoomedVerticalOffset == 0.0))
			{
				// Complete all active or delayed operations when there is no InteractionTracker, when the old content
				// was already at offsets (0,0). The ScrollToOffsets request below will result in their completion otherwise.
				CompleteInteractionTrackerOperations(
					-1 /*requestId*/,
					ScrollPresenterViewChangeResult.Interrupted /*operationResult*/,
					ScrollPresenterViewChangeResult.Ignored     /*unused priorNonAnimatedOperationsResult*/,
					ScrollPresenterViewChangeResult.Ignored     /*unused priorAnimatedOperationsResult*/,
					true  /*completeNonAnimatedOperation*/,
					true  /*completeAnimatedOperation*/,
					false /*completePriorNonAnimatedOperations*/,
					false /*completePriorAnimatedOperations*/);
			}

			if (m_interactionTracker is not null)
			{
				if (m_minPositionExpressionAnimation is not null && m_maxPositionExpressionAnimation is not null)
				{
					UpdatePositionBoundaries(null);
				}
				if (m_translationExpressionAnimation is not null && m_zoomFactorExpressionAnimation is not null)
				{
					StopTransformExpressionAnimations(oldContent, false /*forAnimationsInterruption*/);
				}
				ScrollToOffsets(0.0 /*zoomedHorizontalOffset*/, 0.0 /*zoomedVerticalOffset*/);
			}
		}
	}

	void UpdatePositionBoundaries(
		UIElement content)
	{
		MUX_ASSERT(m_minPositionExpressionAnimation is not null);
		MUX_ASSERT(m_maxPositionExpressionAnimation is not null);
		MUX_ASSERT(m_interactionTracker is not null);

		if (content is null)
		{
			Vector3 boundaryPosition = new(0.0f);

			m_interactionTracker.MinPosition = boundaryPosition;
			m_interactionTracker.MaxPosition = boundaryPosition;
		}
		else
		{
#if DEBUG
			// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, "contentSizeX", m_unzoomedExtentWidth);
			// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, "contentSizeY", m_unzoomedExtentHeight);
			// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_FLT, METH_NAME, this, "contentLayoutOffsetX", m_contentLayoutOffsetX);
			// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_FLT, METH_NAME, this, "contentLayoutOffsetY", m_contentLayoutOffsetY);
#endif

			m_minPositionExpressionAnimation.SetScalarParameter("contentSizeX", (float)m_unzoomedExtentWidth);
			m_maxPositionExpressionAnimation.SetScalarParameter("contentSizeX", (float)m_unzoomedExtentWidth);
			m_minPositionExpressionAnimation.SetScalarParameter("contentSizeY", (float)m_unzoomedExtentHeight);
			m_maxPositionExpressionAnimation.SetScalarParameter("contentSizeY", (float)m_unzoomedExtentHeight);

			m_minPositionExpressionAnimation.SetScalarParameter("contentLayoutOffsetX", m_contentLayoutOffsetX);
			m_maxPositionExpressionAnimation.SetScalarParameter("contentLayoutOffsetX", m_contentLayoutOffsetX);
			m_minPositionExpressionAnimation.SetScalarParameter("contentLayoutOffsetY", m_contentLayoutOffsetY);
			m_maxPositionExpressionAnimation.SetScalarParameter("contentLayoutOffsetY", m_contentLayoutOffsetY);

			m_interactionTracker.StartAnimation(s_minPositionSourcePropertyName, m_minPositionExpressionAnimation);
			RaiseExpressionAnimationStatusChanged(true /*isExpressionAnimationStarted*/, s_minPositionSourcePropertyName /*propertyName*/);

			m_interactionTracker.StartAnimation(s_maxPositionSourcePropertyName, m_maxPositionExpressionAnimation);
			RaiseExpressionAnimationStatusChanged(true /*isExpressionAnimationStarted*/, s_maxPositionSourcePropertyName /*propertyName*/);
		}

#if DEBUG
		DumpMinMaxPositions();
#endif // DBG
	}

	void UpdateTransformSource(
		UIElement oldContent,
		UIElement newContent)
	{
		MUX_ASSERT(m_translationExpressionAnimation is not null && m_zoomFactorExpressionAnimation is not null);
		MUX_ASSERT(m_interactionTracker is not null);

		StopTransformExpressionAnimations(oldContent, false /*forAnimationsInterruption*/);
		StartTransformExpressionAnimations(newContent, false /*forAnimationsInterruption*/);
	}

	void UpdateState(
		ScrollingInteractionState state)
	{
		if (state != ScrollingInteractionState.Idle)
		{
			// Restart the interrupted expression animations sooner than planned to visualize the new view change immediately.
			StartTranslationAndZoomFactorExpressionAnimations(true /*interruptCountdown*/);
		}

		if (state != m_state)
		{
			m_state = state;
			RaiseStateChanged();
		}
	}

	void UpdateExpressionAnimationSources()
	{
		MUX_ASSERT(m_interactionTracker is not null);
		MUX_ASSERT(m_expressionAnimationSources is not null);

		m_expressionAnimationSources.InsertVector2(s_extentSourcePropertyName, new Vector2((float)m_unzoomedExtentWidth, (float)m_unzoomedExtentHeight));
		m_expressionAnimationSources.InsertVector2(s_viewportSourcePropertyName, new Vector2((float)m_viewportWidth, (float)m_viewportHeight));
	}

	void UpdateUnzoomedExtentAndViewport(
		bool renderSizeChanged,
		double unzoomedExtentWidth,
		double unzoomedExtentHeight,
		double viewportWidth,
		double viewportHeight)
	{
#if DEBUG
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, "renderSizeChanged", renderSizeChanged);
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, "unzoomedExtentWidth", unzoomedExtentWidth);
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, "unzoomedExtentHeight", unzoomedExtentHeight);
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, "viewportWidth", viewportWidth);
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, "viewportHeight", viewportHeight);
#endif

		UIElement content = Content;
		UIElement thisAsUIE = this;
		double oldUnzoomedExtentWidth = m_unzoomedExtentWidth;
		double oldUnzoomedExtentHeight = m_unzoomedExtentHeight;
		double oldViewportWidth = m_viewportWidth;
		double oldViewportHeight = m_viewportHeight;

		MUX_ASSERT(!double.IsInfinity(unzoomedExtentWidth));
		MUX_ASSERT(!double.IsNaN(unzoomedExtentWidth));
		MUX_ASSERT(!double.IsInfinity(unzoomedExtentHeight));
		MUX_ASSERT(!double.IsNaN(unzoomedExtentHeight));

		MUX_ASSERT(!double.IsInfinity(viewportWidth));
		MUX_ASSERT(!double.IsNaN(viewportWidth));
		MUX_ASSERT(!double.IsInfinity(viewportHeight));
		MUX_ASSERT(!double.IsNaN(viewportHeight));

		MUX_ASSERT(unzoomedExtentWidth >= 0.0);
		MUX_ASSERT(unzoomedExtentHeight >= 0.0);
		MUX_ASSERT(!(content is null && unzoomedExtentWidth != 0.0));
		MUX_ASSERT(!(content is null && unzoomedExtentHeight != 0.0));

		bool horizontalExtentChanged = oldUnzoomedExtentWidth != unzoomedExtentWidth;
		bool verticalExtentChanged = oldUnzoomedExtentHeight != unzoomedExtentHeight;
		bool extentChanged = horizontalExtentChanged || verticalExtentChanged;

		bool horizontalViewportChanged = oldViewportWidth != viewportWidth;
		bool verticalViewportChanged = oldViewportHeight != viewportHeight;
		bool viewportChanged = horizontalViewportChanged || verticalViewportChanged;

		m_unzoomedExtentWidth = unzoomedExtentWidth;
		m_unzoomedExtentHeight = unzoomedExtentHeight;

		m_viewportWidth = viewportWidth;
		m_viewportHeight = viewportHeight;

		if (m_expressionAnimationSources is not null)
		{
			UpdateExpressionAnimationSources();
		}

		if ((extentChanged || renderSizeChanged) && content is not null)
		{
			OnContentSizeChanged(content);
		}

		if (extentChanged || viewportChanged)
		{
			MaximizeInteractionTrackerOperationsTicksCountdown();
			UpdateScrollAutomationPatternProperties();
		}

		if (horizontalExtentChanged || horizontalViewportChanged)
		{
			// Updating the horizontal scroll mode because GetComputedScrollMode is function of the scrollable width.
			UpdateVisualInteractionSourceMode(ScrollPresenterDimension.HorizontalScroll);

			UpdateScrollControllerValues(ScrollPresenterDimension.HorizontalScroll);
		}

		if (verticalExtentChanged || verticalViewportChanged)
		{
			// Updating the vertical scroll mode because GetComputedScrollMode is function of the scrollable height.
			UpdateVisualInteractionSourceMode(ScrollPresenterDimension.VerticalScroll);

			UpdateScrollControllerValues(ScrollPresenterDimension.VerticalScroll);
		}

		if (horizontalViewportChanged && m_horizontalSnapPoints is not null && m_horizontalSnapPointsNeedViewportUpdates)
		{
			// At least one horizontal scroll snap point is not near-aligned and is thus sensitive to the
			// viewport width. Regenerate and set up all horizontal scroll snap points.
			var horizontalSnapPoints = m_horizontalSnapPoints as IObservableVector<ScrollSnapPointBase>;
			bool horizontalSnapPointsNeedViewportUpdates = SnapPointsViewportChangedHelper(
				horizontalSnapPoints,
				m_viewportWidth);
			MUX_ASSERT(horizontalSnapPointsNeedViewportUpdates);

			RegenerateSnapPointsSet(horizontalSnapPoints, ref m_sortedConsolidatedHorizontalSnapPoints);
			SetupSnapPoints(ref m_sortedConsolidatedHorizontalSnapPoints, ScrollPresenterDimension.HorizontalScroll);
		}

		if (verticalViewportChanged && m_verticalSnapPoints is not null && m_verticalSnapPointsNeedViewportUpdates)
		{
			// At least one vertical scroll snap point is not near-aligned and is thus sensitive to the
			// viewport height. Regenerate and set up all vertical scroll snap points.
			var verticalSnapPoints = m_verticalSnapPoints as IObservableVector<ScrollSnapPointBase>;
			bool verticalSnapPointsNeedViewportUpdates = SnapPointsViewportChangedHelper(
				verticalSnapPoints,
				m_viewportHeight);
			MUX_ASSERT(verticalSnapPointsNeedViewportUpdates);

			RegenerateSnapPointsSet(verticalSnapPoints, ref m_sortedConsolidatedVerticalSnapPoints);
			SetupSnapPoints(ref m_sortedConsolidatedVerticalSnapPoints, ScrollPresenterDimension.VerticalScroll);
		}

		if (extentChanged)
		{
			RaiseExtentChanged();
		}
	}

	// Raise automation peer property change events
	void UpdateScrollAutomationPatternProperties()
	{
		if (FrameworkElementAutomationPeer.FromElement(this) is ScrollPresenterAutomationPeer scrollPresenterAutomationPeer)
		{
			scrollPresenterAutomationPeer.UpdateScrollPatternProperties();
		}
	}

	void UpdateOffset(ScrollPresenterDimension dimension, double zoomedOffset)
	{
		if (dimension == ScrollPresenterDimension.HorizontalScroll)
		{
			if (m_zoomedHorizontalOffset != zoomedOffset)
			{
#if DEBUG
				// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, L"zoomedHorizontalOffset", zoomedOffset);
#endif
				m_zoomedHorizontalOffset = zoomedOffset;
			}
		}
		else
		{
			MUX_ASSERT(dimension == ScrollPresenterDimension.VerticalScroll);
			if (m_zoomedVerticalOffset != zoomedOffset)
			{
#if DEBUG
				// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, L"zoomedVerticalOffset", zoomedOffset);
#endif
				m_zoomedVerticalOffset = zoomedOffset;
			}
		}

		// Uno-specific: Required for EVP.
		ScrollOffsets = new(m_zoomedHorizontalOffset, m_zoomedVerticalOffset);
		InvalidateViewport();
	}

	void UpdateScrollControllerIsScrollable(ScrollPresenterDimension dimension)
	{
		if (dimension == ScrollPresenterDimension.HorizontalScroll)
		{
			if (m_horizontalScrollController is not null)
			{
				m_horizontalScrollController.SetIsScrollable(ComputedHorizontalScrollMode == ScrollingScrollMode.Enabled);
			}
		}
		else
		{
			MUX_ASSERT(dimension == ScrollPresenterDimension.VerticalScroll);

			if (m_verticalScrollController is not null)
			{
				m_verticalScrollController.SetIsScrollable(ComputedVerticalScrollMode == ScrollingScrollMode.Enabled);
			}
		}
	}

	void UpdateScrollControllerValues(ScrollPresenterDimension dimension)
	{
		if (dimension == ScrollPresenterDimension.HorizontalScroll)
		{
			if (m_horizontalScrollController is not null)
			{
				m_horizontalScrollController.SetValues(
					0.0 /*minOffset*/,
					ScrollableWidth /*maxOffset*/,
					m_zoomedHorizontalOffset /*offset*/,
					ViewportWidth /*viewportLength*/);
			}
		}
		else
		{
			MUX_ASSERT(dimension == ScrollPresenterDimension.VerticalScroll);

			if (m_verticalScrollController is not null)
			{
				m_verticalScrollController.SetValues(
					0.0 /*minOffset*/,
					ScrollableHeight /*maxOffset*/,
					m_zoomedVerticalOffset /*offset*/,
					ViewportHeight /*viewportLength*/);
			}
		}
	}

	void UpdateVisualInteractionSourceMode(ScrollPresenterDimension dimension)
	{
		ScrollingScrollMode scrollMode = GetComputedScrollMode(dimension);

		if (m_scrollPresenterVisualInteractionSource is not null)
		{
			SetupVisualInteractionSourceMode(
				m_scrollPresenterVisualInteractionSource,
				dimension,
				scrollMode);

#if IsMouseWheelScrollDisabled // UNO TODO
			SetupVisualInteractionSourcePointerWheelConfig(
				m_scrollPresenterVisualInteractionSource,
				dimension,
				GetComputedMouseWheelScrollMode(dimension));
#endif
		}

		UpdateScrollControllerIsScrollable(dimension);
	}

	void UpdateManipulationRedirectionMode()
	{
		if (m_scrollPresenterVisualInteractionSource is not null)
		{
			SetupVisualInteractionSourceRedirectionMode(m_scrollPresenterVisualInteractionSource);
		}
	}

	void OnContentSizeChanged(UIElement content)
	{
		//SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		if (m_minPositionExpressionAnimation is not null && m_maxPositionExpressionAnimation is not null)
		{
			UpdatePositionBoundaries(content);
		}

		if (m_interactionTracker is not null && m_translationExpressionAnimation is not null && m_zoomFactorExpressionAnimation is not null)
		{
			SetupTransformExpressionAnimations(content);
		}
	}

	void OnViewChanged(bool horizontalOffsetChanged, bool verticalOffsetChanged)
	{
		//SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_INT_INT, METH_NAME, this, horizontalOffsetChanged, verticalOffsetChanged);
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_DBL_DBL_FLT, METH_NAME, this, m_zoomedHorizontalOffset, m_zoomedVerticalOffset, m_zoomFactor);

		if (horizontalOffsetChanged)
		{
			UpdateScrollControllerValues(ScrollPresenterDimension.HorizontalScroll);
		}

		if (verticalOffsetChanged)
		{
			UpdateScrollControllerValues(ScrollPresenterDimension.VerticalScroll);
		}

		UpdateScrollAutomationPatternProperties();

		RaiseViewChanged();
	}

	void OnContentLayoutOffsetChanged(ScrollPresenterDimension dimension)
	{
#if DEBUG
		// MUX_ASSERT(dimension == ScrollPresenterDimension.HorizontalScroll || dimension == ScrollPresenterDimension.VerticalScroll);

		// if (dimension == ScrollPresenterDimension.HorizontalScroll)
		// {
		// 	SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_FLT, METH_NAME, this, L"Horizontal", m_contentLayoutOffsetX);
		// }
		// else
		// {
		// 	SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_FLT, METH_NAME, this, L"Vertical", m_contentLayoutOffsetY);
		// }
#endif

		ScrollPresenterTestHooks globalTestHooks = ScrollPresenterTestHooks.GetGlobalTestHooks();

		if (globalTestHooks is not null)
		{
			if (dimension == ScrollPresenterDimension.HorizontalScroll)
			{
				globalTestHooks.NotifyContentLayoutOffsetXChanged(this);
			}
			else
			{
				globalTestHooks.NotifyContentLayoutOffsetYChanged(this);
			}
		}

		if (m_minPositionExpressionAnimation is not null && m_maxPositionExpressionAnimation is not null)
		{
			UIElement content = Content;

			if (content is not null)
			{
				UpdatePositionBoundaries(content);
			}
		}

		if (m_expressionAnimationSources is not null)
		{
			m_expressionAnimationSources.InsertVector2(s_offsetSourcePropertyName, new Vector2(m_contentLayoutOffsetX, m_contentLayoutOffsetY));
		}

		// Uno TODO: InteractionTracker featurs used in SetupVisualInteractionSourceCenterPointModifier are not yet supported.
		//if (m_scrollPresenterVisualInteractionSource is not null)
		//{
		//	SetupVisualInteractionSourceCenterPointModifier(
		//		m_scrollPresenterVisualInteractionSource,
		//		dimension);
		//}
	}

	void ChangeOffsetsPrivate(
		double zoomedHorizontalOffset,
		double zoomedVerticalOffset,
		ScrollPresenterViewKind offsetsKind,
		ScrollingScrollOptions options,
		BringIntoViewRequestedEventArgs bringIntoViewRequestedEventArgs,
		InteractionTrackerAsyncOperationTrigger operationTrigger,
		int existingViewChangeCorrelationId,
		out int viewChangeCorrelationId)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_DBL_DBL_STR, METH_NAME, this,
		// 	zoomedHorizontalOffset,
		// 	zoomedVerticalOffset,
		// 	TypeLogging::ScrollOptionsToString(options).c_str());
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_STR, METH_NAME, this,
		// 	TypeLogging::InteractionTrackerAsyncOperationTriggerToString(operationTrigger).c_str(),
		// 	TypeLogging::ScrollPresenterViewKindToString(offsetsKind).c_str());
#if DEBUG
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this,
		// 	L"existingViewChangeCorrelationId",
		// 	existingViewChangeCorrelationId);

		// if (bringIntoViewRequestedEventArgs)
		// {
		// 	SCROLLPRESENTER_TRACE_VERBOSE(*this, L"%s[0x%p](bringIntoViewRequestedEventArgs: AnimationDesired:%d, H/V AlignmentRatio:%lf,%lf, H/V Offset:%f,%f, TargetRect:%s, TargetElement:0x%p)\n",
		// 		METH_NAME, this,
		// 		bringIntoViewRequestedEventArgs.AnimationDesired(),
		// 		bringIntoViewRequestedEventArgs.HorizontalAlignmentRatio(), bringIntoViewRequestedEventArgs.VerticalAlignmentRatio(),
		// 		bringIntoViewRequestedEventArgs.HorizontalOffset(), bringIntoViewRequestedEventArgs.VerticalOffset(),
		// 		TypeLogging::RectToString(bringIntoViewRequestedEventArgs.TargetRect()).c_str(),
		// 		bringIntoViewRequestedEventArgs.TargetElement());
		// }
#endif

		viewChangeCorrelationId = s_noOpCorrelationId;

		ScrollingAnimationMode animationMode = options is not null ? options.AnimationMode : ScrollingScrollOptions.s_defaultAnimationMode;
		ScrollingSnapPointsMode snapPointsMode = options is not null ? options.SnapPointsMode : ScrollingScrollOptions.s_defaultSnapPointsMode;
		// Uno specific: We initialize to None. In WinUI code, this variable isn't initialized, but then there is a possibility to reach a variable read without it being assigned.
		// For example, if animationMode is Auto which is not handled in the below switch statement.
		InteractionTrackerAsyncOperationType operationType = InteractionTrackerAsyncOperationType.None;

		animationMode = GetComputedAnimationMode(animationMode);

		switch (animationMode)
		{
			case ScrollingAnimationMode.Disabled:
				{
					switch (offsetsKind)
					{
						case ScrollPresenterViewKind.Absolute:
#if ScrollPresenterViewKind_RelativeToEndOfInertiaView // UNO TODO:
					case ScrollPresenterViewKind.RelativeToEndOfInertiaView:
#endif
							{
								operationType = InteractionTrackerAsyncOperationType.TryUpdatePosition;
								break;
							}
						case ScrollPresenterViewKind.RelativeToCurrentView:
							{
								operationType = InteractionTrackerAsyncOperationType.TryUpdatePositionBy;
								break;
							}
					}
					break;
				}
			case ScrollingAnimationMode.Enabled:
				{
					switch (offsetsKind)
					{
						case ScrollPresenterViewKind.Absolute:
						case ScrollPresenterViewKind.RelativeToCurrentView:
#if ScrollPresenterViewKind_RelativeToEndOfInertiaView // UNO TODO
					case ScrollPresenterViewKind.RelativeToEndOfInertiaView:
#endif
							{
								operationType = InteractionTrackerAsyncOperationType.TryUpdatePositionWithAnimation;
								break;
							}
					}
					break;
				}
		}

		if (Content is null)
		{
			// When there is no content, skip the view change request and return -1, indicating that no action was taken.
			return;
		}

		// When the ScrollPresenter is not loaded or not set up yet, delay the offsets change request until it gets loaded.
		// OnCompositionTargetRendering will launch the delayed changes at that point.
		bool delayOperation = !IsLoadedAndSetUp();

		ScrollingScrollOptions optionsClone = null;

		// Clone the options for this request if needed. The clone or original options will be used if the operation ever gets processed.
		bool isScrollControllerRequest =
			(operationTrigger &
			(InteractionTrackerAsyncOperationTrigger.HorizontalScrollControllerRequest |
				InteractionTrackerAsyncOperationTrigger.VerticalScrollControllerRequest)) != 0;

		if (options is not null && !isScrollControllerRequest)
		{
			// Options are cloned so that they can be modified by the caller after this offsets change call without affecting the outcome of the operation.
			optionsClone = new ScrollingScrollOptions(
				animationMode,
				snapPointsMode);
		}

		if (!delayOperation)
		{
			MUX_ASSERT(m_interactionTracker is not null);

			// Prevent any existing delayed operation from being processed after this request and overriding it.
			// All delayed operations are completed with the Interrupted result.
			CompleteDelayedOperations();

			HookCompositionTargetRendering();
		}

		ViewChange offsetsChange = null;

		if ((operationTrigger & InteractionTrackerAsyncOperationTrigger.BringIntoViewRequest) != 0)
		{
			// Bring-into-view operations use a richer version of OffsetsChange which includes
			// information extracted from the BringIntoViewRequestedEventArgs instance.
			// This allows to use ComputeBringIntoViewUpdatedTargetOffsets just before invoking
			// the InteractionTracker's TryUpdatePosition.
			offsetsChange = new BringIntoViewOffsetsChange(
				this,
				zoomedHorizontalOffset,
				zoomedVerticalOffset,
				offsetsKind,
				optionsClone is not null ? optionsClone : options,
				bringIntoViewRequestedEventArgs.TargetElement,
				bringIntoViewRequestedEventArgs.TargetRect,
				bringIntoViewRequestedEventArgs.HorizontalAlignmentRatio,
				bringIntoViewRequestedEventArgs.VerticalAlignmentRatio,
				bringIntoViewRequestedEventArgs.HorizontalOffset,
				bringIntoViewRequestedEventArgs.VerticalOffset);
		}
		else
		{
			offsetsChange = new OffsetsChange(
				zoomedHorizontalOffset,
				zoomedVerticalOffset,
				offsetsKind,
				optionsClone is not null ? optionsClone : options);
		}

		InteractionTrackerAsyncOperation interactionTrackerAsyncOperation =
			new InteractionTrackerAsyncOperation(
				operationType,
				operationTrigger,
				delayOperation,
				offsetsChange);

		if (operationTrigger != InteractionTrackerAsyncOperationTrigger.DirectViewChange)
		{
			// User-triggered or bring-into-view operations are processed as quickly as possible by minimizing their TicksCountDown
			int ticksCountdown = GetInteractionTrackerOperationsTicksCountdown();

			interactionTrackerAsyncOperation.SetTicksCountdown(Math.Max(1, ticksCountdown));
		}

		m_interactionTrackerAsyncOperations.Add(interactionTrackerAsyncOperation);

		// UNO TODO: This is diverging a bit from WinUI. Make sure it doesn't cause any bugs.
		if (existingViewChangeCorrelationId != s_noOpCorrelationId)
		{
			interactionTrackerAsyncOperation.SetViewChangeCorrelationId(existingViewChangeCorrelationId);
			viewChangeCorrelationId = existingViewChangeCorrelationId;
		}
		else
		{
			m_latestViewChangeCorrelationId = GetNextViewChangeCorrelationId();
			interactionTrackerAsyncOperation.SetViewChangeCorrelationId(m_latestViewChangeCorrelationId);
			viewChangeCorrelationId = m_latestViewChangeCorrelationId;
		}
	}

	private void ChangeOffsetsWithAdditionalVelocityPrivate(
		Vector2 offsetsVelocity,
		Vector2 anticipatedOffsetsChange,
		Vector2? inertiaDecayRate,
		InteractionTrackerAsyncOperationTrigger operationTrigger,
		out int viewChangeCorrelationId)
	{
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_STR_STR, METH_NAME, this,
		// 	TypeLogging::Float2ToString(offsetsVelocity).c_str(),
		// 	TypeLogging::NullableFloat2ToString(inertiaDecayRate).c_str(),
		// 	TypeLogging::InteractionTrackerAsyncOperationTriggerToString(operationTrigger).c_str());

		viewChangeCorrelationId = s_noOpCorrelationId;

		if (Content is null)
		{
			// When there is no content, skip the view change request and return -1, indicating that no action was taken.
			return;
		}

		// When the ScrollPresenter is not loaded or not set up yet, delay the offsets change request until it gets loaded.
		// OnCompositionTargetRendering will launch the delayed changes at that point.
		bool delayOperation = !IsLoadedAndSetUp();

		ViewChangeBase offsetsChangeWithAdditionalVelocity =
			new OffsetsChangeWithAdditionalVelocity(
				offsetsVelocity, anticipatedOffsetsChange, inertiaDecayRate);

		if (!delayOperation)
		{
			MUX_ASSERT(m_interactionTracker is not null);

			// Prevent any existing delayed operation from being processed after this request and overriding it.
			// All delayed operations are completed with the Interrupted result.
			CompleteDelayedOperations();

			HookCompositionTargetRendering();
		}

		InteractionTrackerAsyncOperation interactionTrackerAsyncOperation =
			new InteractionTrackerAsyncOperation(
				InteractionTrackerAsyncOperationType.TryUpdatePositionWithAdditionalVelocity,
				operationTrigger,
				delayOperation,
				offsetsChangeWithAdditionalVelocity);

		if (operationTrigger != InteractionTrackerAsyncOperationTrigger.DirectViewChange)
		{
			// User-triggered or bring-into-view operations are processed as quickly as possible by minimizing their TicksCountDown
			int ticksCountdown = GetInteractionTrackerOperationsTicksCountdown();

			interactionTrackerAsyncOperation.SetTicksCountdown(Math.Max(1, ticksCountdown));
		}

		m_interactionTrackerAsyncOperations.Add(interactionTrackerAsyncOperation);

		m_latestViewChangeCorrelationId = GetNextViewChangeCorrelationId();
		interactionTrackerAsyncOperation.SetViewChangeCorrelationId(m_latestViewChangeCorrelationId);
		viewChangeCorrelationId = m_latestViewChangeCorrelationId;
	}

	private void ChangeZoomFactorPrivate(
		float zoomFactor,
		Vector2? centerPoint,
		ScrollPresenterViewKind zoomFactorKind,
		ScrollingZoomOptions options,
		out int viewChangeCorrelationId)
	{
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_FLT, METH_NAME, this,
		// 	TypeLogging::NullableFloat2ToString(centerPoint).c_str(),
		// 	zoomFactor);
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_STR, METH_NAME, this,
		// 	TypeLogging::ScrollPresenterViewKindToString(zoomFactorKind).c_str(),
		// 	TypeLogging::ZoomOptionsToString(options).c_str());

		viewChangeCorrelationId = s_noOpCorrelationId;

		if (Content is null)
		{
			// When there is no content, skip the view change request and return -1, indicating that no action was taken.
			return;
		}

		ScrollingAnimationMode animationMode = options is not null ? options.AnimationMode : ScrollingZoomOptions.s_defaultAnimationMode;
		ScrollingSnapPointsMode snapPointsMode = options is not null ? options.SnapPointsMode : ScrollingZoomOptions.s_defaultSnapPointsMode;
		// Uno specific: We initialize to None. In WinUI code, this variable isn't initialized, but then there is a possibility to reach a variable read without it being assigned.
		// For example, if animationMode is Auto which is not handled in the below switch statement.
		InteractionTrackerAsyncOperationType operationType = InteractionTrackerAsyncOperationType.None;

		animationMode = GetComputedAnimationMode(animationMode);

		switch (animationMode)
		{
			case ScrollingAnimationMode.Disabled:
				{
					switch (zoomFactorKind)
					{
						case ScrollPresenterViewKind.Absolute:
#if ScrollPresenterViewKind_RelativeToEndOfInertiaView // UNO TODO
						case ScrollPresenterViewKind.RelativeToEndOfInertiaView:
#endif
						case ScrollPresenterViewKind.RelativeToCurrentView:
							{
								operationType = InteractionTrackerAsyncOperationType.TryUpdateScale;
								break;
							}
					}
					break;
				}
			case ScrollingAnimationMode.Enabled:
				{
					switch (zoomFactorKind)
					{
						case ScrollPresenterViewKind.Absolute:
#if ScrollPresenterViewKind_RelativeToEndOfInertiaView // UNO TODO:
						case ScrollPresenterViewKind.RelativeToEndOfInertiaView:
#endif
						case ScrollPresenterViewKind.RelativeToCurrentView:
							{
								operationType = InteractionTrackerAsyncOperationType.TryUpdateScaleWithAnimation;
								break;
							}
					}
					break;
				}
		}

		// When the ScrollPresenter is not loaded or not set up yet (delayOperation==True), delay the zoomFactor change request until it gets loaded.
		// OnCompositionTargetRendering will launch the delayed changes at that point.
		bool delayOperation = !IsLoadedAndSetUp();

		// Set to True when workaround for RS5 InteractionTracker bug 18827625 was applied (i.e. on-going TryUpdateScaleWithAnimation operation
		// is interrupted with TryUpdateScale operation).
		//bool scaleChangeWithAnimationInterrupted = false;

		ScrollingZoomOptions optionsClone = null;

		// Clone the original options if any. The clone will be used if the operation ever gets processed.
		if (options is not null)
		{
			// Options are cloned so that they can be modified by the caller after this zoom factor change call without affecting the outcome of the operation.
			optionsClone = new ScrollingZoomOptions(
				animationMode,
				snapPointsMode);
		}

		if (!delayOperation)
		{
			MUX_ASSERT(m_interactionTracker is not null);

			// Prevent any existing delayed operation from being processed after this request and overriding it.
			// All delayed operations are completed with the Interrupted result.
			CompleteDelayedOperations();

			HookCompositionTargetRendering();
		}

		ViewChange zoomFactorChange =
			new ZoomFactorChange(
				zoomFactor,
				centerPoint,
				zoomFactorKind,
				optionsClone is not null ? optionsClone : options);

		InteractionTrackerAsyncOperation interactionTrackerAsyncOperation =
			new InteractionTrackerAsyncOperation(
				operationType,
				InteractionTrackerAsyncOperationTrigger.DirectViewChange,
				delayOperation,
				zoomFactorChange);

		m_interactionTrackerAsyncOperations.Add(interactionTrackerAsyncOperation);

		// Workaround for InteractionTracker bug 22414894 - calling TryUpdateScale after a non-animated view change during the same tick results in an incorrect position.
		// That non-animated view change needs to complete before this TryUpdateScale gets invoked.
		interactionTrackerAsyncOperation.SetRequiredOperation(GetLastNonAnimatedInteractionTrackerOperation(interactionTrackerAsyncOperation));

		m_latestViewChangeCorrelationId = GetNextViewChangeCorrelationId();
		interactionTrackerAsyncOperation.SetViewChangeCorrelationId(m_latestViewChangeCorrelationId);
		viewChangeCorrelationId = m_latestViewChangeCorrelationId;
	}

	void ChangeZoomFactorWithAdditionalVelocityPrivate(
		float zoomFactorVelocity,
		float anticipatedZoomFactorChange,
		Vector2? centerPoint,
		float? inertiaDecayRate,
		InteractionTrackerAsyncOperationTrigger operationTrigger,
		out int viewChangeCorrelationId)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_FLT_FLT, METH_NAME, this,
		// 	zoomFactorVelocity,
		// 	anticipatedZoomFactorChange);
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_STR_STR, METH_NAME, this,
		// 	TypeLogging::NullableFloat2ToString(centerPoint).c_str(),
		// 	TypeLogging::NullableFloatToString(inertiaDecayRate).c_str(),
		// 	TypeLogging::InteractionTrackerAsyncOperationTriggerToString(operationTrigger).c_str());

		viewChangeCorrelationId = s_noOpCorrelationId;

		if (Content is null)
		{
			// When there is no content, skip the view change request and return -1, indicating that no action was taken.
			return;
		}

		// When the ScrollPresenter is not loaded or not set up yet (delayOperation==True), delay the zoom factor change request until it gets loaded.
		// OnCompositionTargetRendering will launch the delayed changes at that point.
		bool delayOperation = !IsLoadedAndSetUp();

		ViewChangeBase zoomFactorChangeWithAdditionalVelocity =
			new ZoomFactorChangeWithAdditionalVelocity(
				zoomFactorVelocity, anticipatedZoomFactorChange, centerPoint, inertiaDecayRate);

		if (!delayOperation)
		{
			MUX_ASSERT(m_interactionTracker is not null);

			// Prevent any existing delayed operation from being processed after this request and overriding it.
			// All delayed operations are completed with the Interrupted result.
			CompleteDelayedOperations();

			HookCompositionTargetRendering();
		}

		InteractionTrackerAsyncOperation interactionTrackerAsyncOperation =
			new InteractionTrackerAsyncOperation(
				InteractionTrackerAsyncOperationType.TryUpdateScaleWithAdditionalVelocity,
				operationTrigger,
				delayOperation,
				zoomFactorChangeWithAdditionalVelocity);

		if (operationTrigger != InteractionTrackerAsyncOperationTrigger.DirectViewChange)
		{
			// User-triggered operations are processed as quickly as possible by minimizing their TicksCountDown
			int ticksCountdown = GetInteractionTrackerOperationsTicksCountdown();

			interactionTrackerAsyncOperation.SetTicksCountdown(Math.Max(1, ticksCountdown));
		}

		m_interactionTrackerAsyncOperations.Add(interactionTrackerAsyncOperation);

		m_latestViewChangeCorrelationId = GetNextViewChangeCorrelationId();
		interactionTrackerAsyncOperation.SetViewChangeCorrelationId(m_latestViewChangeCorrelationId);
		viewChangeCorrelationId = m_latestViewChangeCorrelationId;
	}

	void ProcessDequeuedViewChange(InteractionTrackerAsyncOperation interactionTrackerAsyncOperation)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_PTR, METH_NAME, this, interactionTrackerAsyncOperation);

		MUX_ASSERT(IsLoadedAndSetUp());
		MUX_ASSERT(!interactionTrackerAsyncOperation.IsQueued());

		ViewChangeBase viewChangeBase = interactionTrackerAsyncOperation.GetViewChangeBase();

		MUX_ASSERT(viewChangeBase is not null);

		switch (interactionTrackerAsyncOperation.GetOperationType())
		{
			case InteractionTrackerAsyncOperationType.TryUpdatePosition:
			case InteractionTrackerAsyncOperationType.TryUpdatePositionBy:
			case InteractionTrackerAsyncOperationType.TryUpdatePositionWithAnimation:
				{
					OffsetsChange offsetsChange = (OffsetsChange)viewChangeBase;

					ProcessOffsetsChange(
						interactionTrackerAsyncOperation.GetOperationTrigger() /*operationTrigger*/,
						offsetsChange,
						interactionTrackerAsyncOperation.GetViewChangeCorrelationId() /*offsetsChangeId*/,
						true /*isForAsyncOperation*/);
					break;
				}
			case InteractionTrackerAsyncOperationType.TryUpdatePositionWithAdditionalVelocity:
				{
					OffsetsChangeWithAdditionalVelocity offsetsChangeWithAdditionalVelocity = (OffsetsChangeWithAdditionalVelocity)viewChangeBase;

					ProcessOffsetsChange(
						interactionTrackerAsyncOperation.GetOperationTrigger() /*operationTrigger*/,
						offsetsChangeWithAdditionalVelocity);
					break;
				}
			case InteractionTrackerAsyncOperationType.TryUpdateScale:
			case InteractionTrackerAsyncOperationType.TryUpdateScaleWithAnimation:
				{
					ZoomFactorChange zoomFactorChange = (ZoomFactorChange)viewChangeBase;

					ProcessZoomFactorChange(
						zoomFactorChange,
						interactionTrackerAsyncOperation.GetViewChangeCorrelationId() /*zoomFactorChangeId*/);
					break;
				}
			case InteractionTrackerAsyncOperationType.TryUpdateScaleWithAdditionalVelocity:
				{
					ZoomFactorChangeWithAdditionalVelocity zoomFactorChangeWithAdditionalVelocity = (ZoomFactorChangeWithAdditionalVelocity)viewChangeBase;

					ProcessZoomFactorChange(
						interactionTrackerAsyncOperation.GetOperationTrigger() /*operationTrigger*/,
						zoomFactorChangeWithAdditionalVelocity);
					break;
				}
			default:
				{
					//MUX_ASSERT(false);
					throw new ArgumentException();
				}
		}
		interactionTrackerAsyncOperation.SetRequestId(m_latestInteractionTrackerRequest);
	}

	// Launches an InteractionTracker request to change the offsets.
	void ProcessOffsetsChange(
		InteractionTrackerAsyncOperationTrigger operationTrigger,
		OffsetsChange offsetsChange,
		int offsetsChangeCorrelationId,
		bool isForAsyncOperation)
	{
		MUX_ASSERT(m_interactionTracker is not null);
		MUX_ASSERT(offsetsChange is not null);

#if DEBUG
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_STR, METH_NAME, this,
		// 	L"operationTrigger", TypeLogging::InteractionTrackerAsyncOperationTriggerToString(operationTrigger).c_str());
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_STR, METH_NAME, this,
		// 	L"viewKind", TypeLogging::ScrollPresenterViewKindToString(offsetsChange.ViewKind()).c_str());
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this,
		// 	L"offsetsChangeCorrelationId", offsetsChangeCorrelationId);
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this,
		// 	L"isForAsyncOperation", isForAsyncOperation);
#endif

		double zoomedHorizontalOffset = offsetsChange.ZoomedHorizontalOffset;
		double zoomedVerticalOffset = offsetsChange.ZoomedVerticalOffset;
		ScrollingScrollOptions options = offsetsChange.Options() as ScrollingScrollOptions;

#if DEBUG
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_DBL, METH_NAME, this,
		// 	L"zoomedHorizontalOffset",
		// 	zoomedHorizontalOffset);
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_DBL, METH_NAME, this,
		// 	L"zoomedVerticalOffset",
		// 	zoomedVerticalOffset);
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_STR, METH_NAME, this,
		// 	L"options",
		// 	TypeLogging::ScrollOptionsToString(options).c_str());
#endif

		ScrollingAnimationMode animationMode = options is not null ? options.AnimationMode : ScrollingScrollOptions.s_defaultAnimationMode;
		ScrollingSnapPointsMode snapPointsMode = options is not null ? options.SnapPointsMode : ScrollingScrollOptions.s_defaultSnapPointsMode;

		animationMode = GetComputedAnimationMode(animationMode);

		if ((operationTrigger & InteractionTrackerAsyncOperationTrigger.BringIntoViewRequest) != 0)
		{
			if (Content is UIElement content)
			{
				BringIntoViewOffsetsChange bringIntoViewOffsetsChange = (BringIntoViewOffsetsChange)offsetsChange;

				if (bringIntoViewOffsetsChange is not null)
				{
					// The target Element may have moved within the Content since the bring-into-view operation was
					// initiated one or more ticks ago in ScrollPresenter::OnBringIntoViewRequestedHandler.
					// The target offsets are therefore re-evaluated according to the latest Element position and size.
					ComputeBringIntoViewUpdatedTargetOffsets(
						content,
						bringIntoViewOffsetsChange.Element,
						bringIntoViewOffsetsChange.ElementRect,
						snapPointsMode,
						bringIntoViewOffsetsChange.HorizontalAlignmentRatio,
						bringIntoViewOffsetsChange.VerticalAlignmentRatio,
						bringIntoViewOffsetsChange.HorizontalOffset,
						bringIntoViewOffsetsChange.VerticalOffset,
						out zoomedHorizontalOffset,
						out zoomedVerticalOffset);
				}
			}
		}

		switch (offsetsChange.ViewKind())
		{
#if ScrollPresenterViewKind_RelativeToEndOfInertiaView // UNO TODO
			case ScrollPresenterViewKind.RelativeToEndOfInertiaView:
			{
				Vector2 endOfInertiaPosition = ComputeEndOfInertiaPosition();
				zoomedHorizontalOffset += endOfInertiaPosition.x;
				zoomedVerticalOffset += endOfInertiaPosition.y;
				break;
			}
#endif
			case ScrollPresenterViewKind.RelativeToCurrentView:
				{
					if (snapPointsMode == ScrollingSnapPointsMode.Default || animationMode == ScrollingAnimationMode.Enabled)
					{
						zoomedHorizontalOffset += m_zoomedHorizontalOffset;
						zoomedVerticalOffset += m_zoomedVerticalOffset;
					}
					break;
				}
		}

		if (snapPointsMode == ScrollingSnapPointsMode.Default)
		{
			zoomedHorizontalOffset = ComputeValueAfterSnapPoints<ScrollSnapPointBase>(zoomedHorizontalOffset, m_sortedConsolidatedHorizontalSnapPoints);
			zoomedVerticalOffset = ComputeValueAfterSnapPoints<ScrollSnapPointBase>(zoomedVerticalOffset, m_sortedConsolidatedVerticalSnapPoints);
		}

#if DEBUG
		// if (m_contentLayoutOffsetX != 0.0)
		// {
		// 	SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_FLT, METH_NAME, this, L"contentLayoutOffsetX", m_contentLayoutOffsetX);
		// }

		// if (m_contentLayoutOffsetY != 0.0)
		// {
		// 	SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_FLT, METH_NAME, this, L"contentLayoutOffsetY", m_contentLayoutOffsetY);
		// }
#endif

		switch (animationMode)
		{
			case ScrollingAnimationMode.Disabled:
				{
					if (offsetsChange.ViewKind() == ScrollPresenterViewKind.RelativeToCurrentView && snapPointsMode == ScrollingSnapPointsMode.Ignore)
					{
#if DEBUG
						// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_METH_STR, METH_NAME,
						// 	this,
						// 	L"TryUpdatePositionBy",
						// 	TypeLogging::Float2ToString(float2(static_cast<float>(zoomedHorizontalOffset), static_cast<float>(zoomedVerticalOffset))).c_str());
#endif

						m_latestInteractionTrackerRequest = m_interactionTracker.TryUpdatePositionBy(
							new Vector3((float)zoomedHorizontalOffset, (float)zoomedVerticalOffset, 0.0f));
						m_lastInteractionTrackerAsyncOperationType = InteractionTrackerAsyncOperationType.TryUpdatePositionBy;
					}
					else
					{
						Vector2 targetPosition = ComputePositionFromOffsets(zoomedHorizontalOffset, zoomedVerticalOffset);

#if DEBUG
						// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_METH_STR, METH_NAME, this,
						// 	L"TryUpdatePosition", TypeLogging::Float2ToString(targetPosition).c_str());
#endif

						m_latestInteractionTrackerRequest = m_interactionTracker.TryUpdatePosition(
							new Vector3(targetPosition, 0.0f));
						m_lastInteractionTrackerAsyncOperationType = InteractionTrackerAsyncOperationType.TryUpdatePosition;
					}

					if (isForAsyncOperation)
					{
						HookCompositionTargetRendering();
					}
					break;
				}
			case ScrollingAnimationMode.Enabled:
				{
#if DEBUG
					// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_METH, METH_NAME, this, L"TryUpdatePositionWithAnimation");
#endif

					m_latestInteractionTrackerRequest = m_interactionTracker.TryUpdatePositionWithAnimation(
						GetPositionAnimation(
							zoomedHorizontalOffset,
							zoomedVerticalOffset,
							operationTrigger,
							offsetsChangeCorrelationId));
					m_lastInteractionTrackerAsyncOperationType = InteractionTrackerAsyncOperationType.TryUpdatePositionWithAnimation;
					break;
				}
		}
	}

	// Launches an InteractionTracker request to change the offsets with an additional velocity and optional scroll inertia decay rate.
	void ProcessOffsetsChange(
		InteractionTrackerAsyncOperationTrigger operationTrigger,
		OffsetsChangeWithAdditionalVelocity offsetsChangeWithAdditionalVelocity)
	{
		MUX_ASSERT(m_interactionTracker is not null);
		MUX_ASSERT(offsetsChangeWithAdditionalVelocity is not null);

		Vector2 offsetsVelocity = offsetsChangeWithAdditionalVelocity.OffsetsVelocity;
		Vector2? inertiaDecayRate = offsetsChangeWithAdditionalVelocity.InertiaDecayRate;

		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR, METH_NAME, this, TypeLogging::NullableFloat2ToString(inertiaDecayRate).c_str());

		if (operationTrigger == InteractionTrackerAsyncOperationTrigger.HorizontalScrollControllerRequest ||
			operationTrigger == InteractionTrackerAsyncOperationTrigger.VerticalScrollControllerRequest)
		{
			// Requests coming from an IScrollController implementation do not include the 'minimum inertia velocity' value of 30.0f, because that
			// concept is InteractionTracker-specific (the IScrollController interface is meant to be InteractionTracker-agnostic).
			if (m_state != ScrollingInteractionState.Inertia)
			{
				// When there is no current inertia, include that minimum velocity automatically. So the IScrollController-provided velocity is always
				// proportional to the resulting offset change.
				const float s_minimumVelocity = 30.0f;

				if (offsetsVelocity.X < 0.0f)
				{
					offsetsVelocity = new Vector2(offsetsVelocity.X - s_minimumVelocity, offsetsVelocity.Y);
				}
				else if (offsetsVelocity.X > 0.0f)
				{
					offsetsVelocity = new Vector2(offsetsVelocity.X + s_minimumVelocity, offsetsVelocity.Y);
				}

				if (offsetsVelocity.Y < 0.0f)
				{
					offsetsVelocity = new Vector2(offsetsVelocity.X, offsetsVelocity.Y - s_minimumVelocity);
				}
				else if (offsetsVelocity.Y > 0.0f)
				{
					offsetsVelocity = new Vector2(offsetsVelocity.X, offsetsVelocity.Y + s_minimumVelocity);
				}
			}
		}

		if (inertiaDecayRate.HasValue)
		{
			float horizontalInertiaDecayRate = Math.Clamp(inertiaDecayRate.Value.X, 0.0f, 1.0f);
			float verticalInertiaDecayRate = Math.Clamp(inertiaDecayRate.Value.Y, 0.0f, 1.0f);

			m_interactionTracker.PositionInertiaDecayRate = new Vector3(horizontalInertiaDecayRate, verticalInertiaDecayRate, 0.0f);
		}

#if DEBUG
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_METH_STR, METH_NAME, this,
		// 	L"TryUpdatePositionWithAdditionalVelocity", TypeLogging::Float2ToString(float2(offsetsVelocity)).c_str());
#endif

		m_latestInteractionTrackerRequest = m_interactionTracker.TryUpdatePositionWithAdditionalVelocity(
			new Vector3(offsetsVelocity, 0.0f));
		m_lastInteractionTrackerAsyncOperationType = InteractionTrackerAsyncOperationType.TryUpdatePositionWithAdditionalVelocity;
	}

	// Restores the default scroll inertia decay rate if no offset change with additional velocity operation is in progress.
	void PostProcessOffsetsChange(
		InteractionTrackerAsyncOperation interactionTrackerAsyncOperation)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_PTR, METH_NAME, this, interactionTrackerAsyncOperation);

		MUX_ASSERT(m_interactionTracker is not null);

		if (interactionTrackerAsyncOperation.GetRequestId() != m_latestInteractionTrackerRequest)
		{
			InteractionTrackerAsyncOperation latestInteractionTrackerAsyncOperation = GetInteractionTrackerOperationFromRequestId(
				m_latestInteractionTrackerRequest);
			if (latestInteractionTrackerAsyncOperation is not null &&
				latestInteractionTrackerAsyncOperation.GetOperationType() == InteractionTrackerAsyncOperationType.TryUpdatePositionWithAdditionalVelocity)
			{
				// Do not reset the scroll inertia decay rate when there is a new ongoing offset change with additional velocity
				return;
			}
		}

		m_interactionTracker.PositionInertiaDecayRate = null;
	}

	// Launches an InteractionTracker request to change the zoomFactor.
	void ProcessZoomFactorChange(
		ZoomFactorChange zoomFactorChange,
		int zoomFactorChangeCorrelationId)
	{
		MUX_ASSERT(m_interactionTracker is not null);
		MUX_ASSERT(zoomFactorChange is not null);

		float zoomFactor = zoomFactorChange.ZoomFactor();
		Vector2? nullableCenterPoint = zoomFactorChange.CenterPoint();
		ScrollPresenterViewKind viewKind = zoomFactorChange.ViewKind();
		ScrollingZoomOptions options = zoomFactorChange.Options() as ScrollingZoomOptions;

		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this,
		// 	TypeLogging::ScrollPresenterViewKindToString(viewKind).c_str(),
		// 	zoomFactorChangeCorrelationId);
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_STR_FLT, METH_NAME, this,
		// 	TypeLogging::NullableFloat2ToString(nullableCenterPoint).c_str(),
		// 	TypeLogging::ZoomOptionsToString(options).c_str(),
		// 	zoomFactor);

		Vector2 centerPoint2D = !nullableCenterPoint.HasValue ?
			new Vector2((float)(m_viewportWidth / 2.0), (float)(m_viewportHeight / 2.0)) : nullableCenterPoint.Value;
		Vector3 centerPoint = new Vector3(centerPoint2D.X - m_contentLayoutOffsetX, centerPoint2D.Y - m_contentLayoutOffsetY, 0.0f);

		switch (viewKind)
		{
#if ScrollPresenterViewKind_RelativeToEndOfInertiaView // UNO TODO
			case ScrollPresenterViewKind.RelativeToEndOfInertiaView:
			{
				zoomFactor += ComputeEndOfInertiaZoomFactor();
				break;
			}
#endif
			case ScrollPresenterViewKind.RelativeToCurrentView:
				{
					zoomFactor += m_zoomFactor;
					break;
				}
		}

		ScrollingAnimationMode animationMode = options is not null ? options.AnimationMode : ScrollingScrollOptions.s_defaultAnimationMode;
		ScrollingSnapPointsMode snapPointsMode = options is not null ? options.SnapPointsMode : ScrollingScrollOptions.s_defaultSnapPointsMode;

		animationMode = GetComputedAnimationMode(animationMode);

		if (snapPointsMode == ScrollingSnapPointsMode.Default)
		{
			zoomFactor = (float)ComputeValueAfterSnapPoints<ZoomSnapPointBase>(zoomFactor, m_sortedConsolidatedZoomSnapPoints);
		}

		switch (animationMode)
		{
			case ScrollingAnimationMode.Disabled:
				{
#if DEBUG
					// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_METH_FLT_STR, METH_NAME, this,
					// 	L"TryUpdateScale", zoomFactor, TypeLogging::Float2ToString(float2(centerPoint.x, centerPoint.y)).c_str());
#endif

					m_latestInteractionTrackerRequest = m_interactionTracker.TryUpdateScale(zoomFactor, centerPoint);
					m_lastInteractionTrackerAsyncOperationType = InteractionTrackerAsyncOperationType.TryUpdateScale;

					HookCompositionTargetRendering();
					break;
				}
			case ScrollingAnimationMode.Enabled:
				{
#if DEBUG
					// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_METH, METH_NAME, this, L"TryUpdateScaleWithAnimation");
#endif

					m_latestInteractionTrackerRequest = m_interactionTracker.TryUpdateScaleWithAnimation(
						GetZoomFactorAnimation(zoomFactor, centerPoint2D, zoomFactorChangeCorrelationId),
						centerPoint);
					m_lastInteractionTrackerAsyncOperationType = InteractionTrackerAsyncOperationType.TryUpdateScaleWithAnimation;
					break;
				}
		}
	}

	// Launches an InteractionTracker request to change the zoomFactor with an additional velocity and an optional zoomFactor inertia decay rate.
	void ProcessZoomFactorChange(
		InteractionTrackerAsyncOperationTrigger operationTrigger,
		ZoomFactorChangeWithAdditionalVelocity zoomFactorChangeWithAdditionalVelocity)
	{
		MUX_ASSERT(m_interactionTracker is not null);
		MUX_ASSERT(zoomFactorChangeWithAdditionalVelocity is not null);

		float zoomFactorVelocity = zoomFactorChangeWithAdditionalVelocity.ZoomFactorVelocity();
		float? inertiaDecayRate = zoomFactorChangeWithAdditionalVelocity.InertiaDecayRate();
		Vector2? nullableCenterPoint = zoomFactorChangeWithAdditionalVelocity.CenterPoint();

		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR, METH_NAME, this,
		// 	TypeLogging::InteractionTrackerAsyncOperationTriggerToString(operationTrigger).c_str());
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_STR_FLT, METH_NAME, this,
		// 	TypeLogging::NullableFloat2ToString(nullableCenterPoint).c_str(),
		// 	TypeLogging::NullableFloatToString(inertiaDecayRate).c_str(),
		// 	zoomFactorVelocity);

		if (inertiaDecayRate.HasValue)
		{
			float scaleInertiaDecayRate = Math.Clamp(inertiaDecayRate.Value, 0.0f, 1.0f);

			m_interactionTracker.ScaleInertiaDecayRate = scaleInertiaDecayRate;
		}

		Vector2 centerPoint2D = !nullableCenterPoint.HasValue ?
			new Vector2((float)(m_viewportWidth / 2.0), (float)(m_viewportHeight / 2.0)) : nullableCenterPoint.Value;
		Vector3 centerPoint = new Vector3(centerPoint2D.X - m_contentLayoutOffsetX, centerPoint2D.Y - m_contentLayoutOffsetY, 0.0f);

#if DEBUG
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_METH_FLT_STR, METH_NAME, this,
		// 	L"TryUpdateScaleWithAdditionalVelocity", zoomFactorVelocity, TypeLogging::Float2ToString(float2(centerPoint.x, centerPoint.y)).c_str());
#endif

		m_latestInteractionTrackerRequest = m_interactionTracker.TryUpdateScaleWithAdditionalVelocity(
			zoomFactorVelocity,
			centerPoint);
		m_lastInteractionTrackerAsyncOperationType = InteractionTrackerAsyncOperationType.TryUpdateScaleWithAdditionalVelocity;
	}

	// Restores the default zoomFactor inertia decay rate if no zoomFactor change with additional velocity operation is in progress.
	void PostProcessZoomFactorChange(
		InteractionTrackerAsyncOperation interactionTrackerAsyncOperation)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH_PTR, METH_NAME, this, interactionTrackerAsyncOperation);

		MUX_ASSERT(m_interactionTracker is not null);

		if (interactionTrackerAsyncOperation.GetRequestId() != m_latestInteractionTrackerRequest)
		{
			InteractionTrackerAsyncOperation latestInteractionTrackerAsyncOperation = GetInteractionTrackerOperationFromRequestId(
				m_latestInteractionTrackerRequest);
			if (latestInteractionTrackerAsyncOperation is not null &&
				latestInteractionTrackerAsyncOperation.GetOperationType() == InteractionTrackerAsyncOperationType.TryUpdateScaleWithAdditionalVelocity)
			{
				// Do not reset the zoomFactor inertia decay rate when there is a new ongoing zoomFactor change with additional velocity
				return;
			}
		}

		m_interactionTracker.ScaleInertiaDecayRate = null;
	}

	void CompleteViewChange(
		InteractionTrackerAsyncOperation interactionTrackerAsyncOperation,
		ScrollPresenterViewChangeResult result)
	{
		int viewChangeCorrelationId = interactionTrackerAsyncOperation.GetViewChangeCorrelationId();

		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_PTR_STR, METH_NAME, this,
		// 	interactionTrackerAsyncOperation, TypeLogging::ScrollPresenterViewChangeResultToString(result).c_str());
#if DEBUG
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this,
		// 	L"viewChangeCorrelationId", viewChangeCorrelationId);
#endif

		interactionTrackerAsyncOperation.SetIsCompleted(true);

		bool onHorizontalOffsetChangeCompleted = false;
		bool onVerticalOffsetChangeCompleted = false;

		switch ((int)interactionTrackerAsyncOperation.GetOperationTrigger())
		{
			case (int)InteractionTrackerAsyncOperationTrigger.DirectViewChange:
			case (int)InteractionTrackerAsyncOperationTrigger.BringIntoViewRequest:
				switch (interactionTrackerAsyncOperation.GetOperationType())
				{
					case InteractionTrackerAsyncOperationType.TryUpdatePosition:
					case InteractionTrackerAsyncOperationType.TryUpdatePositionBy:
					case InteractionTrackerAsyncOperationType.TryUpdatePositionWithAnimation:
					case InteractionTrackerAsyncOperationType.TryUpdatePositionWithAdditionalVelocity:
						RaiseViewChangeCompleted(true /*isForScroll*/, result, viewChangeCorrelationId);
						break;
					default:
						// Stop Translation and Scale animations if needed, to trigger rasterization of Content & avoid fuzzy text rendering for instance.
						StopTranslationAndZoomFactorExpressionAnimations();

						RaiseViewChangeCompleted(false /*isForScroll*/, result, viewChangeCorrelationId);
						break;
				}
				break;
			case (int)InteractionTrackerAsyncOperationTrigger.HorizontalScrollControllerRequest:
				onHorizontalOffsetChangeCompleted = true;
				break;
			case (int)InteractionTrackerAsyncOperationTrigger.VerticalScrollControllerRequest:
				onVerticalOffsetChangeCompleted = true;
				break;
			case (int)InteractionTrackerAsyncOperationTrigger.HorizontalScrollControllerRequest |
				(int)InteractionTrackerAsyncOperationTrigger.VerticalScrollControllerRequest:
				onHorizontalOffsetChangeCompleted = true;
				onVerticalOffsetChangeCompleted = true;
				break;
		}

		if (onHorizontalOffsetChangeCompleted && m_horizontalScrollController is not null)
		{
			m_horizontalScrollController.NotifyRequestedScrollCompleted(viewChangeCorrelationId);
		}

		if (onVerticalOffsetChangeCompleted && m_verticalScrollController is not null)
		{
			m_verticalScrollController.NotifyRequestedScrollCompleted(viewChangeCorrelationId);
		}
	}

	void CompleteInteractionTrackerOperations(
		int requestId,
		ScrollPresenterViewChangeResult operationResult,
		ScrollPresenterViewChangeResult priorNonAnimatedOperationsResult,
		ScrollPresenterViewChangeResult priorAnimatedOperationsResult,
		bool completeNonAnimatedOperation,
		bool completeAnimatedOperation,
		bool completePriorNonAnimatedOperations,
		bool completePriorAnimatedOperations)
	{
		MUX_ASSERT(requestId != 0);
		MUX_ASSERT(completeNonAnimatedOperation || completeAnimatedOperation || completePriorNonAnimatedOperations || completePriorAnimatedOperations);

		if (m_interactionTrackerAsyncOperations.Count == 0)
		{
			return;
		}

		// Uno docs: ToArray call is to avoid modifying the collection while iterating.
		foreach (var interactionTrackerAsyncOperation in m_interactionTrackerAsyncOperations.ToArray())
		{
			bool isMatch = requestId == -1 || requestId == interactionTrackerAsyncOperation.GetRequestId();
			bool isPriorMatch = requestId > interactionTrackerAsyncOperation.GetRequestId() && -1 != interactionTrackerAsyncOperation.GetRequestId();

			if ((isPriorMatch && (completePriorNonAnimatedOperations || completePriorAnimatedOperations)) ||
				(isMatch && (completeNonAnimatedOperation || completeAnimatedOperation)))
			{
				bool isOperationAnimated = interactionTrackerAsyncOperation.IsAnimated();
				bool complete =
					(isMatch && completeNonAnimatedOperation && !isOperationAnimated) ||
					(isMatch && completeAnimatedOperation && isOperationAnimated) ||
					(isPriorMatch && completePriorNonAnimatedOperations && !isOperationAnimated) ||
					(isPriorMatch && completePriorAnimatedOperations && isOperationAnimated);

				if (complete)
				{
					CompleteViewChange(
						interactionTrackerAsyncOperation,
						isMatch ? operationResult : (isOperationAnimated ? priorAnimatedOperationsResult : priorNonAnimatedOperationsResult));

					var interactionTrackerAsyncOperationRemoved = interactionTrackerAsyncOperation;

					m_interactionTrackerAsyncOperations.Remove(interactionTrackerAsyncOperationRemoved);

					switch (interactionTrackerAsyncOperationRemoved.GetOperationType())
					{
						case InteractionTrackerAsyncOperationType.TryUpdatePositionWithAdditionalVelocity:
							PostProcessOffsetsChange(interactionTrackerAsyncOperationRemoved);
							break;
						case InteractionTrackerAsyncOperationType.TryUpdateScaleWithAdditionalVelocity:
							PostProcessZoomFactorChange(interactionTrackerAsyncOperationRemoved);
							break;
					}
				}
			}
		}
	}

	void CompleteDelayedOperations()
	{
		if (m_interactionTrackerAsyncOperations.Count == 0)
		{
			return;
		}

		// SCROLLPRESENTER_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		foreach (var interactionTrackerAsyncOperation in m_interactionTrackerAsyncOperations)
		{
			if (interactionTrackerAsyncOperation.IsDelayed())
			{
				CompleteViewChange(interactionTrackerAsyncOperation, ScrollPresenterViewChangeResult.Interrupted);
				m_interactionTrackerAsyncOperations.Remove(interactionTrackerAsyncOperation);
			}
		}
	}

	// Sets the ticks countdown of the queued operations to the max value of InteractionTrackerAsyncOperation::c_queuedOperationTicks == 3.
	// Invoked when the extent or viewport size changed in order to let it propagate to the Composition thread and thus let the
	// InteractionTracker operate on the latest sizes.
	void MaximizeInteractionTrackerOperationsTicksCountdown()
	{
		if (m_interactionTrackerAsyncOperations.Count == 0)
		{
			return;
		}

		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH, METH_NAME, this);

		foreach (var interactionTrackerAsyncOperation in m_interactionTrackerAsyncOperations)
		{
			if (!interactionTrackerAsyncOperation.IsDelayed() &&
				!interactionTrackerAsyncOperation.IsCanceled() &&
				!interactionTrackerAsyncOperation.IsCompleted() &&
				interactionTrackerAsyncOperation.IsQueued())
			{
				interactionTrackerAsyncOperation.SetMaxTicksCountdown();
			}
		}
	}

	// Returns the maximum remaining ticks countdown of all queued operations.
	// Used by ScrollPresenter::ChangeOffsetsPrivate, ScrollPresenter::ChangeOffsetsWithAdditionalVelocityPrivate and ScrollPresenter::ChangeZoomFactorWithAdditionalVelocityPrivate
	// to make sure newly queued operations do not get processed before existing ones.
	int GetInteractionTrackerOperationsTicksCountdown()
	{
		int ticksCountdown = 0;

		foreach (var interactionTrackerAsyncOperation in m_interactionTrackerAsyncOperations)
		{
			if (!interactionTrackerAsyncOperation.IsCompleted() &&
				!interactionTrackerAsyncOperation.IsCanceled())
			{
				ticksCountdown = Math.Max(ticksCountdown, interactionTrackerAsyncOperation.GetTicksCountdown());
			}
		}

		return ticksCountdown;
	}

#if false // unused, even on WinUI
	int GetInteractionTrackerOperationsCount(bool includeAnimatedOperations, bool includeNonAnimatedOperations)
	{
		MUX_ASSERT(includeAnimatedOperations || includeNonAnimatedOperations);

		int operationsCount = 0;

		foreach (var interactionTrackerAsyncOperation in m_interactionTrackerAsyncOperations)
		{
			bool isOperationAnimated = interactionTrackerAsyncOperation.IsAnimated();

			if ((isOperationAnimated && includeAnimatedOperations) || (!isOperationAnimated && includeNonAnimatedOperations))
			{
				operationsCount++;
			}
		}

		return operationsCount;
	}
#endif

	InteractionTrackerAsyncOperation GetLastNonAnimatedInteractionTrackerOperation(
		InteractionTrackerAsyncOperation priorToInteractionTrackerOperation)
	{
		bool priorInteractionTrackerOperationSeen = false;

		for (int i = m_interactionTrackerAsyncOperations.Count - 1; i >= 0; i--)
		{
			var interactionTrackerAsyncOperation = m_interactionTrackerAsyncOperations[i];

			if (!priorInteractionTrackerOperationSeen && priorToInteractionTrackerOperation == interactionTrackerAsyncOperation)
			{
				priorInteractionTrackerOperationSeen = true;
			}
			else if (priorInteractionTrackerOperationSeen &&
				!interactionTrackerAsyncOperation.IsAnimated() &&
				!interactionTrackerAsyncOperation.IsCompleted() &&
				!interactionTrackerAsyncOperation.IsCanceled())
			{
				MUX_ASSERT(interactionTrackerAsyncOperation.IsDelayed() || interactionTrackerAsyncOperation.IsQueued());
				return interactionTrackerAsyncOperation;
			}
		}

		return null;
	}

	InteractionTrackerAsyncOperation GetInteractionTrackerOperationFromRequestId(int requestId)
	{
		MUX_ASSERT(requestId >= 0);

		foreach (var interactionTrackerAsyncOperation in m_interactionTrackerAsyncOperations)
		{
			if (interactionTrackerAsyncOperation.GetRequestId() == requestId)
			{
				return interactionTrackerAsyncOperation;
			}
		}

		return null;
	}

	InteractionTrackerAsyncOperation GetInteractionTrackerOperationFromKinds(
		bool isOperationTypeForOffsetsChange,
		InteractionTrackerAsyncOperationTrigger operationTrigger,
		ScrollPresenterViewKind viewKind,
		ScrollingScrollOptions options)
	{
		// Going through the existing operations from most recent to oldest, trying to find a match for the trigger, kind and options.
		for (int i = m_interactionTrackerAsyncOperations.Count - 1; i >= 0; i--)
		{
			var interactionTrackerAsyncOperation = m_interactionTrackerAsyncOperations[i];

			if (((int)(interactionTrackerAsyncOperation.GetOperationTrigger()) & (int)(operationTrigger)) == 0x00 &&
				!interactionTrackerAsyncOperation.IsCanceled())
			{
				// When a non-canceled operation with a different trigger is encountered, we bail out right away.
				return null;
			}

			ViewChangeBase viewChangeBase = interactionTrackerAsyncOperation.GetViewChangeBase();

			if (((int)(interactionTrackerAsyncOperation.GetOperationTrigger()) & (int)(operationTrigger)) == 0x00 ||
				!interactionTrackerAsyncOperation.IsQueued() ||
				interactionTrackerAsyncOperation.IsUnqueueing() ||
				interactionTrackerAsyncOperation.IsCanceled() ||
				viewChangeBase is null)
			{
				continue;
			}

			switch (interactionTrackerAsyncOperation.GetOperationType())
			{
				case InteractionTrackerAsyncOperationType.TryUpdatePosition:
				case InteractionTrackerAsyncOperationType.TryUpdatePositionBy:
				case InteractionTrackerAsyncOperationType.TryUpdatePositionWithAnimation:
					{
						if (!isOperationTypeForOffsetsChange)
						{
							continue;
						}

						ViewChange viewChange = (ViewChange)viewChangeBase;

						if (viewChange.ViewKind() != viewKind)
						{
							continue;
						}

						ScrollingScrollOptions optionsClone = viewChange.Options() as ScrollingScrollOptions;
						ScrollingAnimationMode animationMode = options is not null ? options.AnimationMode : ScrollingScrollOptions.s_defaultAnimationMode;
						ScrollingAnimationMode animationModeClone = optionsClone is not null ? optionsClone.AnimationMode : ScrollingScrollOptions.s_defaultAnimationMode;

						if (animationModeClone != animationMode)
						{
							continue;
						}

						ScrollingSnapPointsMode snapPointsMode = options is not null ? options.SnapPointsMode : ScrollingScrollOptions.s_defaultSnapPointsMode;
						ScrollingSnapPointsMode snapPointsModeClone = optionsClone is not null ? optionsClone.SnapPointsMode : ScrollingScrollOptions.s_defaultSnapPointsMode;

						if (snapPointsModeClone != snapPointsMode)
						{
							continue;
						}
						break;
					}
				case InteractionTrackerAsyncOperationType.TryUpdateScale:
				case InteractionTrackerAsyncOperationType.TryUpdateScaleWithAnimation:
					{
						if (isOperationTypeForOffsetsChange)
						{
							continue;
						}

						ViewChange viewChange = (ViewChange)viewChangeBase;

						if (viewChange.ViewKind() != viewKind)
						{
							continue;
						}

						ScrollingZoomOptions optionsClone = viewChange.Options() as ScrollingZoomOptions;
						ScrollingAnimationMode animationMode = options is not null ? options.AnimationMode : ScrollingScrollOptions.s_defaultAnimationMode;
						ScrollingAnimationMode animationModeClone = optionsClone is not null ? optionsClone.AnimationMode : ScrollingScrollOptions.s_defaultAnimationMode;

						if (animationModeClone != animationMode)
						{
							continue;
						}

						ScrollingSnapPointsMode snapPointsMode = options is not null ? options.SnapPointsMode : ScrollingScrollOptions.s_defaultSnapPointsMode;
						ScrollingSnapPointsMode snapPointsModeClone = optionsClone is not null ? optionsClone.SnapPointsMode : ScrollingScrollOptions.s_defaultSnapPointsMode;

						if (snapPointsModeClone != snapPointsMode)
						{
							continue;
						}
						break;
					}
			}

			return interactionTrackerAsyncOperation;
		}

		return null;
	}

	InteractionTrackerAsyncOperation GetInteractionTrackerOperationWithAdditionalVelocity(
		bool isOperationTypeForOffsetsChange,
		InteractionTrackerAsyncOperationTrigger operationTrigger)
	{
		foreach (var interactionTrackerAsyncOperation in m_interactionTrackerAsyncOperations)
		{
			ViewChangeBase viewChangeBase = interactionTrackerAsyncOperation.GetViewChangeBase();

			if (((int)(interactionTrackerAsyncOperation.GetOperationTrigger()) & (int)(operationTrigger)) == 0x00 ||
				!interactionTrackerAsyncOperation.IsQueued() ||
				interactionTrackerAsyncOperation.IsUnqueueing() ||
				interactionTrackerAsyncOperation.IsCanceled() ||
				viewChangeBase is null)
			{
				continue;
			}

			switch (interactionTrackerAsyncOperation.GetOperationType())
			{
				case InteractionTrackerAsyncOperationType.TryUpdatePositionWithAdditionalVelocity:
					{
						if (!isOperationTypeForOffsetsChange)
						{
							continue;
						}
						return interactionTrackerAsyncOperation;
					}
				case InteractionTrackerAsyncOperationType.TryUpdateScaleWithAdditionalVelocity:
					{
						if (isOperationTypeForOffsetsChange)
						{
							continue;
						}
						return interactionTrackerAsyncOperation;
					}
			}
		}

		return null;
	}

	InteractionTrackerInertiaRestingValue GetInertiaRestingValue<T>(
		SnapPointWrapper<T> snapPointWrapper,
		Compositor compositor,
		string target,
		string scale) where T : SnapPointBase
	{
		bool isInertiaFromImpulse = IsInertiaFromImpulse();
		InteractionTrackerInertiaRestingValue modifier = InteractionTrackerInertiaRestingValue.Create(compositor);
		ExpressionAnimation conditionExpressionAnimation = snapPointWrapper.CreateConditionalExpression(m_interactionTracker, target, scale, isInertiaFromImpulse);
		ExpressionAnimation restingPointExpressionAnimation = snapPointWrapper.CreateRestingPointExpression(m_interactionTracker, target, scale, isInertiaFromImpulse);

		modifier.Condition = conditionExpressionAnimation;
		modifier.RestingValue = restingPointExpressionAnimation;

		return modifier;
	}

	// Relies on InteractionTracker.IsInertiaFromImpulse starting with RS5,
	// returns the replacement field m_isInertiaFromImpulse otherwise.
	bool IsInertiaFromImpulse()
	{
		//if (m_interactionTracker is IInteractionTracker4 interactionTracker4)
		//{
		//	return interactionTracker4.IsInertiaFromImpulse();
		//}
		//else
		{
			return m_isInertiaFromImpulse;
		}
	}

	bool IsLoadedAndSetUp()
	{
		return IsLoaded && m_interactionTracker is not null;
	}

	bool IsInputKindIgnored(ScrollingInputKinds inputKind)
	{
		return (IgnoredInputKinds & inputKind) == inputKind;
	}

	void HookCompositionTargetRendering()
	{
		if (m_renderingRevoker.Disposable is null)
		{
			// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
			Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += OnCompositionTargetRendering;
			m_renderingRevoker.Disposable = Disposable.Create(() => Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= OnCompositionTargetRendering);
		}
	}

	void UnhookCompositionTargetRendering()
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
		if (m_renderingRevoker.Disposable is not null)
		{
			m_renderingRevoker.Disposable = null;
		}
	}

	void HookScrollPresenterEvents()
	{
		if (m_loadedRevoker.Disposable is null)
		{
			Loaded += OnLoaded;
			m_loadedRevoker.Disposable = Disposable.Create(() => Loaded -= OnLoaded);
		}

		if (m_unloadedRevoker.Disposable is null)
		{
			Unloaded += OnUnloaded;
			m_unloadedRevoker.Disposable = Disposable.Create(() => Unloaded -= OnUnloaded);
		}

		if (m_bringIntoViewRequestedRevoker.Disposable is null)
		{
			BringIntoViewRequested += OnBringIntoViewRequestedHandler;
			m_bringIntoViewRequestedRevoker.Disposable = Disposable.Create(() => BringIntoViewRequested -= OnBringIntoViewRequestedHandler);
		}

		if (m_pointerPressedEventHandler is null)
		{
			m_pointerPressedEventHandler = new PointerEventHandler(OnPointerPressed);
			MUX_ASSERT(m_pointerPressedEventHandler is not null);
			AddHandler(UIElement.PointerPressedEvent, m_pointerPressedEventHandler, true /*handledEventsToo*/);
		}
	}

	void UnhookScrollPresenterEvents()
	{
		m_loadedRevoker.Disposable = null;
		m_unloadedRevoker.Disposable = null;
		m_bringIntoViewRequestedRevoker.Disposable = null;

		if (m_pointerPressedEventHandler is not null)
		{
			RemoveHandler(UIElement.PointerPressedEvent, m_pointerPressedEventHandler);
			m_pointerPressedEventHandler = null;
		}
	}

	void HookContentPropertyChanged(
		UIElement content)
	{
		if (content is not null)
		{
			if (content is FrameworkElement contentAsFE)
			{
				if (m_contentMinWidthChangedRevoker.Disposable is null)
				{
					long token = contentAsFE.RegisterPropertyChangedCallback(FrameworkElement.MinWidthProperty, OnContentPropertyChanged);
					m_contentMinWidthChangedRevoker.Disposable = Disposable.Create(() => contentAsFE.UnregisterPropertyChangedCallback(FrameworkElement.MinWidthProperty, token));
				}
				if (m_contentWidthChangedRevoker.Disposable is null)
				{
					long token = contentAsFE.RegisterPropertyChangedCallback(FrameworkElement.WidthProperty, OnContentPropertyChanged);
					m_contentWidthChangedRevoker.Disposable = Disposable.Create(() => contentAsFE.UnregisterPropertyChangedCallback(FrameworkElement.WidthProperty, token));
				}
				if (m_contentMaxWidthChangedRevoker.Disposable is null)
				{
					long token = contentAsFE.RegisterPropertyChangedCallback(FrameworkElement.MaxWidthProperty, OnContentPropertyChanged);
					m_contentMaxWidthChangedRevoker.Disposable = Disposable.Create(() => contentAsFE.UnregisterPropertyChangedCallback(FrameworkElement.MaxWidthProperty, token));
				}
				if (m_contentMinHeightChangedRevoker.Disposable is null)
				{
					long token = contentAsFE.RegisterPropertyChangedCallback(FrameworkElement.MinHeightProperty, OnContentPropertyChanged);
					m_contentMinHeightChangedRevoker.Disposable = Disposable.Create(() => contentAsFE.UnregisterPropertyChangedCallback(FrameworkElement.MinHeightProperty, token));
				}
				if (m_contentHeightChangedRevoker.Disposable is null)
				{
					long token = contentAsFE.RegisterPropertyChangedCallback(FrameworkElement.HeightProperty, OnContentPropertyChanged);
					m_contentHeightChangedRevoker.Disposable = Disposable.Create(() => contentAsFE.UnregisterPropertyChangedCallback(FrameworkElement.HeightProperty, token));
				}
				if (m_contentMaxHeightChangedRevoker.Disposable is null)
				{
					long token = contentAsFE.RegisterPropertyChangedCallback(FrameworkElement.MaxHeightProperty, OnContentPropertyChanged);
					m_contentMaxHeightChangedRevoker.Disposable = Disposable.Create(() => contentAsFE.UnregisterPropertyChangedCallback(FrameworkElement.MaxHeightProperty, token));
				}
				if (m_contentHorizontalAlignmentChangedRevoker.Disposable is null)
				{
					long token = contentAsFE.RegisterPropertyChangedCallback(FrameworkElement.HorizontalAlignmentProperty, OnContentPropertyChanged);
					m_contentHorizontalAlignmentChangedRevoker.Disposable = Disposable.Create(() => contentAsFE.UnregisterPropertyChangedCallback(FrameworkElement.HorizontalAlignmentProperty, token));
				}
				if (m_contentVerticalAlignmentChangedRevoker.Disposable is null)
				{
					long token = contentAsFE.RegisterPropertyChangedCallback(FrameworkElement.VerticalAlignmentProperty, OnContentPropertyChanged);
					m_contentVerticalAlignmentChangedRevoker.Disposable = Disposable.Create(() => contentAsFE.UnregisterPropertyChangedCallback(FrameworkElement.VerticalAlignmentProperty, token));
				}
			}
		}
	}

	void UnhookContentPropertyChanged(
		UIElement content)
	{
		if (content is not null)
		{
			FrameworkElement contentAsFE = content as FrameworkElement;

			if (contentAsFE is not null)
			{
				m_contentMinWidthChangedRevoker.Disposable = null;
				m_contentWidthChangedRevoker.Disposable = null;
				m_contentMaxWidthChangedRevoker.Disposable = null;
				m_contentMinHeightChangedRevoker.Disposable = null;
				m_contentHeightChangedRevoker.Disposable = null;
				m_contentMaxHeightChangedRevoker.Disposable = null;
				m_contentHorizontalAlignmentChangedRevoker.Disposable = null;
				m_contentVerticalAlignmentChangedRevoker.Disposable = null;
			}
		}
	}

	void HookHorizontalScrollControllerEvents(
		IScrollController horizontalScrollController)
	{
		MUX_ASSERT(horizontalScrollController is not null);

		if (m_horizontalScrollControllerScrollToRequestedRevoker.Disposable is null)
		{
			horizontalScrollController.ScrollToRequested += OnScrollControllerScrollToRequested;
			m_horizontalScrollControllerScrollToRequestedRevoker.Disposable = Disposable.Create(() => horizontalScrollController.ScrollToRequested -= OnScrollControllerScrollToRequested);
		}

		if (m_horizontalScrollControllerScrollByRequestedRevoker.Disposable is null)
		{
			horizontalScrollController.ScrollByRequested += OnScrollControllerScrollByRequested;
			m_horizontalScrollControllerScrollByRequestedRevoker.Disposable = Disposable.Create(() => horizontalScrollController.ScrollByRequested -= OnScrollControllerScrollByRequested);
		}

		if (m_horizontalScrollControllerAddScrollVelocityRequestedRevoker.Disposable is null)
		{
			horizontalScrollController.AddScrollVelocityRequested += OnScrollControllerAddScrollVelocityRequested;
			m_horizontalScrollControllerAddScrollVelocityRequestedRevoker.Disposable = Disposable.Create(() => horizontalScrollController.AddScrollVelocityRequested -= OnScrollControllerAddScrollVelocityRequested);
		}
	}

	void HookHorizontalScrollControllerPanningInfoEvents(
		IScrollControllerPanningInfo horizontalScrollControllerPanningInfo,
		bool hasInteractionSource)
	{
		MUX_ASSERT(horizontalScrollControllerPanningInfo is not null);

		if (hasInteractionSource)
		{
			HookHorizontalScrollControllerInteractionSourceEvents(horizontalScrollControllerPanningInfo);
		}

		if (m_horizontalScrollControllerPanningInfoChangedRevoker.Disposable is null)
		{
			horizontalScrollControllerPanningInfo.Changed += OnScrollControllerPanningInfoChanged;
			m_horizontalScrollControllerPanningInfoChangedRevoker.Disposable = Disposable.Create(() => horizontalScrollControllerPanningInfo.Changed -= OnScrollControllerPanningInfoChanged);
		}
	}

	void HookHorizontalScrollControllerInteractionSourceEvents(
		IScrollControllerPanningInfo horizontalScrollControllerPanningInfo)
	{
		MUX_ASSERT(horizontalScrollControllerPanningInfo is not null);

		if (m_horizontalScrollControllerPanningInfoPanRequestedRevoker.Disposable is null)
		{
			horizontalScrollControllerPanningInfo.PanRequested += OnScrollControllerPanningInfoPanRequested;
			m_horizontalScrollControllerPanningInfoPanRequestedRevoker.Disposable = Disposable.Create(() => horizontalScrollControllerPanningInfo.PanRequested -= OnScrollControllerPanningInfoPanRequested);
		}
	}

	void HookVerticalScrollControllerEvents(
		IScrollController verticalScrollController)
	{
		MUX_ASSERT(verticalScrollController is not null);

		if (m_verticalScrollControllerScrollToRequestedRevoker.Disposable is null)
		{
			verticalScrollController.ScrollToRequested += OnScrollControllerScrollToRequested;
			m_verticalScrollControllerScrollToRequestedRevoker.Disposable = Disposable.Create(() => verticalScrollController.ScrollToRequested -= OnScrollControllerScrollToRequested);
		}

		if (m_verticalScrollControllerScrollByRequestedRevoker.Disposable is null)
		{
			verticalScrollController.ScrollByRequested += OnScrollControllerScrollByRequested;
			m_verticalScrollControllerScrollByRequestedRevoker.Disposable = Disposable.Create(() => verticalScrollController.ScrollByRequested -= OnScrollControllerScrollByRequested);
		}

		if (m_verticalScrollControllerAddScrollVelocityRequestedRevoker.Disposable is null)
		{
			verticalScrollController.AddScrollVelocityRequested += OnScrollControllerAddScrollVelocityRequested;
			m_verticalScrollControllerAddScrollVelocityRequestedRevoker.Disposable = Disposable.Create(() => verticalScrollController.AddScrollVelocityRequested -= OnScrollControllerAddScrollVelocityRequested);
		}
	}

	void HookVerticalScrollControllerPanningInfoEvents(
		IScrollControllerPanningInfo verticalScrollControllerPanningInfo,
		bool hasInteractionSource)
	{
		MUX_ASSERT(verticalScrollControllerPanningInfo is not null);

		if (hasInteractionSource)
		{
			HookVerticalScrollControllerInteractionSourceEvents(verticalScrollControllerPanningInfo);
		}

		if (m_verticalScrollControllerPanningInfoChangedRevoker.Disposable is null)
		{
			verticalScrollControllerPanningInfo.Changed += OnScrollControllerPanningInfoChanged;
			m_verticalScrollControllerPanningInfoChangedRevoker.Disposable = Disposable.Create(() => verticalScrollControllerPanningInfo.Changed -= OnScrollControllerPanningInfoChanged);
		}
	}

	void HookVerticalScrollControllerInteractionSourceEvents(
		IScrollControllerPanningInfo verticalScrollControllerPanningInfo)
	{
		MUX_ASSERT(verticalScrollControllerPanningInfo is not null);

		if (m_verticalScrollControllerPanningInfoPanRequestedRevoker.Disposable is null)
		{
			verticalScrollControllerPanningInfo.PanRequested += OnScrollControllerPanningInfoPanRequested;
			m_verticalScrollControllerPanningInfoPanRequestedRevoker.Disposable = Disposable.Create(() => verticalScrollControllerPanningInfo.PanRequested -= OnScrollControllerPanningInfoPanRequested);
		}
	}

	void UnhookHorizontalScrollControllerEvents()
	{
		m_horizontalScrollControllerScrollToRequestedRevoker.Disposable = null;
		m_horizontalScrollControllerScrollByRequestedRevoker.Disposable = null;
		m_horizontalScrollControllerAddScrollVelocityRequestedRevoker.Disposable = null;
	}

	void UnhookHorizontalScrollControllerPanningInfoEvents()
	{
		m_horizontalScrollControllerPanningInfoChangedRevoker.Disposable = null;
		m_horizontalScrollControllerPanningInfoPanRequestedRevoker.Disposable = null;
	}

	void UnhookVerticalScrollControllerEvents()
	{
		m_verticalScrollControllerScrollToRequestedRevoker.Disposable = null;
		m_verticalScrollControllerScrollByRequestedRevoker.Disposable = null;
		m_verticalScrollControllerAddScrollVelocityRequestedRevoker.Disposable = null;
	}

	void UnhookVerticalScrollControllerPanningInfoEvents()
	{
		m_verticalScrollControllerPanningInfoChangedRevoker.Disposable = null;
		m_verticalScrollControllerPanningInfoPanRequestedRevoker.Disposable = null;
	}

	void RaiseInteractionSourcesChanged()
	{
		ScrollPresenterTestHooks globalTestHooks = ScrollPresenterTestHooks.GetGlobalTestHooks();

		if (globalTestHooks is not null && ScrollPresenterTestHooks.AreInteractionSourcesNotificationsRaised)
		{
			globalTestHooks.NotifyInteractionSourcesChanged(this, m_interactionTracker.InteractionSources);
		}
	}

	void RaiseExpressionAnimationStatusChanged(
		bool isExpressionAnimationStarted,
		string propertyName)
	{
		ScrollPresenterTestHooks globalTestHooks = ScrollPresenterTestHooks.GetGlobalTestHooks();

		if (globalTestHooks is not null && ScrollPresenterTestHooks.AreExpressionAnimationStatusNotificationsRaised)
		{
			globalTestHooks.NotifyExpressionAnimationStatusChanged(this, isExpressionAnimationStarted, propertyName);
		}
	}

	void RaiseExtentChanged()
	{
		ExtentChanged?.Invoke(this, null);
	}

	void RaiseStateChanged()
	{
		StateChanged?.Invoke(this, null);
	}

	void RaiseViewChanged()
	{
		ViewChanged?.Invoke(this, null);

		InvalidateViewport();
	}

	CompositionAnimation RaiseScrollAnimationStarting(
		Vector3KeyFrameAnimation positionAnimation,
		Vector2 startPosition,
		Vector2 endPosition,
		int offsetsChangeCorrelationId)
	{
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_INT, METH_NAME, this, offsetsChangeCorrelationId);
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_FLT_FLT_FLT_FLT, METH_NAME, this,
		// 	startPosition.x, startPosition.y,
		// 	endPosition.x, endPosition.y);

		if (ScrollAnimationStarting is not null)
		{
			var scrollAnimationStartingEventArgs = new ScrollingScrollAnimationStartingEventArgs();

			if (offsetsChangeCorrelationId != s_noOpCorrelationId)
			{
				scrollAnimationStartingEventArgs.CorrelationId = offsetsChangeCorrelationId;
			}

			scrollAnimationStartingEventArgs.Animation = positionAnimation;
			scrollAnimationStartingEventArgs.StartPosition = startPosition;
			scrollAnimationStartingEventArgs.EndPosition = endPosition;
			ScrollAnimationStarting.Invoke(this, scrollAnimationStartingEventArgs);
			return scrollAnimationStartingEventArgs.Animation;
		}
		else
		{
			return positionAnimation;
		}
	}

	CompositionAnimation RaiseZoomAnimationStarting(
		ScalarKeyFrameAnimation zoomFactorAnimation,
		float endZoomFactor,
		Vector2 centerPoint,
		int zoomFactorChangeCorrelationId)
	{
		// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_FLT_FLT_STR_INT, METH_NAME, this,
		// 	m_zoomFactor,
		// 	endZoomFactor,
		// 	TypeLogging::Float2ToString(centerPoint).c_str(),
		// 	zoomFactorChangeCorrelationId);

		if (ZoomAnimationStarting is not null)
		{
			var zoomAnimationStartingEventArgs = new ScrollingZoomAnimationStartingEventArgs();

			if (zoomFactorChangeCorrelationId != s_noOpCorrelationId)
			{
				zoomAnimationStartingEventArgs.CorrelationId = zoomFactorChangeCorrelationId;
			}

			zoomAnimationStartingEventArgs.Animation = zoomFactorAnimation;
			zoomAnimationStartingEventArgs.CenterPoint = centerPoint;
			zoomAnimationStartingEventArgs.StartZoomFactor = m_zoomFactor;
			zoomAnimationStartingEventArgs.EndZoomFactor = endZoomFactor;
			ZoomAnimationStarting.Invoke(this, zoomAnimationStartingEventArgs);
			return zoomAnimationStartingEventArgs.Animation;
		}
		else
		{
			return zoomFactorAnimation;
		}
	}

	void RaiseViewChangeCompleted(
		bool isForScroll,
		ScrollPresenterViewChangeResult result,
		int viewChangeCorrelationId)
	{
		if (viewChangeCorrelationId != 0)
		{
			if (isForScroll && ScrollCompleted is not null)
			{
				// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this,
				// 	TypeLogging::ScrollPresenterViewChangeResultToString(result).c_str(),
				// 	viewChangeCorrelationId);

				var scrollCompletedEventArgs = new ScrollingScrollCompletedEventArgs();

				scrollCompletedEventArgs.Result = result;
				scrollCompletedEventArgs.CorrelationId = viewChangeCorrelationId;
				ScrollCompleted.Invoke(this, scrollCompletedEventArgs);
			}
			else if (!isForScroll && ZoomCompleted is not null)
			{
				// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this,
				// 	TypeLogging::ScrollPresenterViewChangeResultToString(result).c_str(),
				// 	viewChangeCorrelationId);

				var zoomCompletedEventArgs = new ScrollingZoomCompletedEventArgs();

				zoomCompletedEventArgs.Result = result;
				zoomCompletedEventArgs.CorrelationId = viewChangeCorrelationId;
				ZoomCompleted.Invoke(this, zoomCompletedEventArgs);
			}
		}

		InvalidateViewport();
	}

	// Returns False when ScrollingBringingIntoViewEventArgs.Cancel is set to True to skip the operation.
	bool RaiseBringingIntoView(
		double targetZoomedHorizontalOffset,
		double targetZoomedVerticalOffset,
		BringIntoViewRequestedEventArgs requestEventArgs,
		int offsetsChangeCorrelationId,
		ref ScrollingSnapPointsMode snapPointsMode)
	{
		if (BringingIntoView is not null)
		{
			// SCROLLPRESENTER_TRACE_INFO(*this, TRACE_MSG_METH, METH_NAME, this);

			var bringingIntoViewEventArgs = new ScrollingBringingIntoViewEventArgs();

			bringingIntoViewEventArgs.SnapPointsMode = snapPointsMode;
			bringingIntoViewEventArgs.CorrelationId = offsetsChangeCorrelationId;
			bringingIntoViewEventArgs.RequestEventArgs = requestEventArgs;
			bringingIntoViewEventArgs.TargetOffsets(targetZoomedHorizontalOffset, targetZoomedVerticalOffset);

			BringingIntoView.Invoke(this, bringingIntoViewEventArgs);
			snapPointsMode = bringingIntoViewEventArgs.SnapPointsMode;
			return !bringingIntoViewEventArgs.Cancel;
		}
		return true;
	}

#if DEBUG
	private void DumpMinMaxPositions()
	{
		MUX_ASSERT(m_interactionTracker is not null);

		UIElement content = Content;

		if (content is null)
		{
			// Min/MaxPosition == (0, 0)
			return;
		}

		Visual scrollPresenterVisual = ElementCompositionPreview.GetElementVisual(this);
		FrameworkElement contentAsFE = content as FrameworkElement;
		float minPosX = 0.0f;
		float minPosY = 0.0f;
		float extentWidth = (float)m_unzoomedExtentWidth;
		float extentHeight = (float)m_unzoomedExtentHeight;

		if (contentAsFE is not null)
		{
			if (contentAsFE.HorizontalAlignment == HorizontalAlignment.Center ||
				contentAsFE.HorizontalAlignment == HorizontalAlignment.Stretch)
			{
				minPosX = Math.Min(0.0f, (extentWidth * m_interactionTracker.Scale - scrollPresenterVisual.Size.X) / 2.0f);
			}
			else if (contentAsFE.HorizontalAlignment == HorizontalAlignment.Right)
			{
				minPosX = Math.Min(0.0f, extentWidth * m_interactionTracker.Scale - scrollPresenterVisual.Size.X);
			}

			if (contentAsFE.VerticalAlignment == VerticalAlignment.Center ||
				contentAsFE.VerticalAlignment == VerticalAlignment.Stretch)
			{
				minPosY = Math.Min(0.0f, (extentHeight * m_interactionTracker.Scale - scrollPresenterVisual.Size.Y) / 2.0f);
			}
			else if (contentAsFE.VerticalAlignment == VerticalAlignment.Bottom)
			{
				minPosY = Math.Min(0.0f, extentHeight * m_interactionTracker.Scale - scrollPresenterVisual.Size.Y);
			}
		}

		float maxPosX = Math.Max(0.0f, extentWidth * m_interactionTracker.Scale - scrollPresenterVisual.Size.X);
		float maxPosY = Math.Max(0.0f, extentHeight * m_interactionTracker.Scale - scrollPresenterVisual.Size.Y);

		if (contentAsFE is not null)
		{
			if (contentAsFE.HorizontalAlignment == HorizontalAlignment.Center ||
				contentAsFE.HorizontalAlignment == HorizontalAlignment.Stretch)
			{
				maxPosX = (extentWidth * m_interactionTracker.Scale - scrollPresenterVisual.Size.X) >= 0 ?
					(extentWidth * m_interactionTracker.Scale - scrollPresenterVisual.Size.X) : (extentWidth * m_interactionTracker.Scale - scrollPresenterVisual.Size.X) / 2.0f;
			}
			else if (contentAsFE.HorizontalAlignment == HorizontalAlignment.Right)
			{
				maxPosX = extentWidth * m_interactionTracker.Scale - scrollPresenterVisual.Size.X;
			}

			if (contentAsFE.VerticalAlignment == VerticalAlignment.Center ||
				contentAsFE.VerticalAlignment == VerticalAlignment.Stretch)
			{
				maxPosY = (extentHeight * m_interactionTracker.Scale - scrollPresenterVisual.Size.Y) >= 0 ?
					(extentHeight * m_interactionTracker.Scale - scrollPresenterVisual.Size.Y) : (extentHeight * m_interactionTracker.Scale - scrollPresenterVisual.Size.Y) / 2.0f;
			}
			else if (contentAsFE.VerticalAlignment == VerticalAlignment.Bottom)
			{
				maxPosY = extentHeight * m_interactionTracker.Scale - scrollPresenterVisual.Size.Y;
			}
		}
	}
#endif // DBG

#if false

	private string DependencyPropertyToString(DependencyProperty dependencyProperty)
	{
		if (dependencyProperty == ContentProperty)
		{
			return "Content";
		}
		else if (dependencyProperty == BackgroundProperty)
		{
			return "Background";
		}
		else if (dependencyProperty == ContentOrientationProperty)
		{
			return "ContentOrientation";
		}
		else if (dependencyProperty == VerticalScrollChainModeProperty)
		{
			return "VerticalScrollChainMode";
		}
		else if (dependencyProperty == ZoomChainModeProperty)
		{
			return "ZoomChainMode";
		}
		else if (dependencyProperty == HorizontalScrollRailModeProperty)
		{
			return "HorizontalScrollRailMode";
		}
		else if (dependencyProperty == VerticalScrollRailModeProperty)
		{
			return "VerticalScrollRailMode";
		}
		else if (dependencyProperty == HorizontalScrollModeProperty)
		{
			return "HorizontalScrollMode";
		}
		else if (dependencyProperty == VerticalScrollModeProperty)
		{
			return "VerticalScrollMode";
		}
		else if (dependencyProperty == ComputedHorizontalScrollModeProperty)
		{
			return "ComputedHorizontalScrollMode";
		}
		else if (dependencyProperty == ComputedVerticalScrollModeProperty)
		{
			return "ComputedVerticalScrollMode";
		}
		else if (dependencyProperty == ZoomModeProperty)
		{
			return "ZoomMode";
		}
		else if (dependencyProperty == IgnoredInputKindsProperty)
		{
			return "IgnoredInputKinds";
		}
		else if (dependencyProperty == MinZoomFactorProperty)
		{
			return "MinZoomFactor";
		}
		else if (dependencyProperty == MaxZoomFactorProperty)
		{
			return "MaxZoomFactor";
		}
		else if (dependencyProperty == HorizontalAnchorRatioProperty)
		{
			return "HorizontalAnchorRatio";
		}
		else if (dependencyProperty == VerticalAnchorRatioProperty)
		{
			return "VerticalAnchorRatio";
		}
		else
		{
			return "UNKNOWN";
		}
	}

#endif
}
