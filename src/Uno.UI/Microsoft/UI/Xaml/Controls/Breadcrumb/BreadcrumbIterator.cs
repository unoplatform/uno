// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include "pch.h"
#include "common.h"
#include "BreadcrumbIterator.h"

BreadcrumbIterator( object& itemsSource)
{
    m_currentIndex = 0;

    if (itemsSource)
    {
        m_breadcrumbItemsSourceView = ItemsSourceView(itemsSource);

        // Add 1 to account for the leading null/ellipsis element
        m_size = (uint32_t)(m_breadcrumbItemsSourceView.Count() + 1);
    }
    else
    {
        m_size = 1;
    }
}

object Current()
{
    if (m_currentIndex == 0)
    {
        return null;
    }
    else if (HasCurrent())
    {
        return m_breadcrumbItemsSourceView.GetAt(m_currentIndex - 1);
    }
    else
    {
        throw hresult_out_of_bounds();
    }
}

bool HasCurrent()
{
    return (m_currentIndex < m_size);
}

uint GetMany(array_view<object> items)
{
    uint howMany{};
    if (HasCurrent())
    {
        do
        {
            if (howMany >= items.size()) break;

            items[howMany] = Current();
            howMany++;
        } while (MoveNext());
    }

    return howMany;
}

bool MoveNext()
{
    if (HasCurrent())
    {
        ++m_currentIndex;
        return HasCurrent();
    }
    else
    {
        throw hresult_out_of_bounds();
    }

    return false;
}
