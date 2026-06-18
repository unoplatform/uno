// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference DirectionalControl.h, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// DirectionalControl enumerates types of directional control a
/// DirectionalControlRun may specify.
/// </summary>
internal enum DirectionalControl
{
	// No directional control.
	None,

	// Directional control change from left to right.
	LeftToRightEmbedding,

	// Directional control change from right to left.
	RightToLeftEmbedding,

	// Pop directional formatting - paired with embedding run.
	PopDirectionalFormatting,

	// Explicit left to right mark.
	LeftToRightMark,

	// Explicit right to left mark.
	RightToLeftMark,
}
