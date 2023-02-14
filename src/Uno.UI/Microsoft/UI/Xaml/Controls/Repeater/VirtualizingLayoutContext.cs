// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class VirtualizingLayoutContext : LayoutContext
	{
		private NonVirtualizingLayoutContext m_contextAdapter;

		#region IVirtualizingLayoutContext
		public int ItemCount => ItemCountCore();

		public Point LayoutOrigin
		{
			get => LayoutOriginCore;
			set => LayoutOriginCore = value;
		}

		public Rect RealizationRect => RealizationRectCore();

		public int RecommendedAnchorIndex => RecommendedAnchorIndexCore;

		public object GetItemAt(int index)
			=> GetItemAtCore(index);

		public UIElement GetOrCreateElementAt(int index)
			=> GetOrCreateElementAtCore(index, ElementRealizationOptions.None);

		public UIElement GetOrCreateElementAt(int index, ElementRealizationOptions options)
			=> GetOrCreateElementAtCore(index, options);

		public void RecycleElement(UIElement element)
			=> RecycleElementCore(element);
		#endregion

		#region IVirtualizingLayoutContextOverrides

		protected virtual Point LayoutOriginCore
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		protected virtual int RecommendedAnchorIndexCore => throw new NotImplementedException();

		protected virtual object GetItemAtCore(int index)
			=> throw new NotImplementedException();

		protected virtual UIElement GetOrCreateElementAtCore(int index, ElementRealizationOptions options)
			=> throw new NotImplementedException();

		protected virtual void RecycleElementCore(UIElement element)
			=> throw new NotImplementedException();

		protected virtual Rect RealizationRectCore() => throw new NotImplementedException();

		protected virtual int ItemCountCore() => throw new NotImplementedException();
		#endregion

		internal NonVirtualizingLayoutContext GetNonVirtualizingContextAdapter()
		{
			if (m_contextAdapter == null)
			{
				m_contextAdapter = new VirtualLayoutContextAdapter(this);
			}

			return m_contextAdapter;
		}
	}
}
