// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference WrappingLayoutStrategyImpl.h, tag winui3/release/2.5.1, commit ba3a8d59e6d

//  Abstract:
//      Represents a wrapping strategy for new style virtualizing panels.

#nullable enable

using System.Collections.Generic;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

internal partial class WrappingLayoutStrategyImpl : LayoutStrategyBase
{
	//public:

	// implementation of interface

	#region Layout related methods

	// The wrapping layout doesn't need to do any book keeping.
	public void BeginMeasure() { }
	public void EndMeasure() { }

	#endregion

	#region Snap points related

	public bool HasSnapPointOnElement(ElementType elementType, int elementIndex)
	{
		// IWG always has snap point on element.
		return true;
	}

	#endregion

	// internal configuration
	public void SetMaximumRowsOrColumns(int maxRowsOrColumns) { m_maxRowsOrColumns = maxRowsOrColumns; }

	//
	// Special elements methods
	// wrapgrid always needs a correct size for the first container, since it is special
	// The desiredsize there will determine the size for all
	//

	// If we've set an explicit size, we don't need this
	public bool NeedsSpecialItem() => !IsItemSizePropertySet();
	public bool NeedsSpecialGroup() => IsGrouping;

	public int GetSpecialItemIndex() => c_specialItemIndex;
	public int GetSpecialGroupIndex() => IsGrouping ? c_specialGroupIndex : -1;

	//private:

	// wrapgrid is a fixed size panel, meaning that all the containers
	// get the same size, whether they want to or not
	private Size m_cellSize;
	private bool m_cellSizeSet;

	// also, headers are constrained to the size of the first header
	private Size m_headerSize;
	private bool m_headerSizeSet;

	private int m_maxRowsOrColumns;

	// ItemWidth and ItemHeight properties
	private double m_itemWidthFromPanel;
	private double m_itemHeightFromPanel;

	// Cached data about the size of groups
	private List<float> m_cachedGroupLocations = new();
	private int m_cachedStackingLines;
	private Size m_cachedCellSize;
}
