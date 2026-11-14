// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BreadcrumbBarElementFactory.cpp, tag winui3/release/1.5.3, commit 2a60e27

#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

internal partial class BreadcrumbElementFactory : ElementFactory
{
	private IElementFactory? m_itemTemplateWrapper = null;

	public BreadcrumbElementFactory()
	{
	}

	internal void UserElementFactory(object? newValue)
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
			m_itemTemplateWrapper = customElementFactory;
		}
#if HAS_UNO
		// TODO Uno: remove with IElementFactoryShim.
		else if (newValue is IElementFactoryShim shim)
		{
			m_itemTemplateWrapper = new ElementFactoryShimAdapter(shim);
		}
#endif
	}

	private protected override UIElement GetElementCore(global::Microsoft.UI.Xaml.ElementFactoryGetArgs args)
	{
		object? GetNewContent(IElementFactory? itemTemplateWrapper)
		{
			if (args.Data is BreadcrumbBarItem)
			{
				return args.Data;
			}

			if (itemTemplateWrapper != null)
			{
				return itemTemplateWrapper.GetElement(args);
			}
			return args.Data;
		}

		var newContent = GetNewContent(m_itemTemplateWrapper);

		// Element is already a BreadcrumbBarItem, so we just return it.
		if (newContent is BreadcrumbBarItem breadcrumbItem)
		{
			// When the list has not changed the returned item is still a BreadcrumbBarItem but the
			// item is not reset, so we set the content here
			breadcrumbItem.Content = args.Data;
			return breadcrumbItem;
		}

		var newBreadcrumbBarItem = new BreadcrumbBarItem();
		newBreadcrumbBarItem.Content = args.Data;

		// If a user provided item template exists, we pass the template down
		// to the ContentPresenter of the BreadcrumbBarItem.
		if (m_itemTemplateWrapper is ItemTemplateWrapper itemTemplateWrapper)
		{
			newBreadcrumbBarItem.ContentTemplate = itemTemplateWrapper.Template;
		}

		return newBreadcrumbBarItem;
	}

	private protected override void RecycleElementCore(global::Microsoft.UI.Xaml.ElementFactoryRecycleArgs args)
	{
		if (args.Element is { } element)
		{
			bool isEllipsisDropDownItem = false; // Use of isEllipsisDropDownItem is workaround for
												 // crashing bug when attempting to show ellipsis dropdown after clicking one of its items.

			if (element is BreadcrumbBarItem breadcrumbItem)
			{
				var breadcrumbItemImpl = breadcrumbItem;
				breadcrumbItemImpl.ResetVisualProperties();

				isEllipsisDropDownItem = breadcrumbItemImpl.IsEllipsisDropDownItem();
			}

			if (m_itemTemplateWrapper != null && isEllipsisDropDownItem)
			{
				m_itemTemplateWrapper.RecycleElement(args);
			}
		}
	}
}
