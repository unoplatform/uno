// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference MenuBar.cpp, MenuBar.h

using System.Collections.Generic;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls;

[ContentProperty(Name = nameof(Items))]
public partial class MenuBar : Control
{
	private bool m_isFlyoutOpen;

	// Visual components
	private ItemsControl m_contentRoot;
	private Grid m_layoutRoot;

	public IList<MenuBarItem> Items => (IList<MenuBarItem>)GetValue(ItemsProperty);

	public static DependencyProperty ItemsProperty { get; } =
		DependencyProperty.Register(
			nameof(Items),
			typeof(IList<MenuBarItem>),
			typeof(MenuBar),
			new FrameworkPropertyMetadata(null));

	public MenuBar()
	{
		this.SetDefaultStyleKey();

		var items = new ObservableVector<MenuBarItem>();
		SetValue(ItemsProperty, items);

		items.VectorChanged += (s, e) => UpdateAutomationSizeAndPosition();
	}

	// IUIElement / IUIElementOverridesHelper
	protected override AutomationPeer OnCreateAutomationPeer() => new MenuBarAutomationPeer(this);

	protected override void OnApplyTemplate() => SetUpTemplateParts();

	private void SetUpTemplateParts()
	{
		if (GetTemplateChild("LayoutRoot") is Grid layoutRoot)
		{
			m_layoutRoot = layoutRoot;
		}
		if (GetTemplateChild("ContentRoot") is ItemsControl contentRoot)
		{
			contentRoot.XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled;

			contentRoot.ItemsSource = Items;

			m_contentRoot = contentRoot;
		}
	}

	internal void RequestPassThroughElement(MenuBarItem menuBarItem)
	{
		// To enable switching flyout on hover, every menubar item needs the MenuBar root to include it for hit detection with flyouts open
		menuBarItem.AddPassThroughElement(m_layoutRoot);
	}

	internal bool IsFlyoutOpen
	{
		get => m_isFlyoutOpen;
		set => m_isFlyoutOpen = value;
	}

	// Automation Properties
	private void UpdateAutomationSizeAndPosition()
	{
		int sizeOfSet = 0;

		foreach (var item in Items)
		{
			if (item is UIElement itemAsUIElement)
			{
				if (itemAsUIElement.Visibility == Visibility.Visible)
				{
					sizeOfSet++;
					AutomationProperties.SetPositionInSet(itemAsUIElement, sizeOfSet);
				}
			}
		}

		foreach (var item in Items)
		{
			if (item is UIElement itemAsUIElement)
			{
				if (itemAsUIElement.Visibility == Visibility.Visible)
				{
					AutomationProperties.SetSizeOfSet(itemAsUIElement, sizeOfSet);
				}
			}
		}
	}
}
