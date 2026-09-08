// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

// Windows App SDK Reference dev/AppNotifications/AppNotificationBuilder/AppNotificationComboBox.h, commit 6b178e79e59d28efb10ef5c8c68b051d2615c3e6

#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.Windows.AppNotifications.Builder;

partial class AppNotificationComboBox
{
	// Properties
	public IDictionary<string, string> Items
	{
		get => m_items;
		set => m_items = value ?? new Dictionary<string, string>();
	}

	public string Title
	{
		get => m_title;
		set => m_title = value ?? string.Empty;
	}

	public string SelectedItem
	{
		get => m_selectedItem;
		set => m_selectedItem = value ?? string.Empty;
	}

	private string m_id = string.Empty;
	private IDictionary<string, string> m_items = new SortedDictionary<string, string>(StringComparer.Ordinal);
	private string m_title = string.Empty;
	private string m_selectedItem = string.Empty;
}
