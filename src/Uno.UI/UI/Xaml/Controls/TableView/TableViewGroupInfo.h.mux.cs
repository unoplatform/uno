// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewGroupInfo.h, tag winui3/main, commit dc28206ea35

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Projection surfaced as the Content of a group header, and hence the binding source of the
// built-in content template and any app-supplied TableView.GroupHeaderTemplate. TableViewRowInfo
// remains the internal source of truth for kind / level / expansion; this exposes the group key
// plus the metadata a header template needs.
//
// Updated IN PLACE by PrepareGroupHeaderElement rather than re-created per render: a template
// binding IsExpanded or ItemCount must observe changes, and re-pointing Content at a fresh
// instance every render would re-evaluate every binding in the content template.
//
// Deliberately a plain implementation<> rather than a ReferenceTracker, and not using the
// generated properties base: this is a leaf value object with no outbound references to XAML
// objects, and the generated base's event_source requires an ITrackerHandleManager owner.
// Compares group keys by value for boxed scalars, falling back to COM identity. Exposed so
// TableView can tell whether a recycled header still represents the same group before it
// announces an expansion change -- announcing across a recycle would report the wrong group.
// bool SameGroupKey(winrt::IInspectable const& left, winrt::IInspectable const& right) noexcept;
// (ported as a static member of TableViewGroupInfo in TableViewGroupInfo.mux.cs)
partial class TableViewGroupInfo
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
