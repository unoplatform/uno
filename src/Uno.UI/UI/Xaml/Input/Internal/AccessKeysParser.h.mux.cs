// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\AccessKeys\inc\AccessKeysParser.h, tag winui3/release/1.4.3, commit 685d2bf
// MUX Reference dxaml\xcp\components\AccessKeys\Parser\AccessKeysParser.cpp, tag winui3/release/1.4.3, commit 685d2bf

#if __SKIA__
#nullable enable

namespace Microsoft.UI.Xaml.Input;

/// <summary>
/// Attempts to parse the AccessKey property field into a valid AKAccessKey.
/// </summary>
internal static class AKParser
{
	/// <summary>
	/// Attempts to parse the input accessString into a valid AKAccessKey.
	/// Returns true with valid access keys, and false otherwise.
	/// </summary>
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
	/// Returns true if the passed accessString contains a valid access key.
	/// </summary>
	internal static bool IsValidAccessKey(string? accessString)
	{
		// Right now only allow access keys of length 6 or less.
		if (string.IsNullOrEmpty(accessString) || accessString!.Length > AKAccessKey.MaxAccessKeyLength)
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

	// Strings in this list will cause parsing to fail
	private static bool ContainsInvalidSubstring(string accessString)
	{
		// For each string in invalidStringList, check it's not a substring in accessString
		if (accessString.Contains(' ') ||
			accessString.Contains('\t') ||
			accessString.Contains('\r') ||
			accessString.Contains('\n') ||
			accessString.Contains('\u200b')) // 0 width space character
		{
			return true;
		}

		// A null character terminates the wstring input internally, but it cannot be one of the characters
		// counted in the std::basic_string::length() method. Check for it explicitly here.
		foreach (var c in accessString)
		{
			if (c == '\0')
			{
				return true;
			}
		}

		return false;
	}
}
#endif
