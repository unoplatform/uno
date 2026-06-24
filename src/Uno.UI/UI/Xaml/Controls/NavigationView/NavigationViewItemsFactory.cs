// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference NavigationViewItemsFactory.cpp, commit bac7a9c33

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

internal class NavigationViewItemsFactory : ElementFactory
{
	private IElementFactoryShim m_itemTemplateWrapper = null;
	private NavigationViewItemBase m_settingsItem = null;
	private List<NavigationViewItem> navigationViewItemPool = new List<NavigationViewItem>();

	internal void UserElementFactory(object newValue)
	{
		if (newValue is DataTemplate dataTemplate)
		{
			m_itemTemplateWrapper = new ItemTemplateWrapper(dataTemplate);
		}
		else if (newValue is DataTemplateSelector selector)
		{
			m_itemTemplateWrapper = new ItemTemplateWrapper(selector);
		}
		else if (newValue is IElementFactory customElementFactory)
		{
			// Mirrors the upstream QI: a custom IElementFactory that does not also implement
			// IElementFactoryShim degrades to plain NavigationViewItem wrapping instead of throwing.
			m_itemTemplateWrapper = customElementFactory as IElementFactoryShim;
		}

		navigationViewItemPool = new List<NavigationViewItem>();
	}

	internal void SettingsItem(NavigationViewItemBase settingsItem)
	{
		m_settingsItem = settingsItem;
	}

	// Retrieve the element that will be displayed for a specific data item.
	// If the resolved element is not derived from NavigationViewItemBase, wrap in a NavigationViewItem before returning.
	protected override UIElement GetElementCore(ElementFactoryGetArgs args)
	{
		object GetNewContent(IElementFactoryShim itemTemplateWrapper, NavigationViewItemBase settingsItem)
		{
			// Do not template SettingsItem
			if (settingsItem != null && settingsItem == args.Data)
			{
				return args.Data;
			}

			if (itemTemplateWrapper != null)
			{
				return itemTemplateWrapper.GetElement(args);
			}
			return args.Data;
		}

		var newContent = GetNewContent(m_itemTemplateWrapper, m_settingsItem);

		// Element is already of expected type, just return it
		if (newContent is NavigationViewItemBase newItem)
		{
			return newItem;
		}

		// Get or create a wrapping container for the data
		NavigationViewItem GetNavigationViewItem()
		{
			if (navigationViewItemPool.Count > 0)
			{
				var nvi = navigationViewItemPool[navigationViewItemPool.Count - 1];
				navigationViewItemPool.RemoveAt(navigationViewItemPool.Count - 1);
				return nvi;
			}
			return new NavigationViewItem();
		}
		var nvi = GetNavigationViewItem();
		var nviImpl = nvi;
		nviImpl.CreatedByNavigationViewItemsFactory = true;

		// If a user provided item template exists, just pass the template and data down to the ContentPresenter of the NavigationViewItem
		if (m_itemTemplateWrapper != null)
		{
			if (m_itemTemplateWrapper is ItemTemplateWrapper itemTemplateWrapper)
			{
				// Recycle newContent
				var tempArgs = new ElementFactoryRecycleArgs();
				tempArgs.Element = newContent as UIElement;
				m_itemTemplateWrapper.RecycleElement(tempArgs);

				nviImpl.Content = args.Data;
				nviImpl.ContentTemplate = itemTemplateWrapper.Template;
				nviImpl.ContentTemplateSelector = itemTemplateWrapper.TemplateSelector;
				return nviImpl;
			}
		}

		nviImpl.Content = newContent;
		return nviImpl;
	}

	protected override void RecycleElementCore(ElementFactoryRecycleArgs args)
	{
		var element = args.Element;
		if (element != null)
		{
			if (element is NavigationViewItem nvi)
			{
				var nviImpl = nvi;
				// Check whether we wrapped the element in a NavigationViewItem ourselves.
				// If yes, we are responsible for recycling it.
				if (nviImpl.CreatedByNavigationViewItemsFactory)
				{
					nviImpl.CreatedByNavigationViewItemsFactory = false;
					UnlinkElementFromParent(args);
					args.Element = null;

					// Retain the NVI that we created for future re-use
					navigationViewItemPool.Add(nvi);

					// Retrieve the proper element that requires recycling for a user defined item template
					// and update the args correspondingly
					if (m_itemTemplateWrapper != null)
					{
						// TODO: Retrieve the element and add to the args
					}
				}
			}

			// Do not recycle SettingsItem
			bool isSettingsItem = m_settingsItem != null && m_settingsItem == args.Element;
			UnlinkElementFromParent(args);
			if (m_itemTemplateWrapper != null && !isSettingsItem)
			{
				m_itemTemplateWrapper.RecycleElement(args);
			}
		}
	}

	private void UnlinkElementFromParent(ElementFactoryRecycleArgs args)
	{
		// We want to unlink the containers from the parent repeater
		// in case we are required to move it to a different repeater.
		// WinUI uses try_as<Panel>() here — an interface query that succeeds on an ItemsRepeater because its
		// implementation derives from DeriveFromPanelHelper_base (public base is still FrameworkElement). Uno
		// models that split as `FrameworkElement, IPanel`, so the faithful equivalent is `is IPanel`, not the
		// class check `is Panel` (Panel implements IPanel too, so real panels are still covered).
		if (args.Parent is IPanel panel)
		{
			var children = panel.Children;
			var childIndex = children.IndexOf(args.Element);
			if (childIndex >= 0)
			{
				children.RemoveAt(childIndex);
				args.Parent = null;
			}
		}
	}
}
