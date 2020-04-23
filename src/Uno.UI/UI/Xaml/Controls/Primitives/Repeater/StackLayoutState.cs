// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include <pch.h>
#include <common.h>
#include "ItemsRepeater.common.h"
#include "FlowLayoutAlgorithm.h"
#include "StackLayoutState.h"

#include "StackLayoutState.properties.cpp"

void InitializeForContext(
    const VirtualizingLayoutContext& context,
    IFlowLayoutAlgorithmDelegates* callbacks)
{
    m_flowAlgorithm.InitializeForContext(context, callbacks);
    if (m_estimationBuffer.size() == 0)
    {
        m_estimationBuffer.resize(BufferSize, 0.0f);
    }

    context.LayoutStateCore(this);
}

void UninitializeForContext(const VirtualizingLayoutContext& context)
{
    m_flowAlgorithm.UninitializeForContext(context);
}

void OnElementMeasured(int elementIndex, double majorSize, double minorSize)
{
    int estimationBufferIndex = elementIndex % m_estimationBuffer.size();
    bool alreadyMeasured = m_estimationBuffer[estimationBufferIndex] != 0;
    if (!alreadyMeasured)
    {
        m_totalElementsMeasured++;
    }

    m_totalElementSize -= m_estimationBuffer[estimationBufferIndex];
    m_totalElementSize += majorSize;
    m_estimationBuffer[estimationBufferIndex] = majorSize;

    m_maxArrangeBounds = std.max(m_maxArrangeBounds, minorSize);
}

void OnMeasureStart()
{
    m_maxArrangeBounds = 0.0;
}
