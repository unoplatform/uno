// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RatingControl.h, commit b853109

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

internal enum RatingControlStates
{
	Disabled = 0,
	Set = 1,
	PointerOverSet = 2,
	PointerOverPlaceholder = 3, // Also functions as the pointer over unset state at the moment
	Placeholder = 4,
	Unset = 5,
	Null = 6
}
