// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewToolTipHelpers.h, tag winui3/main, commit dc28206ea35

#nullable enable

using System;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Tabular;

internal static partial class TableViewDetails
{
	// The published HelpText is recorded, not re-derived: it is not always the tooltip's content.
	internal sealed class CellToolTipRecord
	{
		public ToolTip? ToolTip;
		public string PublishedHelpText = "";
	}

	// InitializeDependencyProperty(name, IInspectable, TableView, isAttached: true, defaultValue: nullptr)
	internal static DependencyProperty? s_cellToolTipRecordProperty;

	// Holds the evaluated CellToolTipBinding value. The binding lives on the cell wrapper, so the
	// row's DataContext drives it and a recycled row re-resolves through ordinary inheritance.
	internal static DependencyProperty? s_cellToolTipValueProperty;

	internal static void EnsureCellToolTipRecordProperty()
	{
		if (s_cellToolTipRecordProperty is null)
		{
			s_cellToolTipRecordProperty = DependencyProperty.RegisterAttached(
				"TableViewCellToolTipRecord",
				typeof(object),
				typeof(TableView),
				new FrameworkPropertyMetadata(default(object)));
		}
	}

	// Called from the generated ClearTypeProperties so a XAML re-init re-registers against the new core.
	// TODO Uno: there is no ClearTypeProperties / XAML core re-init in Uno, so nothing calls this. Calling
	// it would also make the next Ensure* throw: Uno rejects re-registering a property name on the same owner.
	internal static void ClearCellToolTipProperties()
	{
		s_cellToolTipRecordProperty = null;
		s_cellToolTipValueProperty = null;
	}

	internal static CellToolTipRecord? GetRecord(FrameworkElement? element)
	{
		if (element is null || s_cellToolTipRecordProperty is null)
		{
			return null;
		}

		return element.GetValue(s_cellToolTipRecordProperty) as CellToolTipRecord;
	}

	internal static void ForgetRecord(FrameworkElement element)
	{
		if (s_cellToolTipRecordProperty is not null)
		{
			element.ClearValue(s_cellToolTipRecordProperty);
		}
	}

	internal static string? TryGetString(object? value)
	{
		// TODO Uno: IPropertyValue projection
		if (value is not null &&
			ValueConversionHelpers.GetPropertyType(value.GetType()) == PropertyType.String)
		{
			return (string)value;
		}

		return null;
	}

	internal static void RetractPublishedHelpText(FrameworkElement element, CellToolTipRecord record)
	{
		if (!string.IsNullOrEmpty(record.PublishedHelpText) &&
			AutomationProperties.GetHelpText(element) == record.PublishedHelpText)
		{
			AutomationProperties.SetHelpText(element, "");
		}

		record.PublishedHelpText = "";
	}

	// Neutralized in place rather than detached, so a recycled cell reuses the ToolTip. Matches TabViewItem.
	internal static void ClearOwnedToolTip(FrameworkElement element)
	{
		var record = GetRecord(element);
		if (record is null)
		{
			return;
		}

		RetractPublishedHelpText(element, record);

		if (record.ToolTip is not null && record.ToolTip == (ToolTipService.GetToolTip(element) as ToolTip))
		{
			// Close before dropping content: clearing an open tooltip's content removes a live
			// popup's child, the shape behind the reentrant CPopup::RemoveChild crash.
			record.ToolTip.IsEnabled = false;
			if (record.ToolTip.IsOpen)
			{
				record.ToolTip.IsOpen = false;
			}
			record.ToolTip.Content = null;
		}
		else
		{
			ForgetRecord(element);
		}
	}

