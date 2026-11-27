using System;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Enables custom template selection logic at the application level.
/// </summary>
public partial class DataTemplateSelector
{
	/// <summary>
	/// Initializes a new instance of the DataTemplateSelector class.
	/// </summary>
	public DataTemplateSelector()
	{
	}

	/// <summary>
	/// Returns a specific DataTemplate for a given item.
	/// </summary>
	/// <param name="item">The item to return a template for.</param>
	/// <returns>The template to use for the given item and/or container.</returns>
	public DataTemplate SelectTemplate(object item) =>
		SelectTemplateCore(item);

	/// <summary>
	/// Returns a specific DataTemplate for a given item and container.
	/// </summary>
	/// <param name="item">The item to return a template for.</param>
	/// <param name="container">The parent container for the templated item.</param>
	/// <returns>The template to use for the given item and/or container.</returns>
	public DataTemplate SelectTemplate(object item, DependencyObject container) =>
		SelectTemplateCore(item, container);

	/// <summary>
	/// When implemented by a derived class, returns a specific DataTemplate for a given item or container.
	/// </summary>
	/// <param name="item">The item to return a template for.</param>
	/// <returns>The template to use for the given item and/or container.</returns>
	protected virtual DataTemplate SelectTemplateCore(object item) => null;

	/// <summary>
	/// When implemented by a derived class, returns a specific DataTemplate for a given item or container.
	/// </summary>
	/// <param name="item">The item to return a template for.</param>
	/// <param name="container">The parent container for the templated item.</param>
	/// <returns>The template to use for the given item and/or container.</returns>
	/// <remarks>
	/// Most implementations will choose to implement the selection logic based on the value
	/// of either item or container, not both. Implementations should still pass the unused
	/// parameter as-is to base.
	/// Base implementation throws when <paramref name="container"/> is null.
	/// </remarks>
	protected virtual DataTemplate SelectTemplateCore(object item, DependencyObject container)
	{
		if (container is null)
		{
			throw new ArgumentNullException(nameof(container));
		}

		return null;
	}

	// MUX Reference DataTemplateSelector_Partial.cpp (GetElementImpl, RecycleElementImpl)

	/// <summary>
	/// Creates or retrieves an existing instance of the UIElement object declared in the DataTemplate selected for the item.
	/// </summary>
	/// <param name="args">An instance of <see cref="global::Microsoft.UI.Xaml.ElementFactoryGetArgs"/>.</param>
	/// <returns>The element returned by the selected template's <see cref="DataTemplate.GetElement"/>.</returns>
	public UIElement GetElement(global::Microsoft.UI.Xaml.ElementFactoryGetArgs args)
	{
		var template = SelectTemplate(args.Data);
		if (template is null)
		{
			// Fall back to the overload with a null container, as WinUI does. Its base implementation
			// throws for a null container, which only means the selector didn't implement it (same as ItemTemplateWrapper).
			try
			{
				template = SelectTemplate(args.Data, null);
			}
			catch (ArgumentException)
			{
			}
		}

		return template?.GetElement(args)
			?? throw new InvalidOperationException($"{GetType()} selected no DataTemplate for '{args.Data}'.");
	}

	/// <summary>
	/// Recycles a UIElement that was previously retrieved using <see cref="GetElement"/>.
	/// </summary>
	/// <param name="args">An instance of <see cref="global::Microsoft.UI.Xaml.ElementFactoryRecycleArgs"/>.</param>
	public void RecycleElement(global::Microsoft.UI.Xaml.ElementFactoryRecycleArgs args)
	{
		// Hand the element back to the template that produced it.
		if (args.Element is { } element && RecyclePool.GetOriginTemplate(element) is { } template)
		{
			template.RecycleElement(args);
		}
	}
}

