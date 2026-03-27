// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference Layout.cpp, commit 5f9e851133b3

using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Private.Controls;

namespace Microsoft.UI.Xaml.Controls;

partial class Layout
{
	// #pragma region ILayout

	internal string LayoutId
	{
		get => m_layoutId;
		set => m_layoutId = value;
	}

	internal int LogItemIndexDbg()
	{
		return m_logItemIndexDbg;
	}

	internal void LogItemIndexDbg(int logItemIndex)
	{
		m_logItemIndexDbg = logItemIndex;
	}

	internal int LayoutAnchorIndexDbg()
	{
		return m_layoutAnchorInfoDbg.Index;
	}

	internal double LayoutAnchorOffsetDbg()
	{
		return m_layoutAnchorInfoDbg.Offset;
	}

	internal IndexBasedLayoutOrientation GetForcedIndexBasedLayoutOrientationDbg()
	{
		return m_forcedIndexBasedLayoutOrientationDbg;
	}

	void SetForcedIndexBasedLayoutOrientationDbg(IndexBasedLayoutOrientation forcedIndexBasedLayoutOrientation)
	{
		m_forcedIndexBasedLayoutOrientationDbg = forcedIndexBasedLayoutOrientation;
		m_isForcedIndexBasedLayoutOrientationSetDbg = true;
	}

	internal void ResetForcedIndexBasedLayoutOrientationDbg()
	{
		m_forcedIndexBasedLayoutOrientationDbg = IndexBasedLayoutOrientation.None;
		m_isForcedIndexBasedLayoutOrientationSetDbg = false;
	}

	private protected void SetLayoutAnchorInfoDbg(int index, double offset)
	{
		bool layoutAnchorIndexChanged = m_layoutAnchorInfoDbg.Index != index;
		bool layoutAnchorOffsetChanged = m_layoutAnchorInfoDbg.Offset != offset;

		m_layoutAnchorInfoDbg = (index, offset);

		if (layoutAnchorIndexChanged || layoutAnchorOffsetChanged)
		{
			var globalTestHooks = LayoutsTestHooks.GetGlobalTestHooks();

			if (globalTestHooks != null)
			{
				if (layoutAnchorIndexChanged)
				{
					globalTestHooks.NotifyLayoutAnchorIndexChanged(this);
				}
				if (layoutAnchorOffsetChanged)
				{
					globalTestHooks.NotifyLayoutAnchorOffsetChanged(this);
				}
			}
		}
	}

	/// <summary>
	/// Gets the orientation, if any, in which items are laid out based on their
	/// index in the source collection.
	/// </summary>
	/// <value>
	/// A value of the enumeration that indicates the orientation, if any, in which
	/// items are laid out based on their index in the source collection. The default
	/// is <see cref="IndexBasedLayoutOrientation.None"/>.
	/// </value>
	public IndexBasedLayoutOrientation IndexBasedLayoutOrientation
		=> m_isForcedIndexBasedLayoutOrientationSetDbg
			? m_forcedIndexBasedLayoutOrientationDbg
			: m_indexBasedLayoutOrientation;

	internal static VirtualizingLayoutContext GetVirtualizingLayoutContext(LayoutContext context)
	{
		switch (context)
		{
			case VirtualizingLayoutContext virtualizingContext:
				return virtualizingContext;
			case NonVirtualizingLayoutContext nonVirtualizingContext:
				return nonVirtualizingContext.GetVirtualizingContextAdapter();
			default:
				throw new NotImplementedException();
		}
	}

	internal static NonVirtualizingLayoutContext GetNonVirtualizingLayoutContext(LayoutContext context)
	{
		switch (context)
		{
			case NonVirtualizingLayoutContext nonVirtualizingContext:
				return nonVirtualizingContext;
			case VirtualizingLayoutContext virtualizingContext:
				return virtualizingContext.GetNonVirtualizingContextAdapter();
			default:
				throw new NotImplementedException();
		}
	}

	/// <summary>
	/// Initializes any per-container state the layout requires when it is attached to a UIElement container.
	/// </summary>
	/// <param name="context">The context object that facilitates communication between the layout and its host container.</param>
	public void InitializeForContext(LayoutContext context)
	{
		switch (this)
		{
			case VirtualizingLayout virtualizingLayout:
				virtualizingLayout.InitializeForContextCore(GetVirtualizingLayoutContext(context));
				break;
			case NonVirtualizingLayout nonVirtualizingLayout:
				nonVirtualizingLayout.InitializeForContextCore(GetNonVirtualizingLayoutContext(context));
				break;
			default:
				throw new NotImplementedException();
		}
	}

