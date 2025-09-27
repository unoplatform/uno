// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Windows.UI.Core;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class Layout : DependencyObject
	{
		// Begin Uno specific:
		//
		// We rely on the GC to manage registrations
		// but in the case of layouts, for ItemView for instance, actual instances
		// may be placed directly in dictionaries, such as:
		// https://github.com/unoplatform/uno/blob/c992ed058d1479cce8e6bca58acbf82cc54ce938/src/Uno.UI/UI/Xaml/Controls/ItemsView/ItemsView.xaml#L12-L16
		// To avoid memory leaks, it's best to use the two register methods.

		private WeakEventHelper.WeakEventCollection _measureInvalidatedHandlers;
		private WeakEventHelper.WeakEventCollection _arrangeInvalidatedHandlers;

		internal IDisposable RegisterMeasureInvalidated(TypedEventHandler<Layout, object> handler)
			=> WeakEventHelper.RegisterEvent(
				_measureInvalidatedHandlers ??= new(),
				handler,
				(h, s, e) =>
					(h as TypedEventHandler<Layout, object>)?.Invoke((Layout)s, e)
			);
		internal IDisposable RegisterArrangeInvalidated(TypedEventHandler<Layout, object> handler)
			=> WeakEventHelper.RegisterEvent(
				_arrangeInvalidatedHandlers ??= new(),
				handler,
				(h, s, e) =>
					(h as TypedEventHandler<Layout, object>)?.Invoke((Layout)s, e)
			);

		// End Uno specific: 

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
		{
			_measureInvalidatedHandlers?.Invoke(this, null);
			MeasureInvalidated?.Invoke(this, null);
		}

		protected void InvalidateArrange()
		{
			_arrangeInvalidatedHandlers?.Invoke(this, null);
			ArrangeInvalidated?.Invoke(this, null);
		}
	}
}
