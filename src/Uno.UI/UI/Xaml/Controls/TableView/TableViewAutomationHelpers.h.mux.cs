// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewAutomationHelpers.h, tag winui3/main, commit dc28206ea35

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Shared helpers for the TableView automation peers, so the visible-column and column-header-string
// logic lives in one place instead of being copy-pasted across the peer translation units. Assumes
// pch.h (winrt type aliases) is included first, per the TableView header convention.

// TODO Uno: a value-type member in C++; a class here, so holders own one instance and mutate it via Track.
internal sealed class TableViewTrackedItemIdentity
{
	public void Track(object? item)
	{
		m_hasItem = item is not null;
		m_weakItem = null;
		m_strongItem = null;

		if (item is null)
		{
			return;
		}

		// TODO Uno: every .NET object is weakly referenceable. Boxed primitives and strings stand in for
		// the IPropertyValue boxes that do not implement IWeakReferenceSource in C++; a box is often
		// created per ItemsSource read, so a weak reference to it would die immediately.
		// Original C++: if (item.try_as<::IWeakReferenceSource>())
		if (!(item.GetType().IsValueType || item is string))
		{
			m_weakItem = new WeakReference<object>(item);
		}
		else
		{
			// Some app data items do not implement IWeakReferenceSource. Hold those narrowly as
			// strong identity tokens: they are ItemsSource objects, not peers/containers, so this
			// does not add a reference path back from app data to TableView.
			m_strongItem = item;
		}
	}

	public bool IsTracking() => m_hasItem;

	public object? Resolve()
	{
		if (m_strongItem is not null)
		{
			return m_strongItem;
		}

		return m_weakItem is not null && m_weakItem.TryGetTarget(out var item) ? item : null;
	}

	public bool SameIdentityAs(object? candidate)
	{
		var item = Resolve();
		return candidate is not null && item is not null && TableView.SameInspectableIdentity(candidate, item);
	}

	private bool m_hasItem;
	private WeakReference<object>? m_weakItem;
	private object? m_strongItem;
}

// TODO Uno: free inline functions in C++; they live in a static class here.
internal static class TableViewAutomationHelpers
{
	// Resource lookups feeding UIA are supplementary: degrade instead of letting a missing PRI (a host
	// app that does not merge the control's resources) escape into a UIA call.
	internal static string TryGetLocalizedString(string resourceName)
	{
		try
		{
			return ResourceAccessor.GetLocalizedStringResource(resourceName);
		}
		catch (Exception)
		{
			return string.Empty;
		}
	}

	internal static string LocalizedOrFallbackForTableViewAutomation(string resourceName, string fallback)
	{
		try
		{
			var resolved = ResourceAccessor.GetLocalizedStringResource(resourceName);
			if (!string.IsNullOrEmpty(resolved))
			{
				return resolved;
			}
		}
		catch (Exception)
		{
		}

		return fallback;
	}

	internal static string FormatLocalizedOrFallback(
		string resourceName,
		string fallback,
		string first,
		string second,
		string finalSeparator)
	{
		var format = LocalizedOrFallbackForTableViewAutomation(resourceName, fallback);
		var formatted = StringUtil.FormatString(format, first, second, "", "");
		if (!string.IsNullOrEmpty(formatted))
		{
			return formatted;
		}

		formatted = StringUtil.FormatString(fallback, first, second, "", "");
		if (!string.IsNullOrEmpty(formatted))
		{
			return formatted;
		}

		return first + finalSeparator + second;
	}

	internal static string FormatUIntForItemName(ulong value)
	{
		try
		{
			if (TableViewDetails.CreateCurrentCultureDecimalFormatter() is { } formatter)
			{
				formatter.FractionDigits = 0;
				return formatter.FormatUInt(value);
			}
		}
		catch (Exception)
		{
		}

		return value.ToString(CultureInfo.InvariantCulture);
	}

	internal static string FormatDoubleForItemName(double value)
	{
		try
		{
			if (TableViewDetails.CreateCurrentCultureDecimalFormatter() is { } formatter)
			{
				formatter.FractionDigits = 0;
				return formatter.FormatDouble(value);
			}
		}
		catch (Exception)
		{
		}

		return CppWinRTHelpers.ToHString(value);
	}

	internal static bool IsVisibleColumn(TableViewColumn? column) =>
		column is not null && column.Visibility == Visibility.Visible;

	internal static AutomationPeer? GetRealizedColumnHeaderPeer(
		TableView? table,
		TableViewColumn? column)
	{
		if (table is null || !IsVisibleColumn(column) ||
			column!.GetOwningTableView() != table)
		{
			return null;
		}

		if (FrameworkElementAutomationPeer.CreatePeerForElement(table) is TableViewAutomationPeer tablePeer)
		{
			if (tablePeer.GetOrCreateColumnHeaderPeer(table, column) is { } headerPeer)
			{
				return headerPeer;
			}
		}

		if (table.GetHeaderHostInternal() is { } host)
		{
			if (TableViewCellsPanel.CellForColumn(host, column) is { } header)
			{
				return FrameworkElementAutomationPeer.CreatePeerForElement(header);
			}
		}
		return null;
	}

