// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference EndOfLineRun.h, EndOfLineRun.cpp, tag winui3/release/2.4.0, commit e8442d07a

#nullable enable

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// EndOfLineRun class represents line terminating content.
/// </summary>
internal sealed class EndOfLineRun : TextRun
{
	// Initializes a new instance of the EndOfLineRun class.
	public EndOfLineRun(uint characterIndex)
		: base(1, characterIndex, TextRunType.EndOfLine)
	{
	}
}
