// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBase_Partial_SemanticZoomInformation.cpp, tag winui3/release/1.8.4, commit dc46907e92

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class ListViewBase
{
	/// <summary>
	/// Prepares the view for a zoom transition.
	/// </summary>
	public void InitializeViewChange()
	{
		m_semanticZoomCompletedFocusState = FocusState.Programmatic;
		m_semanticZoomShouldTakeFocus = false;

		if (SemanticZoomOwner is { } owner)
		{
			m_semanticZoomShouldTakeFocus =
				owner.TryGetFocusState(out m_semanticZoomCompletedFocusState);

			if (owner.GetIsProcessingKeyboardInput())
			{
				m_semanticZoomCompletedFocusState = FocusState.Keyboard;
			}
			else if (owner.GetIsProcessingPointerInput())
			{
				m_semanticZoomCompletedFocusState = FocusState.Pointer;
			}
		}
	}

	/// <summary>
	/// Cleans up the view after a zoom transition.
	/// </summary>
	public void CompleteViewChange()
	{
		m_semanticZoomShouldTakeFocus = false;
	}

	/// <summary>
	/// Forces content to scroll until the coordinate space of the SemanticZoomLocation is visible.
	/// </summary>
	/// <param name="item">The item and bounds to make visible.</param>
	public void MakeVisible(SemanticZoomLocation item)
	{
		if (ResolveSemanticZoomItem(item.Item) is not { } targetItem)
		{
			return;
		}

		ScrollIntoView(targetItem, ScrollIntoViewAlignment.Leading);
		UpdateLayout();
		UpdateLocationBounds(item, targetItem);
	}

	/// <summary>
	/// Provides the source and destination items when this is the active view.
	/// </summary>
	/// <param name="source">The source location.</param>
	/// <param name="destination">The destination location.</param>
	public void StartViewChangeFrom(SemanticZoomLocation source, SemanticZoomLocation destination)
	{
		var sourceItem =
			source.Item ??
			FindItemAtZoomPoint(source.ZoomPoint) ??
			GetFocusedSemanticZoomItem() ??
			SelectedItem ??
			(Items.Count > 0 ? Items[0] : null);

		if (sourceItem is null)
		{
			return;
		}

		source.Item = sourceItem;
		UpdateLocationBounds(source, sourceItem);

		if (destination.Item is null && TryGetSemanticZoomGroup(sourceItem) is { } group)
		{
			destination.Item = IsZoomedInView ? group : group.Group;
		}
	}

	/// <summary>
	/// Provides the source and destination items when this is the inactive view.
	/// </summary>
	/// <param name="source">The source location.</param>
	/// <param name="destination">The destination location.</param>
	public void StartViewChangeTo(SemanticZoomLocation source, SemanticZoomLocation destination)
	{
	}

	/// <summary>
	/// Completes the change away from this view.
	/// </summary>
	/// <param name="source">The source location.</param>
	/// <param name="destination">The destination location.</param>
	public void CompleteViewChangeFrom(SemanticZoomLocation source, SemanticZoomLocation destination)
	{
	}

	/// <summary>
	/// Completes the change that makes this the active view.
	/// </summary>
	/// <param name="source">The source location.</param>
	/// <param name="destination">The destination location.</param>
	public void CompleteViewChangeTo(SemanticZoomLocation source, SemanticZoomLocation destination)
	{
		if (!m_semanticZoomShouldTakeFocus)
		{
			return;
		}

		var targetItem = ResolveSemanticZoomItem(destination.Item);
		if (TryFocusSemanticZoomTarget(targetItem))
		{
			return;
		}

		m_semanticZoomPendingFocusItem = targetItem;
		if (!m_semanticZoomFocusQueued)
		{
			m_semanticZoomFocusQueued =
				DispatcherQueue.TryEnqueue(CompleteDeferredSemanticZoomFocus);
		}

		if (!m_semanticZoomFocusQueued)
		{
			m_semanticZoomPendingFocusItem = null;
			Focus(m_semanticZoomCompletedFocusState);
		}
	}

	private bool TryFocusSemanticZoomTarget(object? targetItem)
		=> targetItem is not null &&
			ContainerFromItem(targetItem) is Control container &&
			container.Focus(m_semanticZoomCompletedFocusState);

	private void CompleteDeferredSemanticZoomFocus()
	{
		m_semanticZoomFocusQueued = false;
		var targetItem = m_semanticZoomPendingFocusItem;
		m_semanticZoomPendingFocusItem = null;

		if (!IsActiveView)
		{
			return;
		}

		if (targetItem is not null)
		{
			ScrollIntoView(targetItem);
		}

		UpdateLayout();
		if (!TryFocusSemanticZoomTarget(targetItem))
		{
			Focus(m_semanticZoomCompletedFocusState);
		}
	}

	private object? FindItemAtZoomPoint(Point point)
	{
		if (point == default)
		{
			return null;
		}

		FrameworkElement? closestContainer = null;
		var closestDistance = double.PositiveInfinity;

		foreach (var container in GetItemsPanelChildren().OfType<FrameworkElement>())
		{
			var bounds = container
				.TransformToVisual(this)
				.TransformBounds(new Rect(0, 0, container.ActualWidth, container.ActualHeight));

			if (point.X >= bounds.X &&
				point.X <= bounds.Right &&
				point.Y >= bounds.Y &&
				point.Y <= bounds.Bottom)
			{
				return ItemFromContainer(container);
			}

			var deltaX = point.X - (bounds.X + bounds.Width / 2);
			var deltaY = point.Y - (bounds.Y + bounds.Height / 2);
			var distance = deltaX * deltaX + deltaY * deltaY;
			if (distance < closestDistance)
			{
				closestDistance = distance;
				closestContainer = container;
			}
		}

		return closestContainer is null ? null : ItemFromContainer(closestContainer);
	}

	private object? GetFocusedSemanticZoomItem()
	{
		if (XamlRoot is null ||
			FocusManager.GetFocusedElement(XamlRoot) is not DependencyObject focusedElement)
		{
			return null;
		}

		for (var current = focusedElement; current is not null && current != this; current = VisualTreeHelper.GetParent(current))
		{
			if (current is SelectorItem selectorItem)
			{
				return ItemFromContainer(selectorItem);
			}
		}

		return null;
	}

	private void UpdateLocationBounds(SemanticZoomLocation location, object item)
	{
		if (ContainerFromItem(item) is not FrameworkElement container)
		{
			return;
		}

		location.Bounds = container
			.TransformToVisual(this)
			.TransformBounds(new Rect(0, 0, container.ActualWidth, container.ActualHeight));
	}

	private object? ResolveSemanticZoomItem(object? item)
	{
		if (item is null)
		{
			return null;
		}

		if (!IsZoomedInView)
		{
			return item;
		}

		if (item is not ICollectionViewGroup)
		{
			foreach (var candidate in GetItems())
			{
				if (Equals(candidate, item))
				{
					return item;
				}
			}
		}

		var group = item as ICollectionViewGroup ?? TryGetSemanticZoomGroup(item);
		return group is { GroupItems.Count: > 0 } ? group.GroupItems[0] : item;
	}

	private ICollectionViewGroup? TryGetSemanticZoomGroup(object item)
	{
		if (item is ICollectionViewGroup group)
		{
			return group;
		}

		if (IsGrouping)
		{
			var indexPath = GetIndexPathFromItem(item);
			if (indexPath.Section >= 0 &&
				indexPath.Section < NumberOfGroups &&
				indexPath.Row >= 0 &&
				indexPath.Row < GetGroupCount(indexPath.Section))
			{
				var indexedGroup = GetGroupAt(indexPath.Section);
				if (Equals(indexedGroup.GroupItems[indexPath.Row], item))
				{
					return indexedGroup;
				}
			}
		}

		var groups = GetSemanticZoomGroups();
		if (groups is not null)
		{
			foreach (var candidate in groups)
			{
				if (candidate is ICollectionViewGroup candidateGroup &&
					(ReferenceEquals(candidateGroup, item) || Equals(candidateGroup.Group, item)))
				{
					return candidateGroup;
				}
			}

			foreach (var candidate in groups)
			{
				if (candidate is not ICollectionViewGroup candidateGroup)
				{
					continue;
				}

				foreach (var groupItem in candidateGroup.GroupItems)
				{
					if (Equals(groupItem, item))
					{
						return candidateGroup;
					}
				}
			}
		}

		if (!IsZoomedInView &&
			SemanticZoomOwner?.ZoomedInView is ListViewBase zoomedInView &&
			!ReferenceEquals(zoomedInView, this))
		{
			return zoomedInView.TryGetSemanticZoomGroup(item);
		}

		return null;
	}

	private global::Windows.Foundation.Collections.IObservableVector<object>? GetSemanticZoomGroups()
		=> (UnwrapItemsSource() as ICollectionView)?.CollectionGroups;
}
