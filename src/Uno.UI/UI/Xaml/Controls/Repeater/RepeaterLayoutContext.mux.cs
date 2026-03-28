// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RepeaterLayoutContext.cpp, commit 5f9e851133b3

using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

partial class RepeaterLayoutContext
{
	public RepeaterLayoutContext(ItemsRepeater owner)
	{
		m_owner = new WeakReference<ItemsRepeater>(owner);
	}

	// #pragma region ILayoutContext

	protected override int ItemCountCore() => GetOwner().ItemsSourceView?.Count ?? 0;

	protected override UIElement GetOrCreateElementAtCore(int index, ElementRealizationOptions options)
	{
		return GetOwner().GetElementImpl(index,
			(options & ElementRealizationOptions.ForceCreate) == ElementRealizationOptions.ForceCreate,
			(options & ElementRealizationOptions.SuppressAutoRecycle) == ElementRealizationOptions.SuppressAutoRecycle);
	}

	protected internal override object LayoutStateCore
	{
		get => GetOwner().LayoutState;
		set => GetOwner().LayoutState = value;
	}

	// #pragma endregion

	// #pragma region IVirtualizingLayoutContextOverrides

	protected override object GetItemAtCore(int index)
		=> GetOwner().ItemsSourceView.GetAt(index);

	protected override void RecycleElementCore(UIElement element)
	{
		var owner = GetOwner();
		REPEATER_TRACE_INFO("RepeaterLayout - RecycleElement: %d \n", owner.GetElementIndex(element));
		owner.ClearElementImpl(element);
	}

	protected override Rect VisibleRectCore() => GetOwner().VisibleWindow;

	protected override Rect RealizationRectCore() => GetOwner().RealizationWindow;

	protected override int RecommendedAnchorIndexCore
	{
		get
		{
			int anchorIndex = -1;
			var repeater = GetOwner();
			var anchor = repeater.SuggestedAnchor;
			if (anchor != null)
			{
				anchorIndex = repeater.GetElementIndex(anchor);
			}

			return anchorIndex;
		}
	}

	protected override Point LayoutOriginCore
	{
		get => GetOwner().LayoutOrigin;
		set => GetOwner().LayoutOrigin = value;
	}

	// #pragma endregion

	private ItemsRepeater GetOwner()
		=> m_owner.TryGetTarget(out var owner)
			? owner
			: throw new InvalidOperationException("Owning ItemsRepeater has been collected");
}
