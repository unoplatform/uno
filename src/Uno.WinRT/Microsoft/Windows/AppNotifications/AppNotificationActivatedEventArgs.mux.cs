// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// Windows App SDK Reference dev/AppNotifications/AppNotificationActivatedEventArgs.cpp, commit 6b178e79e59d28efb10ef5c8c68b051d2615c3e6

#nullable enable

using System.Collections.Generic;
using Microsoft.Windows.AppNotifications.Internal;

namespace Microsoft.Windows.AppNotifications;

partial class AppNotificationActivatedEventArgs
{
	// Uno: the portable payload decoder shares this stateless implementation.
	internal static IDictionary<string, string> DecodeArguments(string arguments)
	{
		var result = new Dictionary<string, string>();
		var pairs = new List<string>();
		int pos;

		// Separate the key/value pairs by ';' as the delimiter
		while ((pos = arguments.IndexOf(';')) >= 0)
		{
			pairs.Add(arguments[..pos]);
			arguments = arguments[(pos + 1)..];
		}

		// Need to push back final string
		pairs.Add(arguments);

		foreach (var pair in pairs)
		{
			// Get the key/value individual values separated by '='
			pos = pair.IndexOf('=');
			if (pos < 0)
			{
				result[AppNotificationArgumentCodec.DecodeComponent(pair)] = string.Empty;
			}
			else
			{
				result[AppNotificationArgumentCodec.DecodeComponent(pair[..pos])] =
					AppNotificationArgumentCodec.DecodeComponent(pair[(pos + 1)..]);
			}
		}
		return result;
	}
}
