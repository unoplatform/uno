// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

namespace Windows.UI.Xaml.Controls;

internal sealed partial class ScrollViewBringIntoViewOperation
{
	public ScrollViewBringIntoViewOperation(UIElement targetElement, bool cancelBringIntoView)
	{
		//SCROLLVIEW_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_PTR, METH_NAME, this, targetElement);

		m_targetElement = new WeakReference<UIElement>(targetElement);
		m_cancelBringIntoView = cancelBringIntoView;
	}

	// ~ScrollViewBringIntoViewOperation()
	// {
	// 	SCROLLVIEW_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_PTR_INT, METH_NAME, this, m_targetElement.get(), m_ticksCount);
	// }

	public bool HasMaxTicksCount
	{
		get
		{
			MUX_ASSERT(m_ticksCount <= s_maxTicksCount);

			return m_ticksCount == s_maxTicksCount;
		}
	}

	public UIElement TargetElement
		=> m_targetElement.TryGetTarget(out var target) ? target : null;

	public sbyte TicksCount
		=> m_ticksCount;

	public sbyte TickOperation()
	{
		//SCROLLVIEW_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_PTR_INT, METH_NAME, this, m_targetElement.get(), m_ticksCount);

		MUX_ASSERT(m_ticksCount < s_maxTicksCount);

		return ++m_ticksCount;
	}
}
