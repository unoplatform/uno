// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	public partial class Layout : DependencyObject
	{
		public event TypedEventHandler<Layout, object> MeasureInvalidated;
		public event TypedEventHandler<Layout, object> ArrangeInvalidated;

		internal string LayoutId { get; set; }

		internal static VirtualizingLayoutContext GetVirtualizingLayoutContext(LayoutContext context)
		{
			switch (context)
			{
				case VirtualizingLayoutContext virtualizingContext:
					return virtualizingContext;
				case NonVirtualizingLayoutContext nonVirtualizingContext:
					return nonVirtualizingContext.GetVirtualizingContextAdapter();
				default:
					throw new NotImplementedException();
			}
		}

		internal static NonVirtualizingLayoutContext GetNonVirtualizingLayoutContext(LayoutContext context)
		{
			switch (context)
			{
				case NonVirtualizingLayoutContext nonVirtualizingContext:
					return nonVirtualizingContext;
				case VirtualizingLayoutContext virtualizingContext:
					return virtualizingContext.GetNonVirtualizingContextAdapter();
				default:
					throw new NotImplementedException();
			}
		}

		public void InitializeForContext(LayoutContext context)
		{
			switch (this)
			{
				case VirtualizingLayout virtualizingLayout:
					virtualizingLayout.InitializeForContextCore(GetVirtualizingLayoutContext(context));
					break;

				case NonVirtualizingLayout nonVirtualizingLayout:
					nonVirtualizingLayout.InitializeForContextCore(GetNonVirtualizingLayoutContext(context));
					break;
				default:
					throw new NotImplementedException();
			}
		}

		public void UninitializeForContext(LayoutContext context)
		{
			switch (this)
			{
				case VirtualizingLayout virtualizingLayout:
					virtualizingLayout.UninitializeForContextCore(GetVirtualizingLayoutContext(context));
					break;

				case NonVirtualizingLayout nonVirtualizingLayout:
					nonVirtualizingLayout.UninitializeForContextCore(GetNonVirtualizingLayoutContext(context));
					break;
				default:
					throw new NotImplementedException();
			}
		}

		public Size Measure(LayoutContext context, Size availableSize)
		{
			switch (this)
			{
				case VirtualizingLayout virtualizingLayout:
					return virtualizingLayout.MeasureOverride(GetVirtualizingLayoutContext(context), availableSize);

				case NonVirtualizingLayout nonVirtualizingLayout:
					return nonVirtualizingLayout.MeasureOverride(GetNonVirtualizingLayoutContext(context), availableSize);

				default:
					throw new NotImplementedException();
			}
		}

		public Size Arrange(LayoutContext context, Size finalSize)
		{
			switch (this)
			{
				case VirtualizingLayout virtualizingLayout:
					return virtualizingLayout.ArrangeOverride(GetVirtualizingLayoutContext(context), finalSize);

				case NonVirtualizingLayout nonVirtualizingLayout:
					return nonVirtualizingLayout.ArrangeOverride(GetNonVirtualizingLayoutContext(context), finalSize);

				default:
					throw new NotImplementedException();
			}
		}

		protected void InvalidateMeasure()
			=> MeasureInvalidated?.Invoke(this, null);

		protected void InvalidateArrange()
			=> ArrangeInvalidated?.Invoke(this, null);
	}
}
