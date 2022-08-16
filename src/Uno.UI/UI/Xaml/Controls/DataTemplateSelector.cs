using System;

namespace Windows.UI.Xaml.Controls;

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
}

