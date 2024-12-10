// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.UI.Xaml.Controls.Primitives;

using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

namespace Windows.UI.Xaml.Controls;

internal enum InteractionTrackerAsyncOperationType
{
	None,
	TryUpdatePosition,
	TryUpdatePositionBy,
	TryUpdatePositionWithAnimation,
	TryUpdatePositionWithAdditionalVelocity,
	TryUpdateScale,
	TryUpdateScaleWithAnimation,
	TryUpdateScaleWithAdditionalVelocity,
}

internal enum InteractionTrackerAsyncOperationTrigger
{
	// Operation is triggered by a direct call to ScrollPresenter's ScrollTo/ScrollBy/AddScrollVelocity or ZoomTo/ZoomBy/AddZoomVelocity
	DirectViewChange = 0x01,
	// Operation is triggered by the horizontal IScrollController.
	HorizontalScrollControllerRequest = 0x02,
	// Operation is triggered by the vertical IScrollController.
	VerticalScrollControllerRequest = 0x04,
	// Operation is triggered by the UIElement.BringIntoViewRequested event handler.
	BringIntoViewRequest = 0x08
}

internal sealed partial class InteractionTrackerAsyncOperation
{
	// Used as a workaround for InteractionTracker bug "12465209 - InteractionTracker remains silent when calling TryUpdatePosition with the current position":
	// Maximum number of UI thread ticks processed while waiting for non-animated operations to complete.
	private const int c_maxNonAnimatedOperationTicks = 10;

	// Number of UI thread ticks elapsed before a queued operation gets processed to allow any pending size
	// changes to be propagated to the InteractionTracker.
	private const int c_queuedOperationTicks = 3;

	internal int GetViewChangeCorrelationId()
	{
		return m_viewChangeCorrelationId;
	}

	internal void SetViewChangeCorrelationId(int viewChangeCorrelationId)
	{
		if (m_viewChangeCorrelationId != viewChangeCorrelationId)
		{
			// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_INT, METH_NAME, this, viewChangeCorrelationId);

			m_viewChangeCorrelationId = viewChangeCorrelationId;
		}
	}

	internal bool IsAnimated()
	{
		switch (m_operationType)
		{
			case InteractionTrackerAsyncOperationType.TryUpdatePosition:
			case InteractionTrackerAsyncOperationType.TryUpdatePositionBy:
			case InteractionTrackerAsyncOperationType.TryUpdateScale:
				return false;
		}
		return true;
	}

	internal bool IsCanceled()
	{
		return m_isCanceled;
	}

#if false
	void SetIsCanceled(bool isCanceled)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_INT, METH_NAME, this, isCanceled);

		m_isCanceled = isCanceled;
	}
