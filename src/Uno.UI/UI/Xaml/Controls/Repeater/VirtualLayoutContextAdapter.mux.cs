// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference VirtualLayoutContextAdapter.cpp, commit 5f9e851133b3

using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Uno.Extensions;

namespace Microsoft.UI.Xaml.Controls;

partial class VirtualLayoutContextAdapter
{
	public VirtualLayoutContextAdapter(VirtualizingLayoutContext virtualizingContext)
	{
		m_virtualizingContext = new System.WeakReference<VirtualizingLayoutContext>(virtualizingContext);
	}

	// #pragma region ILayoutContextOverrides

	protected internal override object LayoutStateCore
	{
		get => m_virtualizingContext.TryGetTarget(out var context) ? context.LayoutState : null;
		set
		{
			if (m_virtualizingContext.TryGetTarget(out var context))
			{
				context.LayoutState = value;
			}
		}
	}

	// #pragma endregion

	// #pragma region INonVirtualizingLayoutContextOverrides

	public override IReadOnlyList<UIElement> ChildrenCore
	{
		get
		{
			if (m_children == null)
			{
				m_children = new ChildrenCollection(m_virtualizingContext.GetTarget());
			}

			return m_children;
		}
	}

	// #pragma endregion
}
