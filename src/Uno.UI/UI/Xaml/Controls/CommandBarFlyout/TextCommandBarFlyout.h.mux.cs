// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\CommandBarFlyout\TextCommandBarFlyout.h, tag winui3/release/1.6.3, commit 66d24dfff3b2763ab3be096a2c7cbaafc81b31eb

#nullable enable

using System;
using System.Collections.Generic;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

[Flags]
internal enum TextControlButtons
{
	None = 0x0000,
	Cut = 0x0001,
	Copy = 0x0002,
	Paste = 0x0004,
	Bold = 0x0008,
	Italic = 0x0010,
	Underline = 0x0020,
	Undo = 0x0040,
	Redo = 0x0080,
	SelectAll = 0x0100,
}

partial class TextCommandBarFlyout
{
	private Dictionary<TextControlButtons, ICommandBarElement> m_buttons = new();
	private AppBarButton? m_proofingButton;

	// TODO:MZ: Ensure disposal of all lists of revokers
	private List<IDisposable> m_buttonCommandRevokers = new();
	private List<IDisposable> m_buttonClickRevokers = new();
	private List<IDisposable> m_toggleButtonCheckedRevokers = new();
	private List<IDisposable> m_toggleButtonUncheckedRevokers = new();

	private SerialDisposable m_proofingButtonLoadedRevoker = new();

	private List<IDisposable> m_proofingMenuItemClickRevokers = new();
	private List<IDisposable> m_proofingMenuToggleItemClickRevokers = new();
	private DispatcherHelper m_dispatcherHelper;

	private bool m_isSettingToggleButtonState;
}
