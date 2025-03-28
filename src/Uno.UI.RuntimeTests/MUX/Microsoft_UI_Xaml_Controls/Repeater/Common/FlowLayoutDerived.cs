// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using ItemsSourceView = Microsoft/* UWP don't rename */.UI.Xaml.Controls.ItemsSourceView;
using FlowLayout = Microsoft/* UWP don't rename */.UI.Xaml.Controls.FlowLayout;
using FlowLayoutAnchorInfo = Microsoft/* UWP don't rename */.UI.Xaml.Controls.FlowLayoutAnchorInfo;
using NonVirtualizingLayoutContext = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NonVirtualizingLayoutContext;
using VirtualizingLayoutContext = Microsoft/* UWP don't rename */.UI.Xaml.Controls.VirtualizingLayoutContext;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests.Common
{
	public class FlowLayoutDerived : FlowLayout
	{
		public Func<Size, VirtualizingLayoutContext, Size, Size> MeasureLayoutFunc { get; set; }
		public Func<Size, VirtualizingLayoutContext, Size, Size> ArrangeLayoutFunc { get; set; }
		public Func<int, Size, VirtualizingLayoutContext, FlowLayoutAnchorInfo, FlowLayoutAnchorInfo> GetAnchorForTargetElementFunc { get; set; }
		public Func<Size, VirtualizingLayoutContext, FlowLayoutAnchorInfo, FlowLayoutAnchorInfo> GetAnchorForRealizationRectFunc { get; set; }
		public Func<Size, VirtualizingLayoutContext, UIElement, int, Rect, UIElement, int, Rect, Rect, Rect> GetExtentFunc { get; set; }
		public Func<int, Size, Size, Size> GetMeasureSizeFunc { get; set; }
		public Func<int, Size, Size, Size, Size> GetProvisionalArrangeSizeFunc { get; set; }
		public Action<VirtualizingLayoutContext> OnAttachedFunc { get; set; }
		public Action<VirtualizingLayoutContext> OnDetatchedFunc { get; set; }
		public Action<UIElement, int, Size, Size, Size, Size, VirtualizingLayoutContext> OnElementMeasuredFunc { get; set; }
		public Action<int, int, double, VirtualizingLayoutContext> OnLineArrangedFunc { get; set; }
		public Func<int, double, bool, bool> ShouldBreakLineFunc { get; set; }

		protected internal override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
		{
			var extent = base.MeasureOverride(context, availableSize);
			return MeasureLayoutFunc != null ? MeasureLayoutFunc(availableSize, context, extent) : extent;
		}

		protected internal override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
		{
			var extent = base.ArrangeOverride(context, finalSize);
			return ArrangeLayoutFunc != null ? ArrangeLayoutFunc(finalSize, context, extent) : extent;
		}

		protected override FlowLayoutAnchorInfo GetAnchorForTargetElement(int targetIndex, Size availableSize, VirtualizingLayoutContext context)
		{
			var anchorInfo = base.GetAnchorForTargetElement(targetIndex, availableSize, context);
			return GetAnchorForTargetElementFunc != null ? GetAnchorForTargetElementFunc(targetIndex, availableSize, context, anchorInfo) : anchorInfo;
		}

		protected override FlowLayoutAnchorInfo GetAnchorForRealizationRect(Size availableSize, VirtualizingLayoutContext context)
		{
			var anchorInfo = base.GetAnchorForRealizationRect(availableSize, context);
			return GetAnchorForRealizationRectFunc != null ? GetAnchorForRealizationRectFunc(availableSize, context, anchorInfo) : anchorInfo;
		}

		protected override Rect GetExtent(
			Size availableSize,
			VirtualizingLayoutContext context,
			UIElement firstRealized,
			int firstRealizedItemIndex,
			Rect firstRealizedLayoutBounds,
			UIElement lastRealized,
			int lastRealizedItemIndex,
			Rect lastRealizedLayoutBounds)
		{
			var extent = base.GetExtent(
				availableSize,
				context,
				firstRealized,
				firstRealizedItemIndex,
				firstRealizedLayoutBounds,
				lastRealized,
				lastRealizedItemIndex,
				lastRealizedLayoutBounds);

			return GetExtentFunc != null ? GetExtentFunc(
				availableSize,
				context,
				firstRealized,
				firstRealizedItemIndex,
				firstRealizedLayoutBounds,
				lastRealized,
				lastRealizedItemIndex,
				lastRealizedLayoutBounds,
				extent) : extent;
		}

		protected override Size GetMeasureSize(int index, Size availableSize)
		{
			var measureSize = base.GetMeasureSize(index, availableSize);
			return GetMeasureSizeFunc != null ? GetMeasureSizeFunc(index, availableSize, measureSize) : measureSize;
		}

		protected override Size GetProvisionalArrangeSize(int index, Size measureSize, Size desiredSize)
		{
			var arrangeSize = base.GetProvisionalArrangeSize(index, measureSize, desiredSize);
			return GetProvisionalArrangeSizeFunc != null ? GetProvisionalArrangeSizeFunc(index, measureSize, desiredSize, arrangeSize) : arrangeSize;
		}


		protected internal override void InitializeForContextCore(VirtualizingLayoutContext context)
		{
			base.InitializeForContextCore(context);
			if (OnAttachedFunc != null) { OnAttachedFunc(context); }
		}

		protected internal override void UninitializeForContextCore(VirtualizingLayoutContext context)
		{
			base.UninitializeForContextCore(context);
			if (OnDetatchedFunc != null) { OnDetatchedFunc(context); }
		}

		protected override void OnElementMeasured(UIElement element, int index, Size availableSize, Size measureSize, Size desiredSize, Size provisionalArrangeSize, VirtualizingLayoutContext context)
		{
			base.OnElementMeasured(element, index, availableSize, measureSize, desiredSize, provisionalArrangeSize, context);
			if (OnElementMeasuredFunc != null) { OnElementMeasuredFunc(element, index, availableSize, measureSize, desiredSize, provisionalArrangeSize, context); }
		}

		protected override void OnLineArranged(int startIndex, int countInLine, double lineSize, VirtualizingLayoutContext context)
		{
			base.OnLineArranged(startIndex, countInLine, lineSize, context);
			if (OnLineArrangedFunc != null) { OnLineArrangedFunc(startIndex, countInLine, lineSize, context); }
		}

		protected override bool ShouldBreakLine(int index, double remainingSpace)
		{
			var shouldBreak = base.ShouldBreakLine(index, remainingSpace);
			return ShouldBreakLineFunc != null ? ShouldBreakLineFunc(index, remainingSpace, shouldBreak) : shouldBreak;
		}
	}
}
