// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference EnumDefs.h (TextGravity), tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Documents.BlockLayout;

[Flags]
internal enum TextGravity : byte
{
	// The text gravity as flags:
	LineBackward = 1,
	CharacterBackward = 2,
	// The four possible values explicitly:
	LineForwardCharacterForward = 0, // E.g. selection start
	LineBackwardCharacterForward = LineBackward, // E.g. end of whole line
	LineForwardCharacterBackward = CharacterBackward, // E.g. start of whole line
	LineBackwardCharacterBackward = LineBackward | CharacterBackward, // E.g. selection end
}
