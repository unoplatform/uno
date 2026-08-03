#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Windows.AppNotifications.Builder;

public sealed class AppNotificationComboBox
{
	private readonly string _id;
	private IDictionary<string, string> _items = new SortedDictionary<string, string>(StringComparer.Ordinal);
	private string _title = string.Empty;
	private string _selectedItem = string.Empty;

	public AppNotificationComboBox(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			throw new ArgumentException("A combo box ID is required.", nameof(id));
		}

		_id = AppNotificationBuilderUtility.EncodeXml(id);
	}

	public IDictionary<string, string> Items
	{
		get => _items;
		set => _items = value ?? new Dictionary<string, string>();
	}

	public string Title
	{
		get => _title;
		set => _title = value ?? string.Empty;
	}

	public string SelectedItem
	{
		get => _selectedItem;
		set => _selectedItem = value ?? string.Empty;
	}

	public AppNotificationComboBox AddItem(string id, string content)
	{
		if (_items.Count >= AppNotificationBuilderUtility.MaxSelectionElements)
		{
			throw new ArgumentException("A combo box supports at most five items.", nameof(id));
		}

		if (string.IsNullOrEmpty(id))
		{
			throw new ArgumentException("A combo box item ID is required.", nameof(id));
		}

		_items[AppNotificationBuilderUtility.EncodeXml(id)] = AppNotificationBuilderUtility.EncodeXml(content);
		return this;
	}

	public AppNotificationComboBox SetTitle(string value)
	{
		_title = AppNotificationBuilderUtility.EncodeXml(value);
		return this;
	}

	public AppNotificationComboBox SetSelectedItem(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			throw new ArgumentException("A selected item ID is required.", nameof(id));
		}

		_selectedItem = AppNotificationBuilderUtility.EncodeXml(id);
		return this;
	}

	internal string ToXml()
	{
		var xml = new StringBuilder($"<input id='{_id}' type='selection'");
		if (_title.Length > 0)
		{
			xml.Append($" title='{_title}'");
		}
		if (_selectedItem.Length > 0)
		{
			xml.Append($" defaultInput='{_selectedItem}'");
		}
		xml.Append('>');

		foreach (var item in _items)
		{
			xml.Append($"<selection id='{item.Key}' content='{item.Value}'/>");
		}

		xml.Append("</input>");
		return xml.ToString();
	}
}
