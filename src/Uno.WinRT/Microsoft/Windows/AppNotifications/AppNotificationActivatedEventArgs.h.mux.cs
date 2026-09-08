// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

// Windows App SDK Reference dev/AppNotifications/AppNotificationActivatedEventArgs.h, commit 6b178e79e59d28efb10ef5c8c68b051d2615c3e6

#nullable enable

using System.Collections.Generic;
using Windows.Foundation.Metadata;

namespace Microsoft.Windows.AppNotifications;

partial class AppNotificationActivatedEventArgs
{
	internal AppNotificationActivatedEventArgs(string argument, IDictionary<string, string>? userInput = null)
	{
		m_argument = argument ?? string.Empty;
		m_userInput = SnapshotUserInput(userInput);
		m_arguments = DecodeArguments(m_argument);
	}

	public string Argument => m_argument;

	public IDictionary<string, string> UserInput => m_userInput;

	[ContractVersion(typeof(AppNotificationsContract), 3 * 0x10000u)]
	public IDictionary<string, string> Arguments => m_arguments;

	private readonly string m_argument;
	private readonly IDictionary<string, string> m_userInput;
	private readonly IDictionary<string, string> m_arguments;
}
