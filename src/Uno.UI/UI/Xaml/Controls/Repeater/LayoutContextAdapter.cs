// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	internal partial class LayoutContextAdapter : VirtualizingLayoutContext
	{
		private readonly WeakReference<NonVirtualizingLayoutContext> m_nonVirtualizingContext;

		public LayoutContextAdapter(NonVirtualizingLayoutContext nonVirtualizingContext)
		{
			m_nonVirtualizingContext = new WeakReference<NonVirtualizingLayoutContext>(nonVirtualizingContext);
		}

		#region ILayoutContextOverrides
		protected internal override object LayoutStateCore
		{
			get => m_nonVirtualizingContext.TryGetTarget(out var context) ? context.LayoutState : null;
			set
			{
				if (m_nonVirtualizingContext.TryGetTarget(out var context))
				{
					context.LayoutState = value;
				}
			}
		}

		#endregion

		#region IVirtualizingLayoutContextOverrides

		protected override int ItemCountCore() => m_nonVirtualizingContext.TryGetTarget(out var context) ? context.Children.Count : 0;

		protected override object GetItemAtCore(int index)
			=> m_nonVirtualizingContext.TryGetTarget(out var context) ? context.Children[index] : null;

		protected override UIElement GetOrCreateElementAtCore(int index, ElementRealizationOptions options)
			=> m_nonVirtualizingContext.TryGetTarget(out var context) ? context.Children[index] : null;

		protected override void RecycleElementCore(UIElement element)
		{
		}

#if false
		private int GetElementIndexCore(UIElement element)
		{
			if (m_nonVirtualizingContext.TryGetTarget(out var context))
			{
				var children = context.Children;
				for (int i = 0; i < children.Count; i++)
				{
					if (children[i] == element)
					{
						return i;
					}
				}
			}

			return -1;
		}
#endif

		protected override Rect RealizationRectCore() => new Rect(0, 0, double.PositiveInfinity, double.PositiveInfinity);

		protected override int RecommendedAnchorIndexCore => -1;

		protected override Point LayoutOriginCore
		{
			get => new Point(0, 0);
			set
			{
				if (value != new Point(0, 0))
				{
					throw new ArgumentException("LayoutOrigin must be at (0,0) when RealizationRect is infinite sized.");
				}
			}
		}
		#endregion
	}
}
