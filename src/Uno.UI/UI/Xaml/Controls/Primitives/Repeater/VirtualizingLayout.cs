// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Specialized;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	public class VirtualizingLayout : Layout
	{
		protected internal virtual void InitializeForContextCore(VirtualizingLayoutContext context)
		{
		}

		protected internal virtual void UninitializeForContextCore(VirtualizingLayoutContext context)
		{
		}

		protected internal virtual Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
		{
			throw new NotImplementedException();
		}

		protected internal virtual Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
		{
			// Do not throw. If the layout decides to arrange its
			// children during measure, then an ArrangeOverride is not required.
			return finalSize;
		}

		protected virtual void OnItemsChangedCore(VirtualizingLayoutContext context, object source, NotifyCollectionChangedEventArgs args)
		{
			InvalidateMeasure();
		}
	}
}
