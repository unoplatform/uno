// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\AccessKeys\inc\AccessKey.h, tag winui3/release/1.5.3

#nullable enable

using System;

namespace Uno.UI.Xaml.Input.AccessKeys;

/// <summary>
/// Encapsulates the representation of an AccessKey (effectively a vector of characters) and comparison methods.
/// Space for 6 characters. In the past we had a max of three characters here, but this doesn't work well for
/// characters that are made up of two unicode code points (called "surrogate pairs"). We've raised the maximum to 6
/// so we can support 3 surrogate pairs.
/// </summary>
internal partial class AKAccessKey : IEquatable<AKAccessKey>
{
	internal const int MaxAccessKeyLength = 6;

	// Collection of characters parsed from the UI element owner's AutomationProperties.AccessKey field.
	// Declaring this with room for a null terminator.
	private readonly char[] _accessKey = new char[MaxAccessKeyLength + 1];

	internal AKAccessKey()
	{
	}

	internal AKAccessKey(char accessKey)
	{
		Set(accessKey);
	}

	internal AKAccessKey(string accessKey)
	{
		Set(accessKey);
	}

	/// <summary>
	/// Gets the access key string.
	/// </summary>
	internal string GetAccessKeyString() => new string(_accessKey, 0, GetLength());

	public static bool operator ==(AKAccessKey? left, AKAccessKey? right) =>
		left is null ? right is null : left.Equals(right);

	public static bool operator !=(AKAccessKey? left, AKAccessKey? right) => !(left == right);

	private int GetLength()
	{
		for (int i = 0; i < MaxAccessKeyLength; i++)
		{
			if (_accessKey[i] == '\0')
			{
				return i;
			}
		}
		return MaxAccessKeyLength;
	}
}
