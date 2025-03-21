using System.Linq;

namespace Microsoft.UI.Xaml.Controls;

public partial class Control
{
	/// <summary>
	/// Gets the first sub-view of this control or null if there is none
	/// </summary>
	internal IFrameworkElement GetTemplateRoot()
	{
		return this.GetChildren()?.FirstOrDefault() as IFrameworkElement;
	}
}