	// Stringifies a data item for UIA. Shared by the TableView peer's item search and the row peer's
	// name fallback, so both describe the same item the same way.
	internal static string ItemToName(object? item)
	{
		// Boxed WinRT primitives surface as IPropertyValue, not IStringable.
		// TODO Uno: IPropertyValue projection. try_as<IPropertyValue>() + Type() becomes ValueConversionHelpers.TryGetPropertyType.
		if (item is not null && ValueConversionHelpers.TryGetPropertyType(item, out var propertyType))
		{
			switch (propertyType)
			{
				case PropertyType.String: return (string)item;
				case PropertyType.Boolean: return (bool)item ? LocalizedOrFallbackForTableViewAutomation(ResourceAccessor.SR_TableViewBooleanTrue, "True") : LocalizedOrFallbackForTableViewAutomation(ResourceAccessor.SR_TableViewBooleanFalse, "False");
				case PropertyType.Int16: return TableViewDetails.FormatIntegerForCurrentCulture((short)item);
				case PropertyType.Int32: return TableViewDetails.FormatIntegerForCurrentCulture((int)item);
				case PropertyType.Int64: return TableViewDetails.FormatIntegerForCurrentCulture((long)item);
				case PropertyType.UInt8: return FormatUIntForItemName((byte)item);
				case PropertyType.UInt16: return FormatUIntForItemName((ushort)item);
				case PropertyType.UInt32: return FormatUIntForItemName((uint)item);
				case PropertyType.UInt64: return FormatUIntForItemName((ulong)item);
				case PropertyType.Single: return FormatDoubleForItemName((float)item);
				case PropertyType.Double: return FormatDoubleForItemName((double)item);
				default: break;
			}
		}

		if (SharedHelpers.IsStringable(item))
		{
			return SharedHelpers.StringableToString(item);
		}

		return string.Empty;
	}

	internal static string GroupInfoToName(TableViewGroupInfo info)
	{
		var keyText = info.KeyText;
		var countText = info.ItemCountText;
		if (!string.IsNullOrEmpty(keyText) && !string.IsNullOrEmpty(countText))
		{
			return FormatLocalizedOrFallback(ResourceAccessor.SR_TableViewGroupHeaderNameFormat, "%1!s! %2!s!", keyText, countText, " ");
		}
		return keyText;
	}

	internal static int CountVisibleColumns(IList<TableViewColumn> columns)
	{
		var count = 0;
		foreach (var column in columns)
		{
			if (IsVisibleColumn(column))
			{
				++count;
			}
		}
		return count;
	}

	// Returns the text a cell displays: the column-generated TextBlock's text, else the content's own
	// computed UIA name. Shared so a cell's name and the row name composed from its cells agree.
	//
	// allowPeerCreation gates the fallback: CreatePeerForElement does not just read a name, it creates
	// and permanently attaches a peer. Worth it for a cell naming itself; not for the row name, which
	// walks every cell on every name query, so it passes false and skips template content.
	internal static string GetCellContentName(
		FrameworkElement? content, bool allowPeerCreation, uint depth, ref uint remaining)
	{
		if (content is null || content.Visibility != Visibility.Visible || depth == 0 || remaining == 0)
		{
			return string.Empty;
		}
		--remaining;

		var name = AutomationProperties.GetName(content);
		if (!string.IsNullOrEmpty(name))
		{
			return name;
		}
		if (AutomationProperties.GetLabeledBy(content) is { } label)
		{
			var labelPeer = allowPeerCreation
				? FrameworkElementAutomationPeer.CreatePeerForElement(label)
				: FrameworkElementAutomationPeer.FromElement(label);
			if (labelPeer is not null)
			{
				var labelName = labelPeer.GetName();
				if (!string.IsNullOrEmpty(labelName))
				{
					return labelName;
				}
			}
		}
		if (content is TextBlock textBlock)
		{
			return textBlock.Text;
		}

		// Templated presenter peers can stringify the data object instead of the visible template.
		var presenter = content as ContentPresenter;
		if (presenter is null || presenter.ContentTemplate is null)
		{
			var peer = allowPeerCreation
				? FrameworkElementAutomationPeer.CreatePeerForElement(content)
				: FrameworkElementAutomationPeer.FromElement(content);
			if (peer is not null)
			{
				var peerName = peer.GetName();
				if (!string.IsNullOrEmpty(peerName))
				{
					return peerName;
				}
			}
		}

		// A named control describes its own content; do not repeat its inner interactive labels.
		// Only traverse layout wrappers, and bound the work of each cell-name query.
		if (presenter is not null || content is Panel || content is Border)
		{
			var count = VisualTreeHelper.GetChildrenCount(content);
			for (int i = 0; i < count && remaining > 0; ++i)
			{
				if (VisualTreeHelper.GetChild(content, i) is FrameworkElement child)
				{
					var childName = GetCellContentName(child, allowPeerCreation, depth - 1, ref remaining);
					if (!string.IsNullOrEmpty(childName))
					{
						return childName;
					}
				}
			}
		}
		return string.Empty;
	}

