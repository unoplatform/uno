// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextSource.h, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// TextSource provides character and formatting data to the TextFormatter. It
/// translates platform-specific types and values into types understood by the
/// TextFormatter class.
/// </summary>
internal abstract class TextSource
{
	// Fetches a run of text starting at the specified character index.
	public abstract TextRun GetTextRun(uint characterIndex);

	// Gets the embedded object host for the TextSource in current formatting
	// context.
	public abstract IEmbeddedElementHost? GetEmbeddedElementHost();
}
