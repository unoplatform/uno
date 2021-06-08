// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
#pragma once

#include "pch.h"
#include "common.h"

#include "Vector.h"

class BreadcrumbIterable :
    public ReferenceTracker<BreadcrumbIterable, reference_tracker_implements_t<IIterable<object>>.type>
{
public:
    BreadcrumbIterable();
    BreadcrumbIterable( object& itemsSource);
    void ItemsSource( object& itemsSource);
    IIterator<object> First();

private:
    tracker_ref<object> m_itemsSource{ this };
};
