// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Uno.Extensions;

namespace Microsoft.UI.Xaml.Controls
{
	internal partial class VirtualLayoutContextAdapter : NonVirtualizingLayoutContext
	{
		private readonly WeakReference<VirtualizingLayoutContext> m_virtualizingContext;

		private IReadOnlyList<UIElement> m_children;

		public VirtualLayoutContextAdapter(VirtualizingLayoutContext virtualizingContext)
		{
			m_virtualizingContext = new WeakReference<VirtualizingLayoutContext>(virtualizingContext);
		}

		#region ILayoutContextOverrides

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

		#endregion

		#region INonVirtualizingLayoutContextOverrides

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

		#endregion
	}
}
