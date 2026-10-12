// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

partial class TextRunCache
{
	// On Uno the LineServices-backed run cache is not ported; the Skia ParsedText engine owns run
	// caching. Create() returns a minimal no-op cache so the block-layout port can hold one.
	public static TextRunCache Create() => new SkiaTextRunCache();

	private sealed class SkiaTextRunCache : TextRunCache
	{
		public override void Clear()
		{
		}
	}
}
