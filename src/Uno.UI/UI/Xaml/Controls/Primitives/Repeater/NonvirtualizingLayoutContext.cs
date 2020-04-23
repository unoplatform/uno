// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Microsoft.UI.Xaml.Controls
{
	public class NonVirtualizingLayoutContext : LayoutContext
	{
		#region INonVirtualizingLayoutContext

		IVectorView<UIElement> NonVirtualizingLayoutContext.Children()
		{
			return overridable().ChildrenCore();
		}

		#endregion

		#region INonVirtualizingLayoutContextOverrides

		IVectorView<UIElement> NonVirtualizingLayoutContext.ChildrenCore()
		{
			throw hresult_not_implemented();
		}

		#endregion

		internal VirtualizingLayoutContext GetVirtualizingContextAdapter()
		{
			if (!m_contextAdapter)
			{
				(m_contextAdapter = new LayoutContextAdapter(get_strong() as NonVirtualizingLayoutContext));
			}

			return m_contextAdapter;
		}
	}
}