	internal static FrameworkElement? GetCellContentElement(FrameworkElement? cell)
	{
		if (cell is null)
		{
			return null;
		}
		if (cell is Border border)
		{
			return border.Child as FrameworkElement;
		}
		if (cell is Grid grid && grid.Tag is TableViewColumn)
		{
			var children = grid.Children;
			return children.Count > 0 ? children[0] as FrameworkElement : null;
		}
		return cell;
	}

	internal static FrameworkElement? GetCellAutomationContent(FrameworkElement? cell) =>
		GetCellContentElement(cell);

	internal static bool IsFocusableCellContent(UIElement? element)
	{
		if (element is null || element.Visibility != Visibility.Visible)
		{
			return false;
		}

		return element is Control control && control.IsEnabled && control.IsTabStop;
	}

	internal static bool ContainsFocusableElement(
		UIElement? element,
		uint depthBudget = 8)
	{
		uint localBudget = 64;
		return ContainsFocusableElement(element, depthBudget, ref localBudget);
	}

	// TODO Uno: C++ takes an optional uint32_t* budget defaulting to a local 64; C# splits it into an overload with a ref budget.
	internal static bool ContainsFocusableElement(
		UIElement? element,
		uint depthBudget,
		ref uint budget)
	{
		if (element is null || element.Visibility != Visibility.Visible || depthBudget == 0 || budget == 0)
		{
			return false;
		}

		--budget;
		if (IsFocusableCellContent(element))
		{
			return true;
		}

		const int maxChildrenPerLevel = 32;
		var childCount = VisualTreeHelper.GetChildrenCount(element);
		for (int i = 0; i < childCount && i < maxChildrenPerLevel && budget > 0; ++i)
		{
			if (VisualTreeHelper.GetChild(element, i) is UIElement child)
			{
				if (ContainsFocusableElement(child, depthBudget - 1, ref budget))
				{
					return true;
				}
			}
		}

		return false;
	}

	internal static bool HasInteractiveCellContent(FrameworkElement? cell) =>
		ContainsFocusableElement(GetCellAutomationContent(cell));

	internal static void SetAccessibilityViewIfNeeded(
		FrameworkElement? element,
		AccessibilityView view)
	{
		if (element is not null && AutomationProperties.GetAccessibilityView(element) != view)
		{
			AutomationProperties.SetAccessibilityView(element, view);
		}
	}

	internal static bool ShouldPreserveCellContentElement(FrameworkElement? element)
	{
		if (element is null)
		{
			return false;
		}

		if (element is ProgressBar)
		{
			return true;
		}

		if (element is Image)
		{
			return !string.IsNullOrEmpty(AutomationProperties.GetName(element)) ||
				AutomationProperties.GetLabeledBy(element) is not null;
		}

		return false;
	}

	internal static void SetCellContentAccessibilityViewRaw(FrameworkElement? root, uint depthBudget = 8)
	{
		if (root is null || root.Visibility != Visibility.Visible || depthBudget == 0 ||
			ShouldPreserveCellContentElement(root))
		{
			return;
		}

		SetAccessibilityViewIfNeeded(root, AccessibilityView.Raw);

		const int maxChildrenPerLevel = 32;
		var childCount = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < childCount && i < maxChildrenPerLevel; ++i)
		{
			if (VisualTreeHelper.GetChild(root, i) is FrameworkElement child)
			{
				SetCellContentAccessibilityViewRaw(child, depthBudget - 1);
			}
		}
	}

	internal static void SetInteractiveCellContentAccessibilityViewContent(FrameworkElement? root, uint depthBudget = 8)
	{
		if (root is null || root.Visibility != Visibility.Visible || depthBudget == 0)
		{
			return;
		}

		if (IsFocusableCellContent(root))
		{
			SetAccessibilityViewIfNeeded(root, AccessibilityView.Content);
			return;
		}

		const int maxChildrenPerLevel = 32;
		var childCount = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < childCount && i < maxChildrenPerLevel; ++i)
		{
			if (VisualTreeHelper.GetChild(root, i) is FrameworkElement child)
			{
				SetInteractiveCellContentAccessibilityViewContent(child, depthBudget - 1);
			}
		}
	}

	internal static string GetCellDisplayText(FrameworkElement? cell, bool allowPeerCreation = true)
	{
		var content = GetCellContentElement(cell);
		uint remaining = 32;
		return GetCellContentName(content, allowPeerCreation, 8, ref remaining);
	}

	// Returns the column Header's string form (an IStringable, or a String-typed IPropertyValue), or
	// nullopt when the header is not a string (or the column is null). Returning nullopt rather than an
	// empty string lets callers distinguish "no string header" from "an explicitly empty string header".
	internal static string? TryGetColumnHeaderString(TableViewColumn? column)
	{
		if (column is not null)
		{
			var header = column.Header;
			if (SharedHelpers.IsStringable(header))
			{
				return SharedHelpers.StringableToString(header);
			}
			// TODO Uno: IPropertyValue projection
			if (header is not null)
			{
				if (ValueConversionHelpers.GetPropertyType(header.GetType()) == PropertyType.String)
				{
					return (string)header;
				}
			}
		}
		return null;
	}
}
