// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextCharactersRun.h, TextCharactersRun.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// TextCharactersRun class represents Unicode text content with associated
/// formatting properties.
/// </summary>
internal sealed class TextCharactersRun : TextRun
{
	// Character buffer containing the run's character data.
	private readonly ReadOnlyMemory<char> m_pCharacters;

	// Resolved formatting properties shared by all characters in the run.
	private readonly TextRunProperties m_pProperties;

	// Initializes a new instance of the TextRun class.
	public TextCharactersRun(
		ReadOnlyMemory<char> pCharacters,
		uint length,
		uint characterIndex,
		TextRunProperties pProperties)
		: base(length, characterIndex, TextRunType.Text)
	{
		m_pCharacters = pCharacters;
		m_pProperties = pProperties;
	}

	// Gets character buffer containing the run's character data.
	public ReadOnlyMemory<char> Characters => m_pCharacters;

	// Gets formatting properties shared by all characters in the run.
	public TextRunProperties Properties => m_pProperties;

	public bool IsTab =>
		m_length == 1 &&
		m_pCharacters.Span[0] == '\t'; // UNICODE_CHARACTER_TABULATION

	// Splits the run into two adjacent runs.
	public TextCharactersRun Split(uint offset)
	{
		MUX_ASSERT(offset > 0 && offset < m_length);

		var pTextCharactersRun = new TextCharactersRun(
			m_pCharacters.Slice((int)offset),
			m_length - offset,
			CharacterIndex + offset,
			m_pProperties);

		m_length = offset;

		return pTextCharactersRun;
	}
}
