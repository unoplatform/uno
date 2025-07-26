// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference DisplayRegionHelperTestApi.cpp, tag winui3/release/1.4.2

namespace Microsoft.UI.Xaml.Controls;

internal static class DisplayRegionHelperTestApi
{
	public static bool SimulateDisplayRegions
	{
		get => DisplayRegionHelper.SimulateDisplayRegions();
		set => DisplayRegionHelper.SimulateDisplayRegions(value);
	}

	public static TwoPaneViewMode SimulateMode
	{
		get => DisplayRegionHelper.SimulateMode();
		set => DisplayRegionHelper.SimulateMode(value);
	}

}
