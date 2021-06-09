// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	internal partial class BreadcrumbElementFactory : ElementFactory
	{
		private IElementFactoryShim? m_itemTemplateWrapper = null;

		public BreadcrumbElementFactory()
		{
		}

		internal void UserElementFactory(object newValue)
		{
			m_itemTemplateWrapper = newValue as IElementFactoryShim;
			if (m_itemTemplateWrapper == null)
			{
				// ItemTemplate set does not implement IElementFactoryShim. We also want to support DataTemplate.
				if (newValue is DataTemplate dataTemplate)
				{
					m_itemTemplateWrapper = new ItemTemplateWrapper(dataTemplate);
				}
			}
		}

		UIElement GetElementCore(ElementFactoryGetArgs args)
		{
			var newContent = [itemTemplateWrapper = m_itemTemplateWrapper, args]()



	{
				if (args.Data() as BreadcrumbBarItem())
        {
					return args.Data();
				}

				if (itemTemplateWrapper)
				{
					return itemTemplateWrapper.GetElement(args).as< object > ();
				}
				return args.Data();
			} ();

			// Element is already a BreadcrumbBarItem, so we just return it.
			if (var breadcrumbItem = newContent as BreadcrumbBarItem())
    {
				// When the list has not changed the returned item is still a BreadcrumbBarItem but the
				// item is not reset, so we set the content here
				breadcrumbItem.Content(args.Data());
				return breadcrumbItem;
			}

			var newBreadcrumbBarItem = BreadcrumbBarItem{ };
			newBreadcrumbBarItem.Content(args.Data());

			// If a user provided item template exists, we pass the template down
			// to the ContentPresenter of the BreadcrumbBarItem.
			if (var itemTemplateWrapper = m_itemTemplateWrapper as ItemTemplateWrapper())
    {
				newBreadcrumbBarItem.ContentTemplate(itemTemplateWrapper.Template());
			}

			return newBreadcrumbBarItem;
		}

		void RecycleElementCore(ElementFactoryRecycleArgs& args)
		{
			if (var element = args.Element())
    {
				bool isEllipsisDropDownItem = false; // Use of isEllipsisDropDownItem is workaround for
													 // crashing bug when attempting to show ellipsis dropdown after clicking one of its items.

				if (var breadcrumbItem = element as BreadcrumbBarItem())
        {
					var breadcrumbItemImpl = get_self<BreadcrumbBarItem>(breadcrumbItem);
					breadcrumbItemImpl.ResetVisualProperties();

					isEllipsisDropDownItem = breadcrumbItemImpl.IsEllipsisDropDownItem();
				}

				if (m_itemTemplateWrapper && isEllipsisDropDownItem)
				{
					m_itemTemplateWrapper.RecycleElement(args);
				}
			}
		}
	}
}