#endif

	internal bool IsDelayed()
	{
		return m_isDelayed;
	}

	internal void SetIsDelayed(bool isDelayed)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_INT, METH_NAME, this, isDelayed);

		m_isDelayed = isDelayed;
	}

	internal bool IsQueued()
	{
		return m_preProcessingTicksCountdown > 0;
	}

	internal bool IsUnqueueing()
	{
		return m_preProcessingTicksCountdown > 0 && m_preProcessingTicksCountdown < m_queuedOperationTicks;
	}

	internal bool IsCompleted()
	{
		return m_isCompleted;
	}

	internal void SetIsCompleted(bool isCompleted)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_INT, METH_NAME, this, isCompleted);

		m_isCompleted = isCompleted;
	}

	internal int GetTicksCountdown()
	{
		return m_preProcessingTicksCountdown;
	}

	internal void SetTicksCountdown(int ticksCountdown)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_INT, METH_NAME, this, ticksCountdown);

		MUX_ASSERT(ticksCountdown > 0);
		m_preProcessingTicksCountdown = m_queuedOperationTicks = ticksCountdown;
	}

	// Sets the ticks countdown to the max value of c_queuedOperationTicks == 3. This is invoked for queued operations
	// when the extent or viewport size changed in order to let it propagate to the Composition thread and thus let the
	// InteractionTracker operate on the latest sizes.
	internal void SetMaxTicksCountdown()
	{
		int ticksCountdownIncrement = Math.Max(0, c_queuedOperationTicks - m_preProcessingTicksCountdown);

		if (ticksCountdownIncrement > 0)
		{
			// SCROLLPRESENTER_TRACE_INFO(nullptr, TRACE_MSG_METH_INT, METH_NAME, this, ticksCountdownIncrement);

			m_preProcessingTicksCountdown += ticksCountdownIncrement;
			m_queuedOperationTicks += ticksCountdownIncrement;
		}
	}

	internal InteractionTrackerAsyncOperationTrigger GetOperationTrigger()
	{
		return m_operationTrigger;
	}

	// Returns True when the operation fulfills a horizontal IScrollController request.
	internal bool IsHorizontalScrollControllerRequest()
	{
		return (m_operationTrigger & InteractionTrackerAsyncOperationTrigger.HorizontalScrollControllerRequest) != 0;
	}

	// Returns True when the operation fulfills a vertical IScrollController request.
	internal bool IsVerticalScrollControllerRequest()
	{
		return (m_operationTrigger & InteractionTrackerAsyncOperationTrigger.VerticalScrollControllerRequest) != 0;
	}

	internal void SetIsScrollControllerRequest(bool isFromHorizontalScrollController)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_INT, METH_NAME, this, isFromHorizontalScrollController);

		if (isFromHorizontalScrollController)
			m_operationTrigger = m_operationTrigger |
								 InteractionTrackerAsyncOperationTrigger.HorizontalScrollControllerRequest;
		else
			m_operationTrigger = m_operationTrigger |
								 InteractionTrackerAsyncOperationTrigger.VerticalScrollControllerRequest;
	}

	// Returns True when the post-processing ticks count has reached 0
	internal bool TickNonAnimatedOperation()
	{
		MUX_ASSERT(!IsAnimated());
		MUX_ASSERT(m_postProcessingTicksCountdown > 0);

		m_postProcessingTicksCountdown--;

		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_INT, METH_NAME, this, m_postProcessingTicksCountdown);
		return m_postProcessingTicksCountdown == 0;
	}

	// Returns True when the pre-processing ticks count has reached 0
	internal bool TickQueuedOperation()
	{
		MUX_ASSERT(m_preProcessingTicksCountdown > 0);

		m_preProcessingTicksCountdown--;

		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_INT, METH_NAME, this, m_preProcessingTicksCountdown);
		return m_preProcessingTicksCountdown == 0;
	}

	internal InteractionTrackerAsyncOperationType GetOperationType()
	{
		return m_operationType;
	}

	internal int GetRequestId()
	{
		return m_requestId;
	}

	internal void SetRequestId(int requestId)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_INT, METH_NAME, this, requestId);

		m_requestId = requestId;
	}

	internal ViewChangeBase GetViewChangeBase()
	{
		return m_viewChangeBase;
	}

	internal InteractionTrackerAsyncOperation GetRequiredOperation()
	{
		return m_requiredOperation;
	}

	internal void SetRequiredOperation(InteractionTrackerAsyncOperation requiredOperation)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_PTR, METH_NAME, this, requiredOperation);

		m_requiredOperation = requiredOperation;
	}

	// Identifies the InteractionTracker request type for this operation.
	private InteractionTrackerAsyncOperationType m_operationType = InteractionTrackerAsyncOperationType.None;

	// Identifies the InteractionTracker trigger type for this operation.
	private InteractionTrackerAsyncOperationTrigger m_operationTrigger = InteractionTrackerAsyncOperationTrigger.DirectViewChange;

	// Number of UI thread ticks remaining before a non-animated InteractionTracker request is declared completed
	// in case no ValuesChanged or status change notification is raised.
	private int m_postProcessingTicksCountdown;

	// Number of UI thread ticks remaining before this queued operation gets processed.
	// Positive between the time the operation is queued in ScrollPresenter::ScrollTo/By/From, ScrollPresenter::ZoomTo/By/From or
	// ScrollPresenter::OnCompositionTargetRendering and the time it is processed in ScrollPresenter::ProcessOffsetsChange or ScrollPresenter::ProcessZoomFactorChange.
	private int m_preProcessingTicksCountdown = c_queuedOperationTicks;

	// Initial value of m_preProcessingTicksCountdown when this operation is queued up.
	private int m_queuedOperationTicks = c_queuedOperationTicks;

	// InteractionTracker RequestId associated with this operation.
	private int m_requestId = -1;

	// Set to True when the operation was canceled early enough to take effect.
#pragma warning disable CS0649 // error CS0649: Field 'InteractionTrackerAsyncOperation.m_isCanceled' is never assigned to, and will always have its default value false
	private bool m_isCanceled;
#pragma warning restore CS0649

	// Set to True when the operation is delayed until the scrollPresenter is loaded.
	private bool m_isDelayed;

	// Set to True when the operation completed and was assigned a final ScrollPresenterViewChangeResult result.
	private bool m_isCompleted;

	// OffsetsChange or ZoomFactorChange instance associated with this operation.
	private ViewChangeBase m_viewChangeBase;

	// ViewChangeCorrelationId associated with this operation.
	private int m_viewChangeCorrelationId = -1;

	// Null by default and optionally set to a prior operation that needs to complete before this one can start.
	private InteractionTrackerAsyncOperation m_requiredOperation;
};

