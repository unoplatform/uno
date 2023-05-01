// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

#if !HAS_UNO_WINUI
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
#endif

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests.Common.Mocks
{
	class MockNonVirtualizingLayout : NonVirtualizingLayout
	{
		public Func<Size, NonVirtualizingLayoutContext, Size> MeasureLayoutFunc { get; set; }
		public Func<Size, NonVirtualizingLayoutContext, Size> ArrangeLayoutFunc { get; set; }

		public new void InvalidateMeasure()
		{
			base.InvalidateMeasure();
		}

		protected internal override Size MeasureOverride(NonVirtualizingLayoutContext context, Size availableSize)
		{
			return MeasureLayoutFunc != null ? MeasureLayoutFunc(availableSize, context) : default(Size);
		}

		protected internal override Size ArrangeOverride(NonVirtualizingLayoutContext context, Size finalSize)
		{
			return ArrangeLayoutFunc != null ? ArrangeLayoutFunc(finalSize, context) : default(Size);
		}
	}
}
