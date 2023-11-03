// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference DisplayRegionHelper.h, tag winui3/release/1.4.2

#nullable enable

using System;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

internal struct DisplayRegionHelperInfo
{
	private const int c_maxRegions = 2;

	public DisplayRegionHelperInfo()
	{
		Mode = TwoPaneViewMode.SinglePane;
		Regions = new Rect[c_maxRegions]; ;
	}

	public TwoPaneViewMode Mode { get; set; }

	public Rect[] Regions { get; set; }
}
