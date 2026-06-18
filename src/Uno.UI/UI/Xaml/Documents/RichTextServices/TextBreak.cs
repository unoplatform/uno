// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextBreak.h, TextBreak.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// Base class representing breaking state for text objects - lines, blocks, etc.
/// </summary>
internal class TextBreak
{
	// Compares with another TextBreak object and checks for equality.
	public virtual bool Equals(TextBreak? pBreak)
	{
		// Base class just returns reference equals.
		return this == pBreak;
	}
}
