// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// Windows App SDK Reference dev/AppNotifications/AppNotificationUtility.h and dev/AppLifecycle/AppInstance.cpp, commit 6b178e79e59d28efb10ef5c8c68b051d2615c3e6

#nullable enable

using System;

namespace Uno.UI.Runtime.Skia.Win32;

internal static class Win32AppNotificationActivation
{
	private const string AppNotificationArgument = "----AppNotificationActivated:";
	private const string ProtocolQualifier = "----ms-protocol:";
	private const string PushQualifier = "----WindowsAppRuntimePushServer:";

	public static bool IsAppNotificationLaunch(ReadOnlySpan<string> arguments)
	{
		var hasAppNotificationArgument = false;
		foreach (var argument in arguments)
		{
			// AppInstance searches these qualifiers anywhere in an argument, ahead of the toast contract.
			if (argument.Contains(ProtocolQualifier, StringComparison.Ordinal) ||
				argument.Contains(PushQualifier, StringComparison.Ordinal))
			{
				return false;
			}

			// The notification COM server registers this complete argument, not a prefix with a payload.
			hasAppNotificationArgument |= string.Equals(argument, AppNotificationArgument, StringComparison.Ordinal);
		}
		return hasAppNotificationArgument;
	}
}
