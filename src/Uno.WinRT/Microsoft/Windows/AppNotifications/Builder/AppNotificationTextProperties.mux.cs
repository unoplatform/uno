// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

// Windows App SDK Reference dev/AppNotifications/AppNotificationBuilder/AppNotificationTextProperties.cpp, commit 6b178e79e59d28efb10ef5c8c68b051d2615c3e6

#nullable enable

namespace Microsoft.Windows.AppNotifications.Builder;

partial class AppNotificationTextProperties
{
	// Fluent Setters
	public AppNotificationTextProperties SetLanguage(string value)
	{
		m_language = value ?? string.Empty;
		return this;
	}

	public AppNotificationTextProperties SetIncomingCallAlignment()
	{
		m_useCallScenarioAlign = true;
		return this;
	}

	public AppNotificationTextProperties SetMaxLines(int value)
	{
		m_maxLines = value;
		return this;
	}

	// IStringable
	public override string ToString()
	{
		var language = m_language.Length > 0 ? $" lang='{NormalizeXmlAttribute(m_language)}'" : string.Empty;
		var callScenarioAlign = m_useCallScenarioAlign ? " hint-callScenarioCenterAlign='true'" : string.Empty;
		var hintMaxLines = m_maxLines != 0 ? $" hint-maxLines='{m_maxLines}'" : string.Empty;

		return $"<text{language}{hintMaxLines}{callScenarioAlign}>";
	}
}
