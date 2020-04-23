// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include <pch.h>
#include <common.h>
#include <DoubleUtil.h>
#include "ItemsRepeater.common.h"
#include "FlowLayoutAlgorithm.h"
#include "StackLayoutState.h"
#include "StackLayout.h"
#include "RuntimeProfiler.h"
#include "VirtualizingLayoutContext.h"

#region IFlowLayout

StackLayout()
{
    __RP_Marker_ClassById(RuntimeProfiler.ProfId_StackLayout);
    LayoutId("StackLayout");
}

#endregion

#region IVirtualizingLayoutOverrides

void InitializeForContextCore(VirtualizingLayoutContext const& context)
{
    var state = context.LayoutState();
    com_ptr<StackLayoutState> stackState = null;
    if (state)
    {
        stackState = GetAsStackState(state);
    }

    if (!stackState)
    {
        if (state)
        {
            throw hresult_error(E_FAIL, "LayoutState must derive from StackLayoutState.");
        }

        // Custom deriving layouts could potentially be stateful.
        // If that is the case, we will just create the base state required by UniformGridLayout ourselves.
        stackState = new StackLayoutState();
    }

    stackState.InitializeForContext(context, this);
}

void UninitializeForContextCore(VirtualizingLayoutContext const& context)
{
    var stackState = GetAsStackState(context.LayoutState());
    stackState.UninitializeForContext(context);
}

Size MeasureOverride(
    VirtualizingLayoutContext const& context,
    Size const& availableSize)
{
    GetAsStackState(context.LayoutState()).OnMeasureStart();

    var desiredSize = GetFlowAlgorithm(context).Measure(
        availableSize,
        context,
        false, /* isWrapping*/
        0 /* minItemSpacing */,
        m_itemSpacing,
        MAXUINT /* maxItemsPerLine */,
        GetScrollOrientation(),
        DisableVirtualization(),
        LayoutId());
    return { desiredSize.Width, desiredSize.Height };
}

Size ArrangeOverride(
    VirtualizingLayoutContext const& context,
    Size const& finalSize)
{
    var value = GetFlowAlgorithm(context).Arrange(
        finalSize,
        context,
        false, /* isWraping */
        FlowLayoutAlgorithm.LineAlignment.Start,
        LayoutId());

    return { value.Width, value.Height };
}

void OnItemsChangedCore(
    VirtualizingLayoutContext const& context,
    IInspectable const& source,
    NotifyCollectionChangedEventArgs const& args)
{
    GetFlowAlgorithm(context).OnItemsSourceChanged(source, args, context);
    // Always invalidate layout to keep the view accurate.
    InvalidateLayout();
}

#endregion

#region IStackLayoutOverrides

FlowLayoutAnchorInfo GetAnchorForRealizationRect(
    Size const& availableSize,
    VirtualizingLayoutContext const& context)
{

    int anchorIndex = -1;
    double offset = DoubleUtil.NaN;

    // Constants
    const int itemsCount = context.ItemCount();
    if (itemsCount > 0)
    {
        const var realizationRect = context.RealizationRect();
        const var state = GetAsStackState(context.LayoutState());
        const var lastExtent = state.FlowAlgorithm().LastExtent();

        const double averageElementSize = GetAverageElementSize(availableSize, context, state) + m_itemSpacing;
        const double realizationWindowOffsetInExtent = MajorStart(realizationRect) - MajorStart(lastExtent);
        const double majorSize = MajorSize(lastExtent) == 0 ? std.max(0.0, averageElementSize * itemsCount - m_itemSpacing) : MajorSize(lastExtent);
        if (itemsCount > 0 &&
            MajorSize(realizationRect) >= 0 &&
            // MajorSize = 0 will account for when a nested repeater is outside the realization rect but still being measured. Also,
            // note that if we are measuring this repeater, then we are already realizing an element to figure out the size, so we could
            // just keep that element alive. It also helps in XYFocus scenarios to have an element realized for XYFocus to find a candidate
            // in the navigating direction.
            realizationWindowOffsetInExtent + MajorSize(realizationRect) >= 0 && realizationWindowOffsetInExtent <= majorSize)
        {
            anchorIndex = (int)(realizationWindowOffsetInExtent / averageElementSize);
            offset = anchorIndex * averageElementSize + MajorStart(lastExtent);
            anchorIndex = std.max(0, std.min(itemsCount - 1, anchorIndex));
        }
    }

    return { anchorIndex, offset };
}

