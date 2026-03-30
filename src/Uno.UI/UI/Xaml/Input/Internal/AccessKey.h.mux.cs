// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\AccessKeys\inc\AccessKey.h, tag winui3/release/1.4.3, commit 685d2bf
// MUX Reference dxaml\xcp\components\AccessKeys\AccessKey\AccessKey.cpp, tag winui3/release/1.4.3, commit 685d2bf

#if __SKIA__
#nullable enable

using System;
using System.Globalization;

namespace Microsoft.UI.Xaml.Input;

/// <summary>
/// Encapsulates the representation of an AccessKey (effectively a string of characters) and comparison methods.
/// </summary>
internal sealed class AKAccessKey : IEquatable<AKAccessKey>
{
	// Space for 6 characters. In the past we had a max of three characters here, but this doesn't work well for
	// characters that are made up of two unicode code points (called "surrogate pairs"). We've raised the maximum to 6
	// so we can support 3 surrogate pairs.
	internal const int MaxAccessKeyLength = 6;

	private readonly string _accessKey;

	internal AKAccessKey()
	{
		_accessKey = string.Empty;
	}

	internal AKAccessKey(char accessKey)
	{
		_accessKey = MakeUppercase(accessKey.ToString());
	}

	internal AKAccessKey(string accessKey)
	{
		if (accessKey.Length > MaxAccessKeyLength)
		{
			accessKey = accessKey.Substring(0, MaxAccessKeyLength);
		}

		_accessKey = MakeUppercase(accessKey);
	}

	internal string GetAccessKeyString() => _accessKey;

	/// <summary>
	/// True when the first non-empty characters of (this) match the first characters of other.
	/// If 2 AccessKeys are partial matches of each other, they are equal.
	/// </summary>
	internal bool IsPartialMatch(AKAccessKey other)
	{
		// The C++ version iterates the fixed buffer and treats null chars as matching.
		// We replicate: for each char in _accessKey, if it equals the corresponding char in other, continue.
		// If we reach the end of our key, it's a match (our key is a prefix of or equal to other).
		if (_accessKey.Length == 0)
		{
			return true;
		}

		if (_accessKey.Length > other._accessKey.Length)
		{
			return false;
		}

		return other._accessKey.StartsWith(_accessKey, StringComparison.CurrentCultureIgnoreCase);
	}

	public bool Equals(AKAccessKey? other)
	{
		if (other is null)
		{
			return false;
		}

		return string.Equals(_accessKey, other._accessKey, StringComparison.CurrentCultureIgnoreCase);
	}

	public override bool Equals(object? obj) => Equals(obj as AKAccessKey);

	public override int GetHashCode()
	{
		// Use uppercase for consistent hashing
		return StringComparer.CurrentCultureIgnoreCase.GetHashCode(_accessKey);
	}

	public static bool operator ==(AKAccessKey? left, AKAccessKey? right)
	{
		if (left is null)
		{
			return right is null;
		}

		return left.Equals(right);
	}

	public static bool operator !=(AKAccessKey? left, AKAccessKey? right) => !(left == right);

	private static string MakeUppercase(string value)
	{
		// C++ uses LCMapStringEx with LCMAP_UPPERCASE | LCMAP_LINGUISTIC_CASING
		// which is equivalent to CultureInfo.CurrentCulture.TextInfo.ToUpper()
		return CultureInfo.CurrentCulture.TextInfo.ToUpper(value);
	}
}
#endif
