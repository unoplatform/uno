// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Uno.UI.Helpers;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	public partial class Layout : DependencyObject
	{
		private readonly WeakEventManager _weakEventManager = new();

		// Uno specific: ItemsRepeater uses a singleton StackLayout.
		// Subscribing to MeasureInvalidated and failing to unsubscribe is a large memory leak.
		// Unsubscribing for that in Unloaded failed, because subscription can happen before Loaded and in cases where Loaded/Unloaded are never raised.
		// The fact that we subscribe in such case feels like a lifecycle bug related to applying templates (things are
		// initiated in NavigationView.OnApplyTemplate, but probably that OnApplyTemplate call shouldn't have happened).
		// For now, the only feasible solution is to have a weak event.
		// NOTE that at the time of writing this, there is another bad subscription that happens early in ItemsRepeater constructor, which isn't the case on WinUI.
		// However, fixing that bad subscription will still leak due to the lifecycle issue (at least, at the time of writing this).
		internal event Action WeakMeasureInvalidated
		{
			add => _weakEventManager.AddEventHandler(value);
			remove => _weakEventManager.RemoveEventHandler(value);
		}

		internal event Action WeakArrangeInvalidated
		{
			add => _weakEventManager.AddEventHandler(value);
			remove => _weakEventManager.RemoveEventHandler(value);
		}


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
			_weakEventManager.HandleEvent(nameof(WeakMeasureInvalidated));
			MeasureInvalidated?.Invoke(this, null);
		}

		protected void InvalidateArrange()
		{
			_weakEventManager.HandleEvent(nameof(WeakArrangeInvalidated));
			ArrangeInvalidated?.Invoke(this, null);
		}
	}
}
