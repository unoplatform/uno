#nullable enable

namespace Microsoft.UI.Xaml;

/// <summary>
/// Supports the creation and recycling of UIElement objects.
/// </summary>
public partial interface IElementFactory
{
	/// <summary>
	/// Gets a UIElement.
	/// </summary>
	/// <param name="args">An instance of <see cref="ElementFactoryGetArgs"/> that provides context for the operation.</param>
	/// <returns>A UIElement.</returns>
	UIElement GetElement(ElementFactoryGetArgs args);

	/// <summary>
	/// Recycles a UIElement that was previously retrieved using GetElement.
	/// </summary>
	/// <param name="args">An instance of <see cref="ElementFactoryRecycleArgs"/> that provides context for the operation.</param>
	void RecycleElement(ElementFactoryRecycleArgs args);
}
