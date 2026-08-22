// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference TextElement_Partial.cpp (get_ContentStartImpl/get_ContentEndImpl/
// get_ElementStartImpl/get_ElementEndImpl), tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

namespace Microsoft.UI.Xaml.Documents;

// The public TextPointer surface of TextElement over the already-ported internal getters
// (GetContentStart/End, GetElementStart/End in TextElement.TextPointers.skia.cs). The underlying
// position model is Skia-only, so on non-Skia targets these return null, matching TextPointer's
// own null returns and WinUI's pre-layout behavior.
partial class TextElement
{
	/// <summary>
	/// Gets a TextPointer that represents the start of the content in the TextElement.
	/// </summary>
	public TextPointer? ContentStart => GetContentStart();

	/// <summary>
	/// Gets a TextPointer that represents the end of the content in the TextElement.
	/// </summary>
	public TextPointer? ContentEnd => GetContentEnd();

	/// <summary>
	/// Gets a TextPointer that represents the start of the TextElement.
	/// </summary>
	public TextPointer? ElementStart => GetElementStart();

	/// <summary>
	/// Gets a TextPointer that represents the end of the TextElement.
	/// </summary>
	public TextPointer? ElementEnd => GetElementEnd();

#if !__SKIA__
	private TextPointer? GetContentStart() => null;
	private TextPointer? GetContentEnd() => null;
	private TextPointer? GetElementStart() => null;
	private TextPointer? GetElementEnd() => null;
#endif
}
