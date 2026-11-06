// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference StackingLayoutStrategyImpl.h, tag winui3/release/2.5.1, commit ba3a8d59e6d

//  Abstract:
//      New stacking strategy to present items in a stacking fashion

#nullable enable

using System.Collections.Generic;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

internal partial class StackingLayoutStrategyImpl : LayoutStrategyBase
{
	#region Layout related methods

	// Begin/End measure are used to keep tracking of the largest element size
	// in the non-virtualizing direction so that we can use it when we estimate
	// the panel's extent.
	public void BeginMeasure() { m_extentInNonVirtualizingDirection = 0.0f; }
	public void EndMeasure() { }

	#endregion

	#region Snap points related

	public bool HasSnapPointOnElement(
		ElementType elementType,
		int elementIndex)
	{
		// ISP always has snap point on element.
		return true;
	}

	#endregion

	//
	// Special elements methods
	//

	public bool NeedsSpecialItem() => true;
	public bool NeedsSpecialGroup() => IsGrouping;

	public int GetSpecialItemIndex() => c_specialItemIndex;
	public int GetSpecialGroupIndex() => IsGrouping ? c_specialGroupIndex : -1;

	// also, headers are constrained to the size of the first header
	private Size m_headerSize;
	private bool m_headerSizeSet;

	// averaging vector. We keep it sized to c_totalSizesForAveraging indices. Each time we
	// take out a size and replace it with a new, we calculate a new total
	private readonly List<float> m_containerSizes = new();
	private readonly List<float> m_headerSizes = new();

	// the total amount of sizes to keep
	private const int c_totalSizesForAveraging = 1000;

	// the total of all valid sizes in the vector
	private double m_containerSizesTotal;
	private double m_headerSizesTotal;
	// the number of valid sizes in the vector
	private int m_containerSizesStoredTotal;
	private int m_headerSizesStoredTotal;

	private float m_extentInNonVirtualizingDirection;
}
