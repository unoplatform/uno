// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

// Windows App SDK Reference dev/AppNotifications/AppNotificationBuilder/AppNotificationProgressBar.cpp, commit 6b178e79e59d28efb10ef5c8c68b051d2615c3e6

#nullable enable

using System.Globalization;

namespace Microsoft.Windows.AppNotifications.Builder;

partial class AppNotificationProgressBar
{
	// AppNotificationProgressBar binds to AppNotificationProgressData so the AppNotification will
	// receive every update to the status and value. In the WinAppSDK, these binding
	// values are static, so developers won't need to define these binding values
	// themselves.
	public AppNotificationProgressBar()
	{
		m_titleBindMode = BindMode.NotSet;
		m_statusBindMode = BindMode.NotSet;
		m_valueBindMode = BindMode.NotSet;
		m_valueStringOverrideBindMode = BindMode.NotSet;
	}

	public AppNotificationProgressBar SetTitle(string value)
	{
		SetTitleValue(value);

		return this;
	}

	public AppNotificationProgressBar BindTitle()
	{
		m_titleBindMode = BindMode.Bind;

		return this;
	}

	public AppNotificationProgressBar SetStatus(string value)
	{
		SetStatusValue(value);

		return this;
	}

	public AppNotificationProgressBar BindStatus()
	{
		m_statusBindMode = BindMode.Bind;

		return this;
	}

	public AppNotificationProgressBar SetValue(double value)
	{
		SetValueCore(value);

		return this;
	}

	public AppNotificationProgressBar BindValue()
	{
		m_valueBindMode = BindMode.Bind;

		return this;
	}

	public AppNotificationProgressBar SetValueStringOverride(string value)
	{
		SetValueStringOverrideValue(value);

		return this;
	}

	public AppNotificationProgressBar BindValueStringOverride()
	{
		m_valueStringOverrideBindMode = BindMode.Bind;

		return this;
	}

	// IStringable
	public override string ToString()
	{
		var title = $" title='{(m_titleBindMode == BindMode.Value ? NormalizeXmlAttribute(m_title) : "{progressTitle}")}'";
		var status = $" status='{(m_statusBindMode == BindMode.Value ? NormalizeXmlAttribute(m_status) : "{progressStatus}")}'";
		var value = $" value='{(m_valueBindMode == BindMode.Value ? m_value.ToString("G6", CultureInfo.InvariantCulture) : "{progressValue}")}'";
		var valueStringOverride = $" valueStringOverride='{(m_valueStringOverrideBindMode == BindMode.Value ? NormalizeXmlAttribute(m_valueStringOverride) : "{progressValueString}")}'";

		return $"<progress{(m_titleBindMode == BindMode.NotSet ? string.Empty : title)}{status}{value}{(m_valueStringOverrideBindMode == BindMode.NotSet ? string.Empty : valueStringOverride)}/>";
	}
}
