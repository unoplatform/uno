// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

// Windows App SDK Reference dev/AppNotifications/AppNotificationBuilder/AppNotificationProgressBar.h, commit 6b178e79e59d28efb10ef5c8c68b051d2615c3e6

#nullable enable

namespace Microsoft.Windows.AppNotifications.Builder;

partial class AppNotificationProgressBar
{
	// Properties
	// Setting these properties will remove the data binding with a static value
	public string Title
	{
		get => m_title;
		set => SetTitleValue(value);
	}

	public string Status
	{
		get => m_status;
		set => SetStatusValue(value);
	}

	public double Value
	{
		get => m_value;
		set => SetValueCore(value);
	}

	public string ValueStringOverride
	{
		get => m_valueStringOverride;
		set => SetValueStringOverrideValue(value);
	}

	private enum BindMode
	{
		NotSet,
		Bind,
		Value,
	}

	private BindMode m_titleBindMode;
	private string m_title = string.Empty;
	private BindMode m_statusBindMode;
	private string m_status = string.Empty;
	private BindMode m_valueBindMode;
	private double m_value;
	private BindMode m_valueStringOverrideBindMode;
	private string m_valueStringOverride = string.Empty;
}
