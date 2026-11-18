// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference dxaml/xcp/dxaml/lib/PropertyPathCommonNames.cpp, commit 3c9c168844f06c6ac000a97977f0bb3f4c90fd75

#nullable enable

using System;

namespace DirectUI;

internal static class PropertyPathCommonNames
{
	// Static table of commonly used property path strings.
	private static readonly string[] s_commonPropertyNames =
	[
		"Description",
		"Label",
		"CommandBarTemplateSettings",
		"TemplateSettings",
		"AccessKey",
		"KeyboardAccelerators",
		"IconSource",
		"IsEnabled",
		"IsVisible",
	];

	// Uno: a constant span keeps the native length table in read-only data without another array.
	private static ReadOnlySpan<byte> CommonPropertyLengths => [11, 5, 26, 16, 9, 20, 10, 9, 9];

	internal static string? TryGetCommonPropertyName(ReadOnlySpan<char> name)
	{
		// Quick reject: length must fit in uint8_t to match any entry
		if (name.Length > byte.MaxValue)
		{
			return null;
		}

		var targetLength = (byte)name.Length;
		var lengths = CommonPropertyLengths;

		// Scan lengths first - this array is compact and cache-friendly
		for (var i = 0; i < lengths.Length; i++)
		{
			if (lengths[i] == targetLength)
			{
				// Length matches - now do the full string comparison
				if (name.SequenceEqual(s_commonPropertyNames[i]))
				{
					return s_commonPropertyNames[i];
				}
			}
		}

		return null;
	}
}
