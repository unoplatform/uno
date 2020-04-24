// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include <pch.h>
#include <common.h>
#include "ItemsRepeater.common.h"
#include "RecyclePoolFactory.h"
#include "RecyclePool.h"

GlobalDependencyProperty RecyclePool.s_reuseKeyProperty = null;
GlobalDependencyProperty RecyclePool.s_originTemplateProperty = null;


#region IRecyclePoolStatics 

string RecyclePool.GetReuseKey(UIElement const& element)
{
    return auto_unbox(element.GetValue(s_reuseKeyProperty));
}

void RecyclePool.SetReuseKey(UIElement const& element, string const& value)
{
    element.SetValue(s_reuseKeyProperty, box_value(value));
}

RecyclePool RecyclePool.GetPoolInstance(DataTemplate const& dataTemplate)
{
    if (!s_PoolInstanceProperty)
    {
        EnsureProperties();
    }

    return RecyclePoolProperties.GetPoolInstance(dataTemplate);
}

void RecyclePool.SetPoolInstance(DataTemplate const& dataTemplate, RecyclePool const& value)
{
    if (!s_PoolInstanceProperty)
    {
        EnsureProperties();
    }

    RecyclePoolProperties.SetPoolInstance(dataTemplate, value);
}


#endregion

/* static */
void RecyclePool.EnsureProperties()
{
    RecyclePoolProperties.EnsureProperties();
    if (s_reuseKeyProperty == null)
    {
        s_reuseKeyProperty =
            InitializeDependencyProperty(
                "ReuseKey",
                name_of<hstring>(),
                name_of<RecyclePool>(),
                true /* isAttached */,
                box_value(wstring_view("")) /* defaultValue */,
                null /* propertyChangedCallback */);
    }

    if (s_originTemplateProperty == null)
    {
        s_originTemplateProperty =
            InitializeDependencyProperty(
                "OriginTemplate",
                name_of<DataTemplate>(),
                name_of<RecyclePool>(),
                true /* isAttached */,
                null /* defaultValue */,
                null /* propertyChangedCallback */);
    }
}

/* static */
void RecyclePool.ClearProperties()
{
    s_reuseKeyProperty = null;
    s_originTemplateProperty = null;
    RecyclePoolProperties.ClearProperties();
}
