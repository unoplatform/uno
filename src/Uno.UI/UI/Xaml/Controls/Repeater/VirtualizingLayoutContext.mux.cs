// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference VirtualizingLayoutContext.cpp, commit 5f9e851133b3

using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

partial class VirtualizingLayoutContext
{
	// #pragma region IVirtualizingLayoutContext

	/// <summary>
	/// Gets the number of items in the data source.
	/// </summary>
	public int ItemCount => ItemCountCore();

	/// <summary>
	/// Returns the data item in the source that corresponds to the specified index.
	/// </summary>
	/// <param name="index">The index of the item to retrieve.</param>
	/// <returns>The data item that corresponds to the specified index.</returns>
	public object GetItemAt(int index) => GetItemAtCore(index);

	/// <summary>
	/// Retrieves the UIElement that is being used to represent the data item at the specified index.
	/// </summary>
	/// <param name="index">The index of the item to retrieve a UIElement for.</param>
	/// <returns>The UIElement that is being used to represent the data item at the specified index.</returns>
	public UIElement GetOrCreateElementAt(int index)
		// Calling via overridable dispatch to ensure correct override resolution,
		// mirroring C++ get_strong().as<IVirtualizingLayoutContextOverrides>().GetOrCreateElementAtCore(...)
		=> GetOrCreateElementAtCore(index, ElementRealizationOptions.None);

	/// <summary>
	/// Retrieves the UIElement that is being used to represent the data item at the specified index using the specified options.
	/// </summary>
	/// <param name="index">The index of the item to retrieve a UIElement for.</param>
	/// <param name="options">A value of the enumeration that specifies options for element realization.</param>
	/// <returns>The UIElement that is being used to represent the data item at the specified index.</returns>
	public UIElement GetOrCreateElementAt(int index, ElementRealizationOptions options)
		// Calling via overridable dispatch to ensure correct override resolution,
		// mirroring C++ get_strong().as<IVirtualizingLayoutContextOverrides>().GetOrCreateElementAtCore(...)
		=> GetOrCreateElementAtCore(index, options);

	/// <summary>
	/// Indicates that the layout no longer needs the element corresponding to the specified index.
	/// </summary>
	/// <param name="element">The UIElement to recycle.</param>
	public void RecycleElement(UIElement element) => RecycleElementCore(element);

	/// <summary>
	/// Gets the visible viewport rectangle within the <see cref="Windows.UI.Xaml.FrameworkElement"/> associated with the <see cref="Layout"/>.
	/// </summary>
	/// <value>The visible viewport rectangle within the FrameworkElement associated with the Layout.</value>
	public Rect VisibleRect => VisibleRectCore();

	/// <summary>
	/// Gets a <see cref="Rect"/> value that represents the space available to the layout.
	/// </summary>
	/// <value>A Rect value that represents the space available to the layout.</value>
	public Rect RealizationRect => RealizationRectCore();

	/// <summary>
	/// Gets the index of the suggested anchor element, or -1 if no anchor is suggested.
	/// </summary>
	/// <value>The index of the suggested anchor element, or -1 if no anchor is suggested.</value>
	public int RecommendedAnchorIndex => RecommendedAnchorIndexCore;

	/// <summary>
	/// Gets or sets the origin point of the layout.
	/// </summary>
	/// <value>The origin point of the layout.</value>
	public Point LayoutOrigin
	{
		get => LayoutOriginCore;
		set => LayoutOriginCore = value;
	}

	// #pragma endregion

	// #pragma region IVirtualizingLayoutContextOverrides

	/// <summary>
	/// When overridden in a derived class, provides the item count for the layout context.
	/// </summary>
	/// <returns>The number of items in the data source.</returns>
	protected virtual int ItemCountCore() => throw new NotImplementedException();

	/// <summary>
	/// When overridden in a derived class, returns the data item at the specified index.
	/// </summary>
	/// <param name="index">The index of the item to retrieve.</param>
	/// <returns>The data item at the specified index.</returns>
	protected virtual object GetItemAtCore(int index) => throw new NotImplementedException();

	/// <summary>
	/// When overridden in a derived class, retrieves the UIElement for the data item at the specified index using the specified options.
	/// </summary>
	/// <param name="index">The index of the item to retrieve a UIElement for.</param>
	/// <param name="options">A value of the enumeration that specifies options for element realization.</param>
	/// <returns>The UIElement for the data item at the specified index.</returns>
	protected virtual UIElement GetOrCreateElementAtCore(int index, ElementRealizationOptions options)
		=> throw new NotImplementedException();

	/// <summary>
	/// When overridden in a derived class, marks the element as no longer needed by the layout.
	/// </summary>
	/// <param name="element">The UIElement to recycle.</param>
	protected virtual void RecycleElementCore(UIElement element) => throw new NotImplementedException();

	/// <summary>
	/// Provides the value that is assigned to the <see cref="VisibleRect"/> property.
	/// </summary>
	/// <returns>The value that is assigned to the <see cref="VisibleRect"/> property.</returns>
	protected virtual Rect VisibleRectCore() => throw new NotImplementedException();

	/// <summary>
	/// When overridden in a derived class, provides the realization rect for the layout context.
	/// </summary>
	/// <returns>The realization rect for the layout context.</returns>
	protected virtual Rect RealizationRectCore() => throw new NotImplementedException();

	/// <summary>
	/// When overridden in a derived class, provides the recommended anchor index for the layout context.
	/// </summary>
	/// <value>The recommended anchor index, or -1 if no anchor is suggested.</value>
	protected virtual int RecommendedAnchorIndexCore => throw new NotImplementedException();

	/// <summary>
	/// When overridden in a derived class, provides the layout origin for the layout context.
	/// </summary>
	/// <value>The origin point of the layout.</value>
	protected virtual Point LayoutOriginCore
	{
		get => throw new NotImplementedException();
		set => throw new NotImplementedException();
	}

	// #pragma endregion

	internal NonVirtualizingLayoutContext GetNonVirtualizingContextAdapter()
	{
		if (m_contextAdapter == null)
		{
			m_contextAdapter = new VirtualLayoutContextAdapter(this);
		}

		return m_contextAdapter;
	}
}
