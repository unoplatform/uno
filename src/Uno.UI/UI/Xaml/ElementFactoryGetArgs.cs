#nullable enable

namespace Microsoft.UI.Xaml;

/// <summary>
/// Represents the optional arguments to use when calling an implementation of the <see cref="IElementFactory"/>'s GetElement method.
/// </summary>
public partial class ElementFactoryGetArgs
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ElementFactoryGetArgs"/> class.
	/// </summary>
	public ElementFactoryGetArgs()
	{
	}

	/// <summary>
	/// Gets or sets the data item for which an appropriate element tree should be realized when calling GetElement.
	/// </summary>
	public object? Data { get; set; }

	/// <summary>
	/// Gets or sets the UIElement that is expected to be the parent of the realized element from GetElement.
	/// </summary>
	public UIElement? Parent { get; set; }

	// Not part of the public contract: ItemsRepeater sets it for its test hooks, as WinUI does.
	internal int Index { get; set; }
}
