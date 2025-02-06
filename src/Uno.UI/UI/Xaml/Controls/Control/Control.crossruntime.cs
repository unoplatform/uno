using System.Linq;

namespace Windows.UI.Xaml.Controls;

public partial class Control
{
	partial void UnregisterSubView()
	{
		var child = this.GetChildren()?.FirstOrDefault();
		if (child != null)
		{
			RemoveChild(child);
		}
	}

	partial void RegisterSubView(UIElement child)
	{
		AddChild(child);
	}

	/// <summary>
	/// Gets the first sub-view of this control or null if there is none
	/// </summary>
	internal IFrameworkElement GetTemplateRoot()
	{
		return this.GetChildren()?.FirstOrDefault() as IFrameworkElement;
	}
}
