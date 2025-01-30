// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference SplitButtonTestApi.cpp, tag winui3/release/1.4.2

namespace Microsoft.UI.Private.Controls;

internal class SplitButtonTestApi
{
	public bool SimulateTouch()
	{
		return SplitButtonTestHelper.SimulateTouch;
	}

	public void SimulateTouch(bool value)
	{
		SplitButtonTestHelper.SimulateTouch = value;
	}
}
