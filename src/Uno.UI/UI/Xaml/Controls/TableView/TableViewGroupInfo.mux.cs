// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewGroupInfo.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using System.ComponentModel;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using static Uno.UI.Helpers.WinUI.ResourceAccessor;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewGroupInfo
{
	// Every step here can fail on a locale-starved or self-contained host, and this runs inside
	// a binding getter during measure -- so nothing is allowed to escape.
	private static string FormatGroupHeaderCountText(int groupItemCount)
	{
		string countFormat = "";
		try
		{
			countFormat = ResourceAccessor.GetLocalizedStringResource(SR_TableViewGroupHeaderCountFormat) ?? "";
		}
		catch (Exception)
		{
		}

		if (string.IsNullOrEmpty(countFormat))
		{
			countFormat = "({0})";
		}

		var formattedCount = TableViewDetails.FormatIntegerForCurrentCulture(groupItemCount);
		var placeholderIndex = countFormat.IndexOf("{0}", StringComparison.Ordinal);
		if (placeholderIndex != -1)
		{
			countFormat = countFormat.Remove(placeholderIndex, 3).Insert(placeholderIndex, formattedCount);
		}
		else
		{
			countFormat += formattedCount;
		}

		return countFormat;
	}

	internal TableViewGroupInfo(
		object? key,
		int itemCount,
		int level,
		bool isExpandable,
		bool isExpanded,
		string keyText)
	{
		m_itemCount = itemCount;
		m_level = level;
		m_isExpandable = isExpandable;
		m_isExpanded = isExpanded;
		m_keyText = keyText;
		m_key = key;
	}

	public object? Key => m_key;

	public int ItemCount => m_itemCount;

	public int Level => m_level;

	public bool IsExpandable => m_isExpandable;

	public bool IsExpanded => m_isExpanded;

	public string KeyText => m_keyText;

	public string ItemCountText
	{
		get
		{
			if (!m_hasItemCountText)
			{
				m_itemCountText = FormatGroupHeaderCountText(m_itemCount);
				m_hasItemCountText = true;
			}
			return m_itemCountText;
		}
	}

	// TODO Uno: Original C++ PropertyChanged add/remove accessors over m_propertyChanged:
	// winrt::event_token TableViewGroupInfo::PropertyChanged(winrt::PropertyChangedEventHandler const& value)
	// {
	//     return m_propertyChanged.add(value);
	// }
	//
	// void TableViewGroupInfo::PropertyChanged(winrt::event_token const& token)
	// {
	//     m_propertyChanged.remove(token);
	// }
	// The PropertyChanged event is a field-like C# event declared in TableViewGroupInfo.Properties.cs.

	private void RaisePropertyChanged(string propertyName)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	// C++/WinRT's operator== compares the ABI pointer of the interface currently held, with no
	// QI, so the same object reached through different interfaces compares unequal. Boxed keys
	// are also re-created per projection pass, so two *equal* keys are distinct objects --
	// identity alone would still raise a spurious change every render. Compare boxed values by
	// value first, then fall back to COM identity for reference keys.
	// TODO Uno: a free function in C++; C# has no free functions, so it is a static member of TableViewGroupInfo.
	internal static bool SameGroupKey(object? left, object? right)
	{
		if (left is null || right is null)
		{
			return left is null && right is null;
		}

		try
		{
			// TODO Uno: IPropertyValue projection. try_as<IPropertyValue> succeeds for boxed WinRT values,
			// which on .NET are boxed value types and strings; Type() comes from ValueConversionHelpers.GetPropertyType.
			var leftValue = TryGetPropertyType(left, out var leftType);
			var rightValue = TryGetPropertyType(right, out var rightType);
			if (leftValue && rightValue)
			{
				if (leftType != rightType)
				{
					return false;
				}

				switch (leftType)
				{
					case PropertyType.String:
						return (string)left == (string)right;
					case PropertyType.Int32:
						return (int)left == (int)right;
					case PropertyType.Int64:
						return (long)left == (long)right;
					case PropertyType.UInt32:
						return (uint)left == (uint)right;
					case PropertyType.UInt64:
						return (ulong)left == (ulong)right;
					case PropertyType.Boolean:
						return (bool)left == (bool)right;
					case PropertyType.Double:
						return (double)left == (double)right;
					case PropertyType.Single:
						return (float)left == (float)right;
					case PropertyType.UInt8:
						return (byte)left == (byte)right;
					case PropertyType.Int16:
						return (short)left == (short)right;
					case PropertyType.UInt16:
						return (ushort)left == (ushort)right;
					case PropertyType.Char16:
						return (char)left == (char)right;
					// Grouping by a date or duration is common (orders by day, tasks by elapsed
					// time); without these the key falls to identity and re-raises every render.
					case PropertyType.DateTime:
						return GetDateTime(left) == GetDateTime(right);
					case PropertyType.TimeSpan:
						return (TimeSpan)left == (TimeSpan)right;
					case PropertyType.Guid:
						return (Guid)left == (Guid)right;
					default:
						break;
				}
			}

			// TODO Uno: Original C++ compares the canonical IUnknown pointers obtained through try_as<::IUnknown>.
			return ReferenceEquals(left, right);
		}
		catch (Exception)
		{
			return false;
		}
	}

	// TODO Uno: IPropertyValue projection helper. Boxed value types and strings are the .NET shape of a WinRT
	// IPropertyValue; any other reference type fails try_as<IPropertyValue>.
	private static bool TryGetPropertyType(object value, out PropertyType type)
	{
		if (value is string || value.GetType().IsValueType)
		{
			type = ValueConversionHelpers.GetPropertyType(value.GetType());
			return true;
		}

		type = PropertyType.Empty;
		return false;
	}

	// TODO Uno: IPropertyValue.GetDateTime() projection. A WinRT DateTime surfaces on .NET as either
	// DateTimeOffset or DateTime; both compare by their UTC instant, as the WinRT DateTime ticks do.
	private static DateTimeOffset GetDateTime(object value)
		=> value is DateTimeOffset dateTimeOffset ? dateTimeOffset : new DateTimeOffset((DateTime)value);

	internal void UpdateInternal(
		object? key,
		int itemCount,
		int level,
		string keyText)
	{
		if (!SameGroupKey(m_key, key))
		{
			m_key = key;
			RaisePropertyChanged("Key");
		}

		if (m_keyText != keyText)
		{
			m_keyText = keyText;
			RaisePropertyChanged("KeyText");
		}

		if (m_itemCount != itemCount)
		{
			m_itemCount = itemCount;
			// Invalidate the derived string too; recomputed only if something binds it.
			m_hasItemCountText = false;
			RaisePropertyChanged("ItemCount");
			RaisePropertyChanged("ItemCountText");
		}

		if (m_level != level)
		{
			m_level = level;
			RaisePropertyChanged("Level");
		}
	}

	internal void SetExpansionInternal(bool isExpandable, bool isExpanded)
	{
		if (m_isExpandable != isExpandable)
		{
			m_isExpandable = isExpandable;
			RaisePropertyChanged("IsExpandable");
		}

		if (m_isExpanded != isExpanded)
		{
			m_isExpanded = isExpanded;
			RaisePropertyChanged("IsExpanded");
		}
	}
}
