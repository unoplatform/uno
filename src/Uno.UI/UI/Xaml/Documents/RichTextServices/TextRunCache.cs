// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextRunCache.h, tag winui3/release/2.4.0, commit e8442d07a

#nullable enable

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// Provides run caching services in order to improve performance.
/// </summary>
internal abstract partial class TextRunCache
{
	// TODO Uno: The concrete instance is created by the Skia text formatter
	// (the C++ static Create() produced the LineServices-backed cache, which
	// is the Uno-specific low-level engine and is not ported).

	// Clears all runs in the cache.
	// Should be called when content is invalidated.
	public abstract void Clear();
}
