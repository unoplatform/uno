// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference DropDownButton.cpp, tag winui3/release/1.4.2

using Uno.UI.Helpers.WinUI;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;

namespace Windows.UI.Xaml.Controls;

public partial class DropDownButton : Button
{
	private bool m_isFlyoutOpen;

	public DropDownButton()
	{
		DefaultStyleKey = typeof(DropDownButton);
	}

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		RegisterFlyoutEvents();
	}

	private void RegisterFlyoutEvents()
	{
		if (Flyout != null)
		{
			Flyout.Opened += OnFlyoutOpened;
			Flyout.Closed += OnFlyoutClosed;
		}
	}

	internal bool IsFlyoutOpen()
	{
		return m_isFlyoutOpen;
	}

	internal void OpenFlyout()
	{
		if (Flyout is Flyout)
		{
			Flyout.ShowAt(this);
		}
	}

	internal void CloseFlyout()
	{
		if (Flyout is Flyout)
		{
			Flyout.Hide();
		}
	}

	private void OnFlyoutPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
		if (args.OldValue is Flyout flyout)
		{
			flyout.Opened -= OnFlyoutOpened;
			flyout.Closed -= OnFlyoutClosed;
		}
		RegisterFlyoutEvents();
	}

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

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new DropDownButtonAutomationPeer(this);
	}
}
