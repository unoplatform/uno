// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

// Windows App SDK Reference dev/AppNotifications/AppNotificationBuilder/AppNotificationComboBox.cpp, commit 6b178e79e59d28efb10ef5c8c68b051d2615c3e6

#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Windows.AppNotifications.Builder;

partial class AppNotificationComboBox
{
	public AppNotificationComboBox(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			throw new ArgumentException("A combo box ID is required.", nameof(id));
		}

		// Uno: keep the constructor value raw and encode it once when serializing.
		m_id = id;
	}

	// Fluent Setters
	// Add a selection to the AppNotificationComboBox. The id parameter identifies the item the user selected.
	public AppNotificationComboBox AddItem(string id, string content)
	{
		if (m_items.Count >= AppNotificationBuilderUtility.MaxSelectionElements)
		{
			throw new ArgumentException("A combo box supports at most five items.", nameof(id));
		}
		if (string.IsNullOrEmpty(id))
		{
			throw new ArgumentException("A combo box item ID is required.", nameof(id));
		}

		// Uno: keep mutable CLR map values raw and encode them once when serializing.
		m_items[id] = content ?? string.Empty;

		return this;
	}

	// Adds a title to display on top
	public AppNotificationComboBox SetTitle(string value)
	{
		// Uno: keep the public property raw and encode it once when serializing.
		m_title = value ?? string.Empty;

		return this;
	}

	// Sets the default selection to be displayed by the AppNotificationComboBox
	public AppNotificationComboBox SetSelectedItem(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			throw new ArgumentException("A selected item ID is required.", nameof(id));
		}

		// Uno: keep the public property raw and encode it once when serializing.
		m_selectedItem = id;

		return this;
	}

	private string GetSelectionItems()
	{
		var items = new StringBuilder();
		var encodedItems = new SortedDictionary<string, string>(StringComparer.Ordinal);
		foreach (var pair in m_items)
		{
			encodedItems[NormalizeXmlAttribute(pair.Key)] = NormalizeXmlAttribute(pair.Value);
		}

		foreach (var pair in encodedItems)
		{
			items.Append($"<selection id='{pair.Key}' content='{pair.Value}'/>");
		}

		return items.ToString();
	}

	// IStringable
	public override string ToString()
	{
		var xmlResult = new StringBuilder($"<input id='{NormalizeXmlAttribute(m_id)}' type='selection'");
		if (m_title.Length > 0)
		{
			xmlResult.Append($" title='{NormalizeXmlAttribute(m_title)}'");
		}
		if (m_selectedItem.Length > 0)
		{
			xmlResult.Append($" defaultInput='{NormalizeXmlAttribute(m_selectedItem)}'");
		}
		xmlResult.Append('>');
		xmlResult.Append(GetSelectionItems());
		xmlResult.Append("</input>");

		return xmlResult.ToString();
	}
}