	/// <summary>
	/// Removes any state the layout previously stored on the UIElement container.
	/// </summary>
	/// <param name="context">The context object that facilitates communication between the layout and its host container.</param>
	public void UninitializeForContext(LayoutContext context)
	{
		switch (this)
		{
			case VirtualizingLayout virtualizingLayout:
				virtualizingLayout.UninitializeForContextCore(GetVirtualizingLayoutContext(context));
				break;
			case NonVirtualizingLayout nonVirtualizingLayout:
				nonVirtualizingLayout.UninitializeForContextCore(GetNonVirtualizingLayoutContext(context));
				break;
			default:
				throw new NotImplementedException();
		}
	}

	/// <summary>
	/// Suggests a DesiredSize for a container element. A container element that supports
	/// attached layouts should call this method from their own MeasureOverride implementations
	/// to form a recursive layout update. The attached layout is expected to call the Measure
	/// for each of the container’s UIElement children.
	/// </summary>
	/// <param name="context">The context object that facilitates communication between the layout and its host container.</param>
	/// <param name="availableSize">
	/// The available space that a container can allocate to a child object.
	/// A child object can request a larger space than what is available; the provided size
	/// might be accommodated if scrolling or other resize behavior is possible in that particular container.</param>
	/// <returns>The size that this object determines it needs during layout, based on its calculations of the allocated
	/// sizes for child objects or based on other considerations such as a fixed container size.</returns>
	/// <exception cref="NotImplementedException"></exception>
	public Size Measure(LayoutContext context, Size availableSize)
	{
		switch (this)
		{
			case VirtualizingLayout virtualizingLayout:
				return virtualizingLayout.MeasureOverride(GetVirtualizingLayoutContext(context), availableSize);
			case NonVirtualizingLayout nonVirtualizingLayout:
				return nonVirtualizingLayout.MeasureOverride(GetNonVirtualizingLayoutContext(context), availableSize);
			default:
				throw new NotImplementedException();
		}
	}

	/// <summary>
	/// Positions child elements and determines a size for a container UIElement. Container elements that
	/// support attached layouts should call this method from their layout override implementations to
	/// form a recursive layout update.
	/// </summary>
	/// <param name="context">The context object that facilitates communication between the layout and its host container.</param>
	/// <param name="finalSize">The final size that the container computes for the child in layout.</param>
	/// <returns>The actual size that is used after the element is arranged in layout.</returns>
	public Size Arrange(LayoutContext context, Size finalSize)
	{
		switch (this)
		{
			case VirtualizingLayout virtualizingLayout:
				return virtualizingLayout.ArrangeOverride(GetVirtualizingLayoutContext(context), finalSize);
			case NonVirtualizingLayout nonVirtualizingLayout:
				return nonVirtualizingLayout.ArrangeOverride(GetNonVirtualizingLayoutContext(context), finalSize);
			default:
				throw new NotImplementedException();
		}
	}

	// In C++ these use winrt::event_token add/remove; in C# they are plain events.

	/// <summary>Occurs when the measurement state (layout) has been invalidated.</summary>
	public event TypedEventHandler<Layout, object> MeasureInvalidated;

	/// <summary>Occurs when the arrange state (layout) has been invalidated.</summary>
	public event TypedEventHandler<Layout, object> ArrangeInvalidated;

	// #pragma endregion

	// #pragma region ILayoutProtected

	/// <summary>
	/// Invalidates the measurement state (layout) for all UIElement containers that reference this layout.
	/// </summary>
	// Uno-specific: widened to protected internal so LayoutsTestHooks (same assembly) can call it directly.
	protected internal void InvalidateMeasure()
	{
#if HAS_UNO
		NotifyMeasureInvalidatedUno();
#endif
		MeasureInvalidated?.Invoke(this, null);
	}

	/// <summary>
	/// Invalidates the arrange state (layout) for all UIElement containers that reference this layout.
	/// After the invalidation, the UIElement will have its layout updated, which occurs asynchronously.
	/// </summary>
	protected void InvalidateArrange()
	{
#if HAS_UNO
		NotifyArrangeInvalidatedUno();
#endif
		ArrangeInvalidated?.Invoke(this, null);
	}

	/// <summary>
	/// Sets the value of the <see cref="IndexBasedLayoutOrientation"/> property.
	/// </summary>
	/// <param name="orientation">
	/// A value of the enumeration that indicates the orientation, if any, in which
	/// items are laid out based on their index in the source collection.
	/// </param>
	protected void SetIndexBasedLayoutOrientation(IndexBasedLayoutOrientation orientation)
	{
		m_indexBasedLayoutOrientation = orientation;
	}

	// #pragma endregion
}
