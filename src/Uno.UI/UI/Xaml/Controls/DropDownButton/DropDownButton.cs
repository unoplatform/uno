// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference DropDownButton.cpp, DropDownButton.h, tag winui3/release/2.6.5

using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Controls;

public partial class DropDownButton : Button
{
	private bool m_isFlyoutOpen;

	private readonly SerialDisposable m_flyoutPropertyChangedRevoker = new();
	private readonly SerialDisposable m_flyoutOpenedRevoker = new();
	private readonly SerialDisposable m_flyoutClosedRevoker = new();

	public DropDownButton()
	{
		this.SetDefaultStyleKey();
	}

	protected override void OnApplyTemplate()
	{
		var token = RegisterPropertyChangedCallback(FlyoutProperty, OnFlyoutPropertyChanged);
		m_flyoutPropertyChangedRevoker.Disposable = Disposable.Create(() => UnregisterPropertyChangedCallback(FlyoutProperty, token));

		RegisterFlyoutEvents();
	}

	private void RegisterFlyoutEvents()
	{
		m_flyoutOpenedRevoker.Disposable = null;
		m_flyoutClosedRevoker.Disposable = null;

		if (Flyout is { } flyout)
		{
			flyout.Opened += OnFlyoutOpened;
			m_flyoutOpenedRevoker.Disposable = Disposable.Create(() => flyout.Opened -= OnFlyoutOpened);
			flyout.Closed += OnFlyoutClosed;
			m_flyoutClosedRevoker.Disposable = Disposable.Create(() => flyout.Closed -= OnFlyoutClosed);
		}
	}

	internal bool IsFlyoutOpen() => m_isFlyoutOpen;

	internal void OpenFlyout()
	{
		if (Flyout is { } flyout)
		{
			flyout.ShowAt(this);
		}
	}

	internal void CloseFlyout()
	{
		if (Flyout is { } flyout)
		{
			flyout.Hide();
		}
	}

	private void OnFlyoutPropertyChanged(DependencyObject sender, DependencyProperty args) => RegisterFlyoutEvents();

	private void OnFlyoutOpened(object sender, object args)
	{
		m_isFlyoutOpen = true;
		SharedHelpers.RaiseAutomationPropertyChangedEvent(this, ExpandCollapseState.Collapsed, ExpandCollapseState.Expanded);
	}

	private void OnFlyoutClosed(object sender, object args)
	{
		m_isFlyoutOpen = false;
		SharedHelpers.RaiseAutomationPropertyChangedEvent(this, ExpandCollapseState.Expanded, ExpandCollapseState.Collapsed);
	}

	protected override AutomationPeer OnCreateAutomationPeer() => new DropDownButtonAutomationPeer(this);
}
