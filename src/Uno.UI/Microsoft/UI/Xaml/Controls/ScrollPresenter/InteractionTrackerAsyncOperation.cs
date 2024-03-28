// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Windows.UI.Xaml.Controls;

internal sealed partial class InteractionTrackerAsyncOperation
{
	public InteractionTrackerAsyncOperation(
		InteractionTrackerAsyncOperationType operationType,
		InteractionTrackerAsyncOperationTrigger operationTrigger,
		bool isDelayed,
		ViewChangeBase viewChangeBase)
	{
		m_operationType = operationType;
		m_operationTrigger = operationTrigger;
		m_isDelayed = isDelayed;
		m_viewChangeBase = viewChangeBase;
		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, L"%s[0x%p](operationType: %s, operationTrigger: %s, isDelayed: %d, viewChange: 0x%p)\n",
		// 	METH_NAME, this, TypeLogging::InteractionTrackerAsyncOperationTypeToString(operationType).c_str(),
		// 	TypeLogging::InteractionTrackerAsyncOperationTriggerToString(operationTrigger).c_str(), isDelayed, viewChangeBase);

		if (!IsAnimated())
		{
			m_postProcessingTicksCountdown = c_maxNonAnimatedOperationTicks;
		}
	}

	// ~InteractionTrackerAsyncOperation()
	// {
	// 	SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
	// }
}
