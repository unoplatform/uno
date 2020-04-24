// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include <pch.h>
#include <common.h>
#include "ItemsRepeater.common.h"
#include "VirtualizingLayoutContext.h"
#include "LayoutContextAdapter.h"

LayoutContextAdapter(NonVirtualizingLayoutContext const& nonVirtualizingContext)
{
    m_nonVirtualizingContext = make_weak(nonVirtualizingContext);
}

#region ILayoutContextOverrides

IInspectable LayoutStateCore()
{
    if (var context = m_nonVirtualizingContext.get())
    {
        return context.LayoutState();
    }
    return null;
}

void LayoutStateCore(IInspectable const& state)
{
    if (var context = m_nonVirtualizingContext.get())
    {
        context.LayoutStateCore(state);
    }
}

#endregion

#region IVirtualizingLayoutContextOverrides

int ItemCountCore()
{
    if (var context = m_nonVirtualizingContext.get())
    {
        return context.Children().Size();
    }
    return 0;
}

IInspectable GetItemAtCore(int index)
{
    if (var context = m_nonVirtualizingContext.get())
    {
        return context.Children().GetAt(index);
    }
    return null;
}

UIElement GetOrCreateElementAtCore(int index, ElementRealizationOptions const& options)
{
    if (var context = m_nonVirtualizingContext.get())
    {
        return context.Children().GetAt(index);
    }
    return null;
}

void RecycleElementCore(UIElement const& element)
{

}

int GetElementIndexCore(UIElement const& element)
{
    if (var context = m_nonVirtualizingContext.get())
    {
        var children = context.Children();
        for (uint  i = 0; i < children.Size(); i++)
        {
            if (children.GetAt(i) == element)
            {
                return i;
            }
        }
    }
    
    return -1;
}

Rect RealizationRectCore()
{
    return Rect{ 0, 0, std.numeric_limits<float>.infinity(), std.numeric_limits<float>.infinity() };
}

int RecommendedAnchorIndexCore()
{
    return -1;
}

Point LayoutOriginCore()
{
    return Point(0, 0);
}

void LayoutOriginCore(Point const& value)
{
    if (value != Point(0, 0))
    {
        throw hresult_invalid_argument("LayoutOrigin must be at (0,0) when RealizationRect is infinite sized.");
    }
}

#endregion
