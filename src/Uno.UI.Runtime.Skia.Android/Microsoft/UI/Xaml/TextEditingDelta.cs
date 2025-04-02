// Copyright 2013 The Flutter Authors. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be
// found in the THIRD-PARTY-NOTICES.md file.

// Ported to C# from https://github.com/flutter/flutter/blob/ea4cdcf39e935bb643b1294abe52c45063597caf/engine/src/flutter/shell/platform/android/io/flutter/plugin/editing/TextEditingDelta.java

namespace Uno.UI.Runtime.Skia.Android;

/// A representation of the change that occurred to an editing state, along with the resulting
/// composing and selection regions.
internal record TextEditingDelta(
		string OldText,
		int DeltaStart,
		int DeltaEnd,
		string DeltaText,
		int NewSelectionStart,
		int NewSelectionEnd,
		int NewComposingStart,
		int NewComposingEnd
	)
{
	public TextEditingDelta(
		string oldText,
		int selectionStart,
		int selectionEnd,
		int composingStart,
		int composingEnd)
		: this(oldText, -1, -1, "", selectionStart, selectionEnd, composingStart, composingEnd)
	{
	}
}
