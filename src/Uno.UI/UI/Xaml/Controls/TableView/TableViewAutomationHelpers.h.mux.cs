// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewAutomationHelpers.h, tag winui3/main, commit 79209e041

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Documents;
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
	// A stale provider must fail rather than report coordinates that now belong to another element.
	[DoesNotReturn]
	internal static void ThrowElementNotAvailable() =>
		throw new ElementNotAvailableException();

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

	internal const int c_maxCellContentChildrenPerLevel = 32;

	internal static bool ShouldWalkCellContentSubtree(FrameworkElement? content) =>
		content is not null &&
			(content is ContentPresenter ||
				content is Panel ||
				content is Border);

	internal static string GetExplicitCellContentName(
		FrameworkElement content, bool allowPeerCreation)
	{
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
		return string.Empty;
	}

	// Returns the text a cell displays: the column-generated TextBlock's text, else the content's own
	// computed UIA name. Shared so a cell's name and the row name composed from its cells agree.
	//
	// allowPeerCreation gates the fallback: CreatePeerForElement does not just read a name, it creates
	// and permanently attaches a peer. Worth it for a cell naming itself; not for the row name, which
	// walks every cell on every name query, so it passes false and only reads already-realized content.
	internal static string GetCellContentName(
		FrameworkElement? content, bool allowPeerCreation, uint depth, ref uint remaining)
	{
		if (content is null || content.Visibility != Visibility.Visible || depth == 0 || remaining == 0)
		{
			return string.Empty;
		}
		--remaining;

		var explicitName = GetExplicitCellContentName(content, allowPeerCreation);
		if (!string.IsNullOrEmpty(explicitName))
		{
			return explicitName;
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
		// Per-level cap retained: removing it destabilised the column-header peers.
		if (ShouldWalkCellContentSubtree(content))
		{
			var count = VisualTreeHelper.GetChildrenCount(content);
			for (int i = 0; i < count && i < c_maxCellContentChildrenPerLevel && remaining > 0; ++i)
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

	// The name UIA uses for a column: its string header, else an element header's own name (hidden
	// headers collapse their header cells), else the name its realized header item exposes, which is
	// the only source for an object or templated header. Shared by the cell peer and the built-in
	// editor so both name the column the same way.
	internal static string ResolveColumnAccessibleName(TableView? table, TableViewColumn? column)
	{
		if (TableViewDetails.TryGetColumnHeaderText(column) is { } headerString)
		{
			return headerString;
		}

		if (column?.Header is FrameworkElement headerElement)
		{
			uint remaining = 32;
			var name = GetCellContentName(headerElement, false /* allowPeerCreation */, 8, ref remaining);
			if (!string.IsNullOrEmpty(name))
			{
				return name;
			}
		}

		if (table is not null)
		{
			if (GetRealizedColumnHeaderPeer(table, column) is { } headerPeer)
			{
				return headerPeer.GetName();
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

		var childCount = VisualTreeHelper.GetChildrenCount(element);
		for (int i = 0; i < childCount && i < c_maxCellContentChildrenPerLevel && budget > 0; ++i)
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

	// A text control's built-in copy menu is its default value, not an app menu; reading the local value
	// first also avoids creating that default flyout. A Style-set flyout on a text control is not counted.
	internal static bool HasAppContextFlyout(UIElement element)
	{
		if (element is TextBlock || element is RichTextBlock ||
			element is RichTextBlockOverflow || element is TextBox ||
			element is RichEditBox || element is PasswordBox)
		{
			// A local binding that resolves to null, or fails, leaves the built-in menu in effect.
			var local = element.ReadLocalValue(UIElement.ContextFlyoutProperty);
			if (local is null || local == DependencyProperty.UnsetValue)
			{
				return false;
			}
			var flyout = element.ContextFlyout;
			// Uno specific: ReadLocalValue returns the bound value, not the BindingExpression.
			var isBound = local is Microsoft.UI.Xaml.Data.BindingExpression ||
				(element as FrameworkElement)?.GetBindingExpression(UIElement.ContextFlyoutProperty) is not null;
			return flyout is not null && !(isBound && flyout is TextCommandBarFlyout);
		}
		return element.ContextFlyout is not null;
	}

	// An inline Hyperlink (directly or in nested Spans such as Bold/Italic) in a TextBlock or a RichTextBlock paragraph.
	internal static Hyperlink? FindHyperlink(UIElement element)
	{
		static Hyperlink? FindLink(InlineCollection inlines, int depth)
		{
			foreach (var item in inlines)
			{
				if (item is Hyperlink link)
				{
					return link;
				}
				if (item is Span span && depth > 0)
				{
					if (FindLink(span.Inlines, depth - 1) is { } nestedLink)
					{
						return nestedLink;
					}
				}
			}
			return null;
		}

		const int maxSpanDepth = 4;
		if (element is TextBlock textBlock)
		{
			return FindLink(textBlock.Inlines, maxSpanDepth);
		}
		if (element is RichTextBlock richText)
		{
			foreach (var block in richText.Blocks)
			{
				if (block is Paragraph paragraph)
				{
					if (FindLink(paragraph.Inlines, maxSpanDepth) is { } link)
					{
						return link;
					}
				}
			}
		}
		return null;
	}

	internal static bool HasHyperlink(UIElement element) =>
		FindHyperlink(element) is not null;

	// Focusable controls, inline hyperlinks, app context menus or a nested TableView below element. A
	// subtree too deep to scan is assumed interactive rather than made unreachable. IsTabStop is not
	// consulted for TableView-suppressed tab stops; callers pass isSuppressed for those.
	internal static bool HasActionableContent(
		DependencyObject element, int depth, ref int budget,
		Func<UIElement, bool>? isSuppressed = null)
	{
		var count = VisualTreeHelper.GetChildrenCount(element);
		if (count > 0 && (depth <= 0 || budget <= 0))
		{
			return true;
		}

		for (int i = 0; i < count && i < c_maxCellContentChildrenPerLevel; ++i)
		{
			var child = VisualTreeHelper.GetChild(element, i) as UIElement;
			if (child is null || child.Visibility != Visibility.Visible)
			{
				continue;
			}
			// Nothing under a disabled Control can take focus.
			if (child is Control control && !control.IsEnabled)
			{
				continue;
			}
			--budget;
			// A non-Control UIElement (Grid, Border) with IsTabStop is focusable too, as Enter's FindFirstFocusableElement sees it.
			if (IsFocusableCellContent(child) || (child is not Control && child.IsTabStop) ||
				HasAppContextFlyout(child) || child is TableView ||
				(isSuppressed is not null && isSuppressed(child)) || HasHyperlink(child) ||
				HasActionableContent(child, depth - 1, ref budget, isSuppressed))
			{
				return true;
			}
		}
		// Children past the per-level cap were not scanned; like an over-budget subtree, assume interactive.
		return count > c_maxCellContentChildrenPerLevel;
	}

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

		var childCount = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < childCount && i < c_maxCellContentChildrenPerLevel; ++i)
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

		var childCount = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < childCount && i < c_maxCellContentChildrenPerLevel; ++i)
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
}