	// Returns whether the element is left carrying a control-owned tooltip. Never touches one the
	// app set itself. publishHelpText is false when a peer publishes the text instead.
	internal static bool SetOwnedToolTip(
		FrameworkElement? element,
		object? content,
		PlacementMode placement,
		bool publishHelpText = true,
		string propertyName = "CellToolTipBinding")
	{
		if (element is null)
		{
			return false;
		}

		var record = GetRecord(element);
		// The raw value, not a ToolTip-narrowed one: a bare string would otherwise read as empty.
		var existingValue = ToolTipService.GetToolTip(element);
		var owned = (record is not null && record.ToolTip is not null && record.ToolTip == (existingValue as ToolTip))
			? record.ToolTip : null;

		if (existingValue is not null && owned is null)
		{
			if (record is not null)
			{
				RetractPublishedHelpText(element, record);
				ForgetRecord(element);
			}
			return false;
		}

		// A ToolTip as content would render nested inside ours, and the control owns placement.
		var text = TryGetString(content);
		if (content is not null && content is ToolTip)
		{
			TVDiag.LogRetailF("[TableView] A %ls value must be tooltip content, " +
				"not a ToolTip; the element has no tooltip.", propertyName);
			ClearOwnedToolTip(element);
			return false;
		}

		if (content is null || (text is not null && text.Length == 0))
		{
			ClearOwnedToolTip(element);
			return false;
		}

		EnsureCellToolTipRecordProperty();
		if (record is null)
		{
			record = new CellToolTipRecord();
			element.SetValue(s_cellToolTipRecordProperty!, record);
		}

		RetractPublishedHelpText(element, record);

		if (owned is not null)
		{
			// Neutralize first: a throwing assignment must not leave the previous item's content live.
			owned.IsEnabled = false;
			if (owned.IsOpen)
			{
				owned.IsOpen = false;
			}
			owned.Content = null;
			owned.Content = content;
			owned.Placement = placement;
			owned.IsEnabled = true;
		}
		else
		{
			ToolTip toolTip = new();
			toolTip.Content = content;
			toolTip.Placement = placement;

			// Record before attaching: a throw then reads as "not ours" rather than orphaning a
			// tooltip nothing can clear.
			record.ToolTip = toolTip;
			ToolTipService.SetToolTip(element, toolTip);
		}

		// Published unconditionally and recorded; TableViewCellAutomationPeer decides at query time
		// whether it merely repeats the cell's own text. Comparing here would race the cell's binding.
		var helpText = text ?? "";
		if (publishHelpText &&
			helpText.Length != 0 &&
			string.IsNullOrEmpty(AutomationProperties.GetHelpText(element)))
		{
			AutomationProperties.SetHelpText(element, helpText);
			record.PublishedHelpText = helpText;
		}

		// Both paths report string content only.
		if (text is null)
		{
			TVDiag.DbgLogF("[TableView] %ls content is not a string; it shows on hover but is " +
				"not reported to assistive technology.", propertyName);
		}

		return true;
	}

	// The bound value did not change, so only an explicit re-apply restores a tooltip an edit retracted.
	internal static void RefreshOwnedToolTip(FrameworkElement? element)
	{
		if (element is null || s_cellToolTipValueProperty is null)
		{
			return;
		}

		if (element.GetValue(s_cellToolTipValueProperty) is { } value)
		{
			SetOwnedToolTip(element, value, PlacementMode.Mouse);
		}
		else
		{
			ClearOwnedToolTip(element);
		}
	}

	// The whole per-cell update path: no event, no invalidation, no realization-time work.
	internal static void OnCellToolTipValueChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var element = sender as FrameworkElement;
		if (element is null)
		{
			return;
		}

		// Contained: this runs from the property system, where an escaping exception is a fail-fast.
		try
		{
			if (args.NewValue is { } value)
			{
				SetOwnedToolTip(element, value, PlacementMode.Mouse);
			}
			else
			{
				ClearOwnedToolTip(element);
			}
		}
		catch (Exception ex)
		{
			TVDiag.LogRetailF("[TableView] Applying a cell tooltip value failed (HRESULT 0x%08X).",
				unchecked((uint)ex.HResult));
		}
	}

	internal static DependencyProperty EnsureCellToolTipValueProperty()
	{
		if (s_cellToolTipValueProperty is null)
		{
			s_cellToolTipValueProperty = DependencyProperty.RegisterAttached(
				"TableViewCellToolTipValue",
				typeof(object),
				typeof(TableView),
				new FrameworkPropertyMetadata(default(object), OnCellToolTipValueChanged));
		}

		return s_cellToolTipValueProperty;
	}

	// Set once when the cell is created; the binding then tracks the row's DataContext.
	internal static void ApplyCellToolTipBinding(
		FrameworkElement? element,
		Binding? binding)
	{
		if (element is not null && binding is not null)
		{
			BindingOperations.SetBinding(element, EnsureCellToolTipValueProperty(), binding);
		}
	}

	// Headers rebuild wholesale, so there is no binding or refresh path. Contained: both call sites
	// are fail-fast, and app content can throw (a UIElement already parented by another header).
	internal static void ApplyHeaderToolTip(
		FrameworkElement? element,
		object? content)
	{
		try
		{
			SetOwnedToolTip(
				element,
				content,
				PlacementMode.Mouse,
				false /* publishHelpText */,
				"HeaderToolTip");
		}
		catch (Exception ex)
		{
			TVDiag.LogRetailF("[TableView] Applying a header tooltip failed (HRESULT 0x%08X).",
				unchecked((uint)ex.HResult));
		}
	}
}
