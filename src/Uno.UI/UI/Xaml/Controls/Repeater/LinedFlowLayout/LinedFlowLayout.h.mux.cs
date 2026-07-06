// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LinedFlowLayout.h, commit b8cfb8490

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class LinedFlowLayout
	{
		// Default property values (LinedFlowLayout.h).
		private const LinedFlowLayoutItemsJustification s_defaultItemsJustification = LinedFlowLayoutItemsJustification.Start;
		private const LinedFlowLayoutItemsStretch s_defaultItemsStretch = LinedFlowLayoutItemsStretch.None;
		private const double s_defaultActualLineHeight = 0.0;
		private const double s_defaultLineSpacing = 0.0;
		private const double s_defaultLineHeight = double.NaN;
		private const double s_defaultMinItemSpacing = 0.0;

		// Items info collected through the ItemsInfoRequested event for the regular (non-fast) path.
		private readonly List<double> m_itemsInfoDesiredAspectRatiosForRegularPath = new();
		private readonly List<double> m_itemsInfoMinWidthsForRegularPath = new();
		private readonly List<double> m_itemsInfoMaxWidthsForRegularPath = new();
		private readonly List<double> m_itemsInfoArrangeWidths = new();

#pragma warning disable 414 // Assigned but not used field: consumed by later WS-D2/D3 slices (measure/arrange), kept to mirror WinUI.
		// Items info collected through the ItemsInfoRequested event for the fast path.
		private double[] m_itemsInfoDesiredAspectRatiosForFastPath = Array.Empty<double>();
		private double[] m_itemsInfoMinWidthsForFastPath = Array.Empty<double>();
		private double[] m_itemsInfoMaxWidthsForFastPath = Array.Empty<double>();

		private int m_itemsInfoFirstIndex = -1;
		private double m_itemsInfoMinWidth = -1.0;
		private double m_itemsInfoMaxWidth = -1.0;
		private bool m_forceRelayout;
#pragma warning restore 414
	}
}
