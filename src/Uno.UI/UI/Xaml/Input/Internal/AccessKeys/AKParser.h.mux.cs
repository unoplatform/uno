// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\AccessKeys\inc\AccessKeysParser.h, tag winui3/release/1.5.3
// MUX Reference dxaml\xcp\components\AccessKeys\Parser\AccessKeysParser.cpp, tag winui3/release/1.5.3

#nullable enable

using System;

namespace Uno.UI.Xaml.Input.AccessKeys;

/// <summary>
/// Parser for access key strings from AutomationProperties.AccessKey property.
/// </summary>
internal static class AKParser
{
	// Strings in this list will cause parsing to fail
	private static readonly string[] InvalidStrings =
	[
		" ",      // Space
		"\t",     // Tab
		"\r",     // Carriage return
		"\n",     // Newline
		"\u200b", // Zero-width space character
	];

	/// <summary>
	/// Attempts to parse the AutomationProperties.AccessKey property field from the input accessString.
	/// Returns true with valid access keys, and false otherwise.
	/// </summary>
	/// <param name="accessString">The access key string to parse.</param>
	/// <param name="accessKey">The parsed output access key.</param>
	/// <returns>True if parsing succeeded, false otherwise.</returns>
	internal static bool TryParseAccessKey(string? accessString, out AKAccessKey accessKey)
	{
		accessKey = new AKAccessKey();

		if (!IsValidAccessKey(accessString))
		{
			return false;
		}

		// Because accessString is valid (has valid length and character composition) set it to the accessKey
		accessKey = new AKAccessKey(accessString!);

		return true;
	}

	/// <summary>
	/// Returns true if the passed accessString contains a valid mnemonics access key.
	/// </summary>
	/// <param name="accessString">The access key string to validate.</param>
	/// <returns>True if the access key is valid, false otherwise.</returns>
	internal static bool IsValidAccessKey(string? accessString)
	{
		// Right now only allow access keys of length 6 or less.
		if (string.IsNullOrEmpty(accessString) || accessString.Length > AKAccessKey.MaxAccessKeyLength)
		{
			return false;
		}

		// If the access string contains any invalid characters or substrings, it's an invalid access string
		if (ContainsInvalidSubstring(accessString))
		{
			return false;
		}

		return true;
	}

	/// <summary>
	/// Checks if the access string contains any invalid substrings (whitespace, zero-width space, etc.).
	/// </summary>
	private static bool ContainsInvalidSubstring(string accessString)
	{
		// For each string in invalidStringList, check it's not a substring in accessString
		foreach (var invalidString in InvalidStrings)
		{
			if (accessString.Contains(invalidString, StringComparison.Ordinal))
			{
				return true;
			}
		}

		// A null character terminates the wstring input internally, but it cannot be one of the characters
		// counted in the std::basic_string::length() method.
		// Unfortunately, the find method seems to match the null character as a valid substring of
		// non-null characters. Will check for it explicitly here.
		foreach (var accessChar in accessString)
		{
			if (accessChar == '\0')
			{
				return true;
			}
		}

		return false;
	}
}
