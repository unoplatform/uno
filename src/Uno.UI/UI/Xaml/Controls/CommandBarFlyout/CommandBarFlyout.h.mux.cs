﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\CommandBarFlyout\CommandBarFlyout.h, tag winui3/release/1.7.3, commit 65718e2813a90fc900e8775d2ddc580b268fcc2f

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Disposables;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

partial class CommandBarFlyout
{
	/// <summary>
	/// Gets or sets a value that indicates whether or not the CommandBarFlyout
	/// should always stay in its Expanded state and block the user from entering
	/// the Collapsed state. Defaults to false.
	/// </summary>
	public bool AlwaysExpanded
	{
		get => m_alwaysExpanded;
		set => m_alwaysExpanded = value;
	}

	/// <summary>
	/// Gets the collection of primary command elements for the CommandBarFlyout.
	/// </summary>
	public IObservableVector<ICommandBarElement> PrimaryCommands => m_primaryCommands;

	/// <summary>
	/// Gets the collection of secondary command elements for the CommandBarFlyout.
	/// </summary>
	public IObservableVector<ICommandBarElement> SecondaryCommands => m_secondaryCommands;

	internal CommandBarFlyoutCommandBar? m_commandBar;

	private const int s_commandBarElementDependencyPropertiesCount = 3;

	private bool m_alwaysExpanded;

	private IObservableVector<ICommandBarElement> m_primaryCommands;
	private IObservableVector<ICommandBarElement> m_secondaryCommands;

	// winrt::IObservableVector<winrt::ICommandBarElement>::VectorChanged_revoker throws a weird build error when used,
	// so we're using the event token pattern for manual detachment instead.
	private readonly SerialDisposable m_primaryCommandsVectorChangedToken = new();
	private readonly SerialDisposable m_secondaryCommandsVectorChangedToken = new();

	private readonly SerialDisposable m_commandBarOpenedRevoker = new();
	private readonly SerialDisposable m_commandBarOpeningRevoker = new();
#if false // Not used in WinUI
    private readonly SerialDisposable m_commandBarClosedRevoker = new();
#endif
	private readonly SerialDisposable m_commandBarClosingRevoker = new();

	private readonly Dictionary<int, IDisposable> m_secondaryButtonClickRevokerByIndexMap = new();
	private readonly Dictionary<int, IDisposable> m_secondaryToggleButtonCheckedRevokerByIndexMap = new();
	private readonly Dictionary<int, IDisposable> m_secondaryToggleButtonUncheckedRevokerByIndexMap = new();
	private readonly Dictionary<int, IDisposable[]> m_propertyChangedRevokersByIndexMap = new();

	private FlyoutPresenter? m_presenter;

	private bool m_isClosingAfterCloseAnimation;
}