Rect GetExtent(
    Size const& availableSize,
    VirtualizingLayoutContext const& context,
    UIElement const& firstRealized,
    int firstRealizedItemIndex,
    Rect const& firstRealizedLayoutBounds,
    UIElement const& lastRealized,
    int lastRealizedItemIndex,
    Rect const& lastRealizedLayoutBounds)
{
    UNREFERENCED_PARAMETER(lastRealized);

    var extent = Rect{};

    // Constants
    const int itemsCount = context.ItemCount();
    const var stackState = GetAsStackState(context.LayoutState());
    const double averageElementSize = GetAverageElementSize(availableSize, context, stackState) + m_itemSpacing;

    MinorSize(extent) = (float)(stackState.MaxArrangeBounds());
    MajorSize(extent) = std.max(0.0f, (float)(itemsCount * averageElementSize - m_itemSpacing));
    if (itemsCount > 0)
    {
        if (firstRealized)
        {
            MUX_ASSERT(lastRealized);
            MajorStart(extent) = (float)(MajorStart(firstRealizedLayoutBounds) - firstRealizedItemIndex * averageElementSize);
            var remainingItems = itemsCount - lastRealizedItemIndex - 1;
            MajorSize(extent) = MajorEnd(lastRealizedLayoutBounds) - MajorStart(extent) + (float)(remainingItems* averageElementSize);
        }
        else
        {
            REPEATER_TRACE_INFO("%*s: \tEstimating extent with no realized elements.  \n",
                get_self<VirtualizingLayoutContext>(context).Indent(),
                LayoutId().data());
        }
    }
    else
    {
        MUX_ASSERT(firstRealizedItemIndex == -1);
        MUX_ASSERT(lastRealizedItemIndex == -1);
    }

    REPEATER_TRACE_INFO("%*s: \tExtent is (%.0f,%.0f). Based on average %.0f. \n",
        get_self<VirtualizingLayoutContext>(context).Indent(),
        LayoutId().data(), extent.Width, extent.Height, averageElementSize);
    return extent;
}

void OnElementMeasured(
    UIElement const& /*element*/,
    int index,
    Size const& /*availableSize*/,
    Size const& /*measureSize*/,
    Size const& /*desiredSize*/,
    Size const& provisionalArrangeSize,
    VirtualizingLayoutContext const& context)
{

    const var virtualContext = context.try_as<VirtualizingLayoutContext>();
    if (virtualContext)
    {
        const var stackState = GetAsStackState(virtualContext.LayoutState());
        const var provisionalArrangeSizeWinRt = provisionalArrangeSize;
        stackState.OnElementMeasured(
            index,
            Major(provisionalArrangeSizeWinRt),
            Minor(provisionalArrangeSizeWinRt));
    }
}

#endregion

#region IFlowLayoutAlgorithmDelegates

Size StackLayout.Algorithm_GetMeasureSize(int /*index*/, const Size & availableSize, const VirtualizingLayoutContext& /*context*/)
{
    return availableSize;
}

Size StackLayout.Algorithm_GetProvisionalArrangeSize(int /*index*/, const Size & measureSize, Size const& desiredSize, const VirtualizingLayoutContext& /*context*/)
{
    const var measureSizeMinor = Minor(measureSize);
    return MinorMajorSize(
        std.isfinite(measureSizeMinor) ?
            std.max(measureSizeMinor, Minor(desiredSize)) :
            Minor(desiredSize),
        Major(desiredSize));
}

