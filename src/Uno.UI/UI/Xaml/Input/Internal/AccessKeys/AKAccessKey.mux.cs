// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\AccessKeys\AccessKey\AccessKey.cpp, tag winui3/release/1.5.3

#nullable enable

using System;
using System.Globalization;

namespace Uno.UI.Xaml.Input.AccessKeys;

internal partial class AKAccessKey
{
	private void Set(char accessKey)
	{
		_accessKey[0] = accessKey;

		for (int i = 1; i < MaxAccessKeyLength; i++)
		{
			_accessKey[i] = '\0';
		}

		_accessKey[MaxAccessKeyLength] = '\0';

		MakeAccessKeyUppercase();
	}

	private void Set(string accessKey)
	{
		for (int i = 0; i < MaxAccessKeyLength; i++)
		{
			_accessKey[i] = i < accessKey.Length ? accessKey[i] : '\0';
		}

		_accessKey[MaxAccessKeyLength] = '\0';

		MakeAccessKeyUppercase();
	}

	public bool Equals(AKAccessKey? other)
	{
		if (other is null)
		{
			return false;
		}

		for (int i = 0; i < MaxAccessKeyLength; i++)
		{
			if (_accessKey[i] != other._accessKey[i])
			{
				return false;
			}
		}

		return true;
	}

	public override bool Equals(object? obj) => obj is AKAccessKey other && Equals(other);

	public override int GetHashCode()
	{
		// Use a simple hash based on the access key string
		var hashCode = new HashCode();
		for (int i = 0; i < MaxAccessKeyLength; i++)
		{
			if (_accessKey[i] == '\0')
			{
				break;
			}
			hashCode.Add(_accessKey[i]);
		}
		return hashCode.ToHashCode();
	}

	/// <summary>
	/// Converts the access key to uppercase using the current culture.
	/// </summary>
	/// <remarks>
	/// Original C++ uses LCMapStringEx with LCMAP_UPPERCASE | LCMAP_LINGUISTIC_CASING.
	/// In C# we use TextInfo.ToUpper for equivalent linguistic casing behavior.
	/// </remarks>
	private void MakeAccessKeyUppercase()
	{
		// Get the current culture's text info for linguistic casing
		var textInfo = CultureInfo.CurrentCulture.TextInfo;

		for (int i = 0; i < MaxAccessKeyLength; i++)
		{
			if (_accessKey[i] == '\0')
			{
				break;
			}
			_accessKey[i] = textInfo.ToUpper(_accessKey[i]);
		}
	}

	/// <summary>
	/// Returns true when the first non-null characters of this match the first characters of other.
	/// If 2 AccessKeys are partial matches of each other, they are equal.
	/// </summary>
	/// <example>
	/// "A" is a partial match of "AB" (A matches the start of AB)
	/// "AB" is NOT a partial match of "A" (AB doesn't fit in A)
	/// "A" is a partial match of "A" (they're equal)
	/// "" (empty) is a partial match of anything (no characters to mismatch)
	/// </example>
	internal bool IsPartialMatch(AKAccessKey other)
	{
		for (int i = 0; i < MaxAccessKeyLength; i++)
		{
			// If we've reached the end of this access key, it's a partial match
			// (all characters so far matched)
			if (_accessKey[i] == '\0')
			{
				return true;
			}

			// If this character matches, continue checking
			if (_accessKey[i] == other._accessKey[i])
			{
				continue;
			}

			// Characters don't match
			return false;
		}

		// All characters in the buffer matched
		return true;
	}
}
