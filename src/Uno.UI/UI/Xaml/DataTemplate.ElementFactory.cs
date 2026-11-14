#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;

namespace Microsoft.UI.Xaml;

// MUX Reference DataTemplate_Partial.cpp (GetElementImpl, RecycleElementImpl)
partial class DataTemplate
{
	// Elements handed back through RecycleElement, with the parent they were recycled from.
	private List<(UIElement Element, WeakReference<UIElement>? Parent)>? _recycledElements;

	/// <summary>
	/// Creates or retrieves an existing instance of the UIElement object declared in the DataTemplate.
	/// </summary>
	/// <param name="args">An instance of <see cref="ElementFactoryGetArgs"/>.</param>
	/// <returns>A UIElement, reused from the elements given back through <see cref="RecycleElement"/> when there is one.</returns>
	public UIElement GetElement(ElementFactoryGetArgs args)
	{
		var element = TryTakeRecycledElement(args.Parent);
		if (element is null)
		{
			// The empty-template fallback matches ItemTemplateWrapper; WinUI has no defined result here.
			element = LoadContent() as UIElement ?? new Rectangle { Width = 0, Height = 0 };
		}

		// Lets DataTemplateSelector.RecycleElement find the template that produced the element.
		RecyclePool.SetOriginTemplate(element, this);

		return element;
	}

	/// <summary>
	/// Recycles a UIElement that was previously retrieved using <see cref="GetElement"/>.
	/// </summary>
	/// <param name="args">An instance of <see cref="ElementFactoryRecycleArgs"/>.</param>
	public void RecycleElement(ElementFactoryRecycleArgs args)
	{
		if (args.Element is not { } element)
		{
			return;
		}

		(_recycledElements ??= new()).Add((element, args.Parent is { } parent ? new WeakReference<UIElement>(parent) : null));
	}

	private UIElement? TryTakeRecycledElement(UIElement? parent)
	{
		if (_recycledElements is not { Count: > 0 } recycled)
		{
			return null;
		}

		// Prefer an element recycled from the same parent, or from none, to avoid moving it between parents.
		var index = recycled.FindIndex(entry => entry.Parent is null || !entry.Parent.TryGetTarget(out var previous) || previous == parent);
		if (index == -1)
		{
			// No match: moving an existing element is still cheaper than creating a new one.
			index = recycled.Count - 1;
		}

		var (element, previousParentRef) = recycled[index];
		recycled.RemoveAt(index);

		if (previousParentRef?.TryGetTarget(out var previousParent) is true
			&& previousParent is Panel previousPanel
			&& previousParent != parent)
		{
			previousPanel.Children.Remove(element);
		}

		return element;
	}
}
