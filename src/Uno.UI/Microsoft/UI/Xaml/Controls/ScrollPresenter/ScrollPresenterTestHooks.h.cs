// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Private.Controls;

internal partial class ScrollPresenterTestHooks
{
	public static ScrollPresenterTestHooks GetGlobalTestHooks()
	{
		return s_testHooks;
	}

	//private winrt::event<TypedEventHandler<ScrollPresenter, ScrollPresenterTestHooksAnchorEvaluatedEventArgs>> m_anchorEvaluatedEventSource;
	//private winrt::event<TypedEventHandler<ScrollPresenter, ScrollPresenterTestHooksInteractionSourcesChangedEventArgs>> m_interactionSourcesChangedEventSource;
	//private winrt::event<TypedEventHandler<ScrollPresenter, ScrollPresenterTestHooksExpressionAnimationStatusChangedEventArgs>> m_expressionAnimationStatusChangedEventSource;
	//private winrt::event<TypedEventHandler<ScrollPresenter, object>> m_contentLayoutOffsetXChangedEventSource;
	//private winrt::event<TypedEventHandler<ScrollPresenter, object>> m_contentLayoutOffsetYChangedEventSource;

	private bool m_areAnchorNotificationsRaised;
	private bool m_areInteractionSourcesNotificationsRaised;
	private bool m_areExpressionAnimationStatusNotificationsRaised;
	private bool? m_isAnimationsEnabledOverride;
	private int m_offsetsChangeMsPerUnit = ScrollPresenter.s_offsetsChangeMsPerUnit;
	private int m_offsetsChangeMinMs = ScrollPresenter.s_offsetsChangeMinMs;
	private int m_offsetsChangeMaxMs = ScrollPresenter.s_offsetsChangeMaxMs;
	private int m_zoomFactorChangeMsPerUnit = ScrollPresenter.s_zoomFactorChangeMsPerUnit;
	private int m_zoomFactorChangeMinMs = ScrollPresenter.s_zoomFactorChangeMinMs;
	private int m_zoomFactorChangeMaxMs = ScrollPresenter.s_zoomFactorChangeMaxMs;
};
