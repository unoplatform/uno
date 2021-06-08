// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
#pragma once

#include "pch.h"
#include "common.h"

#include "BreadcrumbIterable.h"


class BreadcrumbIterator :
    public implements<BreadcrumbIterator, IIterator<object>>
{
public:
    BreadcrumbIterator( object& itemsSource);

    object Current();
    bool HasCurrent();
    uint GetMany(array_view<object> items);
    bool MoveNext();

private:

    uint m_currentIndex{};
    uint m_size{};
    ItemsSourceView m_breadcrumbItemsSourceView{null};
};
