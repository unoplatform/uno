// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LineMetrics.h, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using Windows.Foundation;
using Microsoft.UI.Xaml.Documents.RichTextServices;

namespace Microsoft.UI.Xaml.Documents.BlockLayout;

/// <summary>
/// Stores formatting results for one line of text.
/// </summary>
// Ported from a C++ value struct to a class: it carries reference-typed line /
// break fields and is mutated in place inside the ParagraphNode line cache,
// which is far more ergonomic with reference semantics in C#.
internal sealed class LineMetrics
{
	public uint FirstCharIndex; // TODO: could get this value from the line break
	public uint Length;
	public Rect Rect;
	public float VerticalAdvance;
	public float BaselineOffset;
	public TextLineBreak? LineBreak;
	public TextLine? Line;
	public bool HasMultiCharacterClusters;
}
