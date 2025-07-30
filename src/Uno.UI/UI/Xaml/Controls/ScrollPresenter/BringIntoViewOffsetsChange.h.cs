// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

internal sealed partial class BringIntoViewOffsetsChange : OffsetsChange
{
	public UIElement Element { get; }

	public Rect ElementRect { get; }

	public double HorizontalAlignmentRatio { get; }

	public double VerticalAlignmentRatio { get; }

	public double HorizontalOffset { get; }

	public double VerticalOffset { get; }
};