bool StackLayout.Algorithm_ShouldBreakLine(int /*index*/, double /*remainingSpace*/)
{
    return true;
}

FlowLayoutAnchorInfo StackLayout.Algorithm_GetAnchorForRealizationRect(
    const Size & availableSize,
    const VirtualizingLayoutContext & context)
{
    return GetAnchorForRealizationRect(availableSize, context);
}

FlowLayoutAnchorInfo StackLayout.Algorithm_GetAnchorForTargetElement(
    int targetIndex,
    const Size & availableSize,
    const VirtualizingLayoutContext & context)
{
    double offset = DoubleUtil.NaN;
    int index = -1;
    const int itemsCount = context.ItemCount();

    if (targetIndex >= 0 && targetIndex < itemsCount)
    {
        index = targetIndex;
        const var state = GetAsStackState(context.LayoutState());
        const double averageElementSize = GetAverageElementSize(availableSize, context, state) + m_itemSpacing;
        offset = index * averageElementSize + MajorStart(state.FlowAlgorithm().LastExtent());
    }

    return FlowLayoutAnchorInfo{ index, offset };
}

Rect StackLayout.Algorithm_GetExtent(
    const Size & availableSize,
    const VirtualizingLayoutContext & context,
    const UIElement & firstRealized,
    int firstRealizedItemIndex,
    const Rect & firstRealizedLayoutBounds,
    const UIElement & lastRealized,
    int lastRealizedItemIndex,
    const Rect & lastRealizedLayoutBounds)
{
    return GetExtent(
        availableSize,
        context,
        firstRealized,
        firstRealizedItemIndex,
        firstRealizedLayoutBounds,
        lastRealized,
        lastRealizedItemIndex,
        lastRealizedLayoutBounds);
}

void StackLayout.Algorithm_OnElementMeasured(
    const UIElement & element,
    int index,
    const Size & availableSize,
    const Size & measureSize,
    const Size & desiredSize,
    const Size & provisionalArrangeSize,
    const VirtualizingLayoutContext & context)
{
    OnElementMeasured(
        element,
        index,
        availableSize,
        measureSize,
        desiredSize,
        provisionalArrangeSize,
        context);
}

#endregion

void OnPropertyChanged(const DependencyPropertyChangedEventArgs& args)
{
    var property = args.Property();
    if (property == s_OrientationProperty)
    {
        var orientation = unbox_value<Orientation>(args.NewValue());

        //Note: For StackLayout Vertical Orientation means we have a Vertical ScrollOrientation.
        //Horizontal Orientation means we have a Horizontal ScrollOrientation.
        ScrollOrientation scrollOrientation = (orientation == Orientation.Horizontal) ? ScrollOrientation.Horizontal : ScrollOrientation.Vertical;
        OrientationBasedMeasures.SetScrollOrientation(scrollOrientation);
    }
    else if (property == s_SpacingProperty)
    {
        m_itemSpacing = unbox_value<double>(args.NewValue());
    }

    InvalidateLayout();
}

#region private helpers

double GetAverageElementSize(
    Size availableSize,
    VirtualizingLayoutContext context,
    const com_ptr<StackLayoutState>& stackLayoutState)
{
    double averageElementSize = 0;

    if (context.ItemCount() > 0)
    {
        if (stackLayoutState.TotalElementsMeasured() == 0)
        {
            const var tmpElement = context.GetOrCreateElementAt(0, ElementRealizationOptions.ForceCreate | ElementRealizationOptions.SuppressAutoRecycle);
            stackLayoutState.FlowAlgorithm().MeasureElement(tmpElement, 0, availableSize, context);
            context.RecycleElement(tmpElement);
        }

        MUX_ASSERT(stackLayoutState.TotalElementsMeasured() > 0);
        averageElementSize = round(stackLayoutState.TotalElementSize() / stackLayoutState.TotalElementsMeasured());
    }

    return averageElementSize;
}

#endregion
