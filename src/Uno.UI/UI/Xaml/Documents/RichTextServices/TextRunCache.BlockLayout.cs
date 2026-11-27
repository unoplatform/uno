// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

partial class TextRunCache
{
	// On Uno the LineServices-backed run cache is not ported; the cache holds the paragraph's
	// UnicodeText layouts instead.
	public static TextRunCache Create() => new UnicodeTextRunCache();
}
