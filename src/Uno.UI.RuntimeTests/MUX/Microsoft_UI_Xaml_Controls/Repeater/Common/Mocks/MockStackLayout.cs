// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.Foundation;

using Microsoft/* UWP don't rename */.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests.Common.Mocks;

partial class MockStackLayout : StackLayout
{
	public Func<Size, VirtualizingLayoutContext, Size> MeasureLayoutFunc { get; set; }
	public Func<Size, VirtualizingLayoutContext, Size> ArrangeLayoutFunc { get; set; }

	public new void InvalidateMeasure()
	{
		base.InvalidateMeasure();
	}

	protected internal override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
	{
		return MeasureLayoutFunc != null ? MeasureLayoutFunc(availableSize, context) : default(Size);
	}

	protected internal override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
	{
		return ArrangeLayoutFunc != null ? ArrangeLayoutFunc(finalSize, context) : default(Size);
	}
}
