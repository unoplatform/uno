// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewGroupInfo.h, tag winui3/main, commit 79209e041

#nullable enable

using Microsoft.UI.Xaml.Data;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Projection surfaced as the Content of a group header, and hence the binding source of the
// built-in content template and any app-supplied TableView.GroupHeaderTemplate. TableViewRowInfo
// remains the internal source of truth for kind / level / expansion; this exposes the group key
// plus the metadata a header template needs.
//
// Updated in place while the header represents the same group, so template bindings observe
// metadata changes. Recycling to another group creates a new projection; references retained
// by an open context menu must never change group identity.
//
// Deliberately a plain implementation<> rather than a ReferenceTracker, and not using the
// generated properties base: this is a leaf value object with no outbound references to XAML
// objects, and the generated base's event_source requires an ITrackerHandleManager owner.
// Compares group keys by value for boxed scalars, falling back to COM identity. Exposed so
// TableView can tell whether a recycled header still represents the same group before it
// announces an expansion change -- announcing across a recycle would report the wrong group.
// bool SameGroupKey(winrt::IInspectable const& left, winrt::IInspectable const& right) noexcept;
// (ported as a static member of TableViewGroupInfo in TableViewGroupInfo.mux.cs)
// TODO Uno: Windows.Foundation.IStringable is covered by the ToString override.
partial class TableViewGroupInfo : ICustomPropertyProvider
{
	// TableViewGroupInfo(
	//     winrt::IInspectable const& key,
	//     int32_t itemCount,
	//     int32_t level,
	//     bool isExpandable,
	//     bool isExpanded,
	//     winrt::hstring const& keyText);

	// winrt::IInspectable Key();
	// int32_t ItemCount();
	// int32_t Level();
	// bool IsExpandable();
	// bool IsExpanded();
	// winrt::hstring KeyText();
	// winrt::hstring ItemCountText();

	// winrt::event_token PropertyChanged(winrt::PropertyChangedEventHandler const& value);
	// void PropertyChanged(winrt::event_token const& token);

	// {Binding} resolves the properties here when the app's metadata can't describe this type (trimmed or AOT apps).
	// Not [bindable]: that adds it to every C++ app's generated XamlTypeInfo, which then needs the Tabular projection header.
	// winrt::TypeName Type();
	// winrt::Microsoft::UI::Xaml::Data::ICustomProperty GetCustomProperty(winrt::hstring const& name);
	// winrt::Microsoft::UI::Xaml::Data::ICustomProperty GetIndexedProperty(winrt::hstring const& name, winrt::TypeName const& type);
	// winrt::hstring GetStringRepresentation();
	// winrt::hstring ToString();

	// Internal: refresh identity/metadata in place, raising PropertyChanged only for what
	// actually changed. Expansion is NOT set here -- see SetExpansionInternal.
	// void UpdateInternal(
	//     winrt::IInspectable const& key,
	//     int32_t itemCount,
	//     int32_t level,
	//     winrt::hstring const& keyText);

	// Expansion is owned by the header's IsExpandable/IsExpanded DPs (they drive the
	// VisualStates) and mirrored here. The header is the only caller, so there is one
	// write path rather than two that can drift.
	// void SetExpansionInternal(bool isExpandable, bool isExpanded);

	// private:
	// void RaisePropertyChanged(wchar_t const* propertyName);

	private object? m_key = null;
	private int m_itemCount = 0;
	private int m_level = 0;
	private bool m_isExpandable = false;
	private bool m_isExpanded = false;
	private string m_keyText = "";

	// Formatted lazily so a template binding only Key / ItemCount never pays for it.
	private string m_itemCountText = "";
	private bool m_hasItemCountText = false;

	// winrt::event<winrt::PropertyChangedEventHandler> m_propertyChanged;
	// The PropertyChanged event is declared in TableViewGroupInfo.Properties.cs; RaisePropertyChanged keeps the
	// winrt::event semantics (handler exceptions swallowed) through CppWinRTHelpers.InvokeWinRTEvent.
}
