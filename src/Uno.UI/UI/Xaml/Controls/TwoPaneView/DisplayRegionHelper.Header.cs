// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference DisplayRegionHelper.h, tag winui3/release/1.4.2

#nullable enable

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

internal partial class DisplayRegionHelper
{
	private bool m_simulateDisplayRegions = false;
	private TwoPaneViewMode m_simulateMode = TwoPaneViewMode.SinglePane;

	private static readonly Rect m_simulateWide0 = new Rect(0, 0, 300, 400);
	private static readonly Rect m_simulateWide1 = new Rect(312, 0, 300, 400);
	private static readonly Rect m_simulateTall0 = new Rect(0, 0, 400, 300);
	private static readonly Rect m_simulateTall1 = new Rect(0, 312, 400, 300);
}
