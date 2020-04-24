// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include "pch.h"
#include "ItemTemplateWrapper.h"
#include "RecyclePool.h"
#include "ItemsRepeater.common.h"

ItemTemplateWrapper(DataTemplate const& dataTemplate)
{
    m_dataTemplate = dataTemplate;
}

ItemTemplateWrapper(DataTemplateSelector const& dataTemplateSelector)
{
    m_dataTemplateSelector = dataTemplateSelector;
}

DataTemplate Template()
{
    return m_dataTemplate;
}

void Template(DataTemplate const& value)
{
    m_dataTemplate = value;
}

DataTemplateSelector TemplateSelector()
{
    return m_dataTemplateSelector;
}

void TemplateSelector(DataTemplateSelector const& value)
{
    m_dataTemplateSelector = value;
}

#region IElementFactory

UIElement GetElement(ElementFactoryGetArgs const& args)
{
    var selectedTemplate = m_dataTemplate ? m_dataTemplate : m_dataTemplateSelector.SelectTemplate(args.Data());
    // Check if selected template we got is valid
    if (selectedTemplate == null)
    {
        // Null template, use other SelectTemplate method
        try
        {
            selectedTemplate = m_dataTemplateSelector.SelectTemplate(args.Data(), null);
        }
        catch (hresult_error e)
        {
            // The default implementation of SelectTemplate(IInspectable item, ILayout container) throws invalid arg for null container
            // To not force everbody to provide an implementation of that, catch that here
            if (e.code().value != E_INVALIDARG)
            {
                throw e;
            }
        }

        if (selectedTemplate == null)
        {
            // Still null, fail with a reasonable message now.
            throw hresult_invalid_argument("Null encountered as data template. That is not a valid value for a data template, and can not be used.");
        }
    }
    var recyclePool = RecyclePool.GetPoolInstance(selectedTemplate);
    UIElement element = null;

    if (recyclePool)
    {
        // try to get an element from the recycle pool.
(        element = recyclePool.TryGetElement("" /* key */, args.Parent() as FrameworkElement));
    }

    if (!element)
    {
        // no element was found in recycle pool, create a new element
(        element = selectedTemplate.LoadContent() as FrameworkElement);

        // Template returned null, so insert empty element to render nothing
        if (!element) {
            var rectangle = Rectangle();
            rectangle.Width(0);
            rectangle.Height(0);
            element = rectangle;
        }

        // Associate template with element
        element.SetValue(RecyclePool.GetOriginTemplateProperty(), selectedTemplate);
    }

    return element;
}

void RecycleElement(ElementFactoryRecycleArgs const& args)
{
    var element = args.Element();
    DataTemplate selectedTemplate = m_dataTemplate? 
        m_dataTemplate:
(        element.GetValue(RecyclePool.GetOriginTemplateProperty()) as DataTemplate);
    var recyclePool = RecyclePool.GetPoolInstance(selectedTemplate);
    if (!recyclePool)
    {
        // No Recycle pool in the template, create one.
        recyclePool = new RecyclePool();
        RecyclePool.SetPoolInstance(selectedTemplate, recyclePool);
    }

    recyclePool.PutElement(args.Element(), "" /* key */, args.Parent());
}

#endregion
