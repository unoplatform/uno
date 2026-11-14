#nullable enable

namespace Microsoft.UI.Xaml;

/// <summary>
/// Represents the optional arguments to use when calling an implementation of the <see cref="IElementFactory"/>'s RecycleElement method.
/// </summary>
public partial class ElementFactoryRecycleArgs
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ElementFactoryRecycleArgs"/> class.
	/// </summary>
	public ElementFactoryRecycleArgs()
	{
	}

	/// <summary>
	/// Gets or sets the UIElement that was previously realized by the element factory and is now being recycled.
	/// </summary>
	public UIElement? Element { get; set; }

	/// <summary>
	/// Gets or sets a reference to the current parent UIElement of the element being recycled.
	/// </summary>
	public UIElement? Parent { get; set; }
}
