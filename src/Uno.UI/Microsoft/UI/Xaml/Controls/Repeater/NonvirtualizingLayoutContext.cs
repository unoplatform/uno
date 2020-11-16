// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Windows.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public class NonVirtualizingLayoutContext : LayoutContext
	{
		private LayoutContextAdapter m_contextAdapter;

		public IReadOnlyList<UIElement> Children => ChildrenCore;

		protected virtual IReadOnlyList<UIElement> ChildrenCore => throw new NotImplementedException();

		internal VirtualizingLayoutContext GetVirtualizingContextAdapter()
		{
			if (m_contextAdapter == null)
			{
				m_contextAdapter = new LayoutContextAdapter(this);
			}

			return m_contextAdapter;
		}
	}
}
