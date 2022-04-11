#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using CoreGraphics;
using Uno.UI;

namespace Windows.UI.Xaml;

internal partial interface ILayouterElement
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal bool XamlMeasureInternal(
		Size availableSize,
		Size? lastAvailableSize,
		out CGSize measuredSize)
	{
		if (IsMeasureDirty || availableSize != lastAvailableSize)
		{
			if (this.DoMeasure(availableSize, out var measuredSizeLogical))
			{
				measuredSize = measuredSizeLogical.LogicalToPhysicalPixels();
				return true;
			}
		}

		// No need to measure
		measuredSize = default;
		return false;
	}
}
