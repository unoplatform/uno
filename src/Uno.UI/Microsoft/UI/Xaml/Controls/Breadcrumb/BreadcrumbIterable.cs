// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include "pch.h"
#include "common.h"
#include "BreadcrumbIterable.h"
#include "BreadcrumbIterator.h"

BreadcrumbIterable()
{
}

BreadcrumbIterable( object& itemsSource)
{
    ItemsSource(itemsSource);
}

void ItemsSource( object& itemsSource)
{
    m_itemsSource = itemsSource;
}

IIterator<object> First()
{
    return new BreadcrumbIterator(m_itemsSource);
}
