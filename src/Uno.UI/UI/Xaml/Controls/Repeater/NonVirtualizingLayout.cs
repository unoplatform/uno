// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class NonVirtualizingLayout : Layout
	{
		protected internal virtual void InitializeForContextCore(LayoutContext context)
		{

		}

		protected internal virtual void UninitializeForContextCore(LayoutContext context)
		{

		}

		protected internal virtual Size MeasureOverride(NonVirtualizingLayoutContext context, Size availableSize)
		{
			throw new NotImplementedException();
		}

		protected internal virtual Size ArrangeOverride(NonVirtualizingLayoutContext context, Size finalSize)
		{
			throw new NotImplementedException();
		}
	}
}
