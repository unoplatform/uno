// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

// Windows App SDK Reference dev/AppNotifications/AppNotificationBuilder/AppNotificationTextProperties.h, commit 6b178e79e59d28efb10ef5c8c68b051d2615c3e6

#nullable enable

namespace Microsoft.Windows.AppNotifications.Builder;

partial class AppNotificationTextProperties
{
	// Properties
	public string Language
	{
		get => m_language;
		set => m_language = value ?? string.Empty;
	}

	public int MaxLines
	{
		get => m_maxLines;
		set => m_maxLines = value;
	}

	public bool IncomingCallAlignment
	{
		get => m_useCallScenarioAlign;
		set => m_useCallScenarioAlign = value;
	}

	private int m_maxLines;
	private string m_language = string.Empty;
	private bool m_useCallScenarioAlign;
}
