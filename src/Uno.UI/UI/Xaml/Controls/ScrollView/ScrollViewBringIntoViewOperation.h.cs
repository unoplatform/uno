// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace Microsoft.UI.Xaml.Controls;

internal sealed partial class ScrollViewBringIntoViewOperation
{
	public bool ShouldCancelBringIntoView()
	{
		return m_cancelBringIntoView;
	}

	// Number of UI thread ticks allowed before this expected bring-into-view operation is no
	// longer expected and removed from the ScrollView's m_bringIntoViewOperations list.
	private const sbyte s_maxTicksCount = 3;

	private sbyte m_ticksCount;
	private WeakReference<UIElement> m_targetElement;
	private bool m_cancelBringIntoView;
}
