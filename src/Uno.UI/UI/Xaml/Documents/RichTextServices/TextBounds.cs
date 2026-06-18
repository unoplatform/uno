// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextBounds.h, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// Represents information about text bounds including bounding rect and
/// flow direction.
/// </summary>
internal readonly record struct TextBounds(
	// Bounding rect.
	Rect Rect,
	// Flow Direction.
	FlowDirection FlowDirection);
