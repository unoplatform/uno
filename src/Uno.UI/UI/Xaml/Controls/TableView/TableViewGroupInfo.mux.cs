// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewGroupInfo.cpp, tag winui3/main, commit 79209e041

#nullable enable

using System;
using System.ComponentModel;
using Microsoft.UI.Xaml.Data;
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

	private sealed class GroupInfoProperty : ICustomProperty
	{
		private readonly string m_name;
		private readonly Type m_type;
		private readonly Func<TableViewGroupInfo, object?> m_getter;

		public GroupInfoProperty(string name, Type type, Func<TableViewGroupInfo, object?> getter)
		{
			m_name = name;
			m_type = type;
			m_getter = getter;
		}

		public Type Type => m_type;
		public string Name => m_name;
		public bool CanRead => true;
		public bool CanWrite => false;

		public object GetValue(object target) => m_getter((TableViewGroupInfo)target)!;

		public void SetValue(object target, object value) => throw new NotImplementedException();

		public object GetIndexedValue(object target, object index) => throw new NotImplementedException();

		public void SetIndexedValue(object target, object value, object index) => throw new NotImplementedException();
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
		PropertyChangedEventArgs args = new(propertyName);
		CppWinRTHelpers.InvokeWinRTEvent(ref PropertyChanged, handler => handler(this, args));
	}

	// Uno specific: System.Type replaces winrt::TypeName.
	Type ICustomPropertyProvider.Type => GetType();

	ICustomProperty ICustomPropertyProvider.GetCustomProperty(string name)
	{
		switch (name)
		{
			case "Key":
				return new GroupInfoProperty("Key", typeof(object), info => info.Key);
			case "KeyText":
				return new GroupInfoProperty("KeyText", typeof(string), info => info.KeyText);
			case "ItemCount":
				return new GroupInfoProperty("ItemCount", typeof(int), info => info.ItemCount);
			case "ItemCountText":
				return new GroupInfoProperty("ItemCountText", typeof(string), info => info.ItemCountText);
			case "Level":
				return new GroupInfoProperty("Level", typeof(int), info => info.Level);
			case "IsExpandable":
				return new GroupInfoProperty("IsExpandable", typeof(bool), info => info.IsExpandable);
			case "IsExpanded":
				return new GroupInfoProperty("IsExpanded", typeof(bool), info => info.IsExpanded);
		}

		return null!;
	}

	ICustomProperty ICustomPropertyProvider.GetIndexedProperty(string name, Type type) => null!;

	string ICustomPropertyProvider.GetStringRepresentation() => m_keyText;

	/// <inheritdoc />
	public override string ToString() => m_keyText;

	// C++/WinRT's operator== compares the ABI pointer of the interface currently held, with no
	// QI, so the same object reached through different interfaces compares unequal. Boxed keys
	// are also re-created per projection pass, so two *equal* keys are distinct objects --
	// identity alone would still raise a spurious change every render. Compare boxed values by
	// value first, then fall back to COM identity for reference keys.
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
			var leftValue = ValueConversionHelpers.TryGetPropertyType(left, out var leftType);
			var rightValue = ValueConversionHelpers.TryGetPropertyType(right, out var rightType);
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
					// NaN != NaN, but a NaN key is still one group; treat it as equal so the cached
					// projection is updated in place rather than replaced.
					case PropertyType.Double:
						{
							var l = (double)left;
							var r = (double)right;
							return l == r || (double.IsNaN(l) && double.IsNaN(r));
						}
					case PropertyType.Single:
						{
							var l = (float)left;
							var r = (float)right;
							return l == r || (float.IsNaN(l) && float.IsNaN(r));
						}
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
