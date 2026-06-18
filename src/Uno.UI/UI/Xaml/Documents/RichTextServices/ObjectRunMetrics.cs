// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ObjectRunMetrics.h, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// Provides information about size and properties of an embedded inline object.
/// </summary>
internal readonly record struct ObjectRunMetrics(
	float Width,
	float Height,
	float Baseline);
