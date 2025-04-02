// Copyright 2013 The Flutter Authors. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be
// found in the THIRD-PARTY-NOTICES.md file.

// Ported to C# from https://github.com/flutter/flutter/blob/ea4cdcf39e935bb643b1294abe52c45063597caf/engine/src/flutter/shell/platform/android/io/flutter/embedding/engine/systemchannels/TextInputChannel.java#L764-L765

using System;

namespace Uno.UI.Runtime.Skia.Android;

/// <summary>
/// State of an on-going text editing session.
/// </summary>
internal class TextEditState
{
	public string text;
	public int selectionStart;
	public int selectionEnd;
	public int composingStart;
	public int composingEnd;

	public TextEditState(
		string text,
		int selectionStart,
		int selectionEnd,
		int composingStart,
		int composingEnd)
	{

		if ((selectionStart != -1 || selectionEnd != -1)
			&& (selectionStart < 0 || selectionEnd < 0))
		{
			throw new IndexOutOfRangeException(
				"invalid selection: ("
					+ selectionStart
					+ ", "
					+ selectionEnd
					+ ")");
		}

		if ((composingStart != -1 || composingEnd != -1)
			&& (composingStart < 0 || composingStart > composingEnd))
		{
			throw new IndexOutOfRangeException(
				"invalid composing range: ("
					+ composingStart
					+ ", "
					+ composingEnd
					+ ")");
		}

		if (composingEnd > text.Length)
		{
			throw new IndexOutOfRangeException(
				"invalid composing start: " + composingStart);
		}

		if (selectionStart > text.Length)
		{
			throw new IndexOutOfRangeException(
				"invalid selection start: " + selectionStart);
		}

		if (selectionEnd > text.Length)
		{
			throw new IndexOutOfRangeException(
				"invalid selection end: " + selectionEnd);
		}

		this.text = text;
		this.selectionStart = selectionStart;
		this.selectionEnd = selectionEnd;
		this.composingStart = composingStart;
		this.composingEnd = composingEnd;
	}

	public bool HasSelection
		// When selectionStart == -1, it's guaranteed that selectionEnd will also
		// be -1.
		=> selectionStart >= 0;

	public bool HasComposing
		=> composingStart >= 0 && composingEnd > composingStart;
}
