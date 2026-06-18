// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextRun.h, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// TextRun represents one run of content. TextRun may be of different types as
/// detailed by the <see cref="TextRunType"/> enum.
/// </summary>
internal abstract class TextRun
{
	// Number of characters in the run.
	protected uint m_length;

	// Type of data stored in this run - hidden characters, plain text, etc.
	private readonly TextRunType m_type;

	// Index of the first character of the run (relative to the current context).
	private readonly uint m_characterIndex;

	// Initializes a new instance of the TextRun class.
	protected TextRun(
		uint length,
		uint characterIndex,
		TextRunType type)
	{
		m_length = length;
		m_characterIndex = characterIndex;
		m_type = type;
	}

	// Gets the length of the run in backing store characters.
	public uint Length => m_length;

	// Gets type of data stored in this run.
	public TextRunType Type => m_type;

	// Gets index of the first character of the run.
	public uint CharacterIndex => m_characterIndex;
}
