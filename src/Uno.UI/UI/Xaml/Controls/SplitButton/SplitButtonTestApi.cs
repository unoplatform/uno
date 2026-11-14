// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference SplitButtonTestApi.cpp

namespace Microsoft.UI.Private.Controls;

internal class SplitButtonTestApi
{
	public static bool SimulateTouch
	{
		get => SplitButtonTestHelper.SimulateTouch;
		set => SplitButtonTestHelper.SimulateTouch = value;
	}
}
