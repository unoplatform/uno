using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls;

partial class MenuFlyoutPresenter
{
	/// <summary>
	/// Gets an object that provides calculated values that can be referenced
	/// as TemplateBinding sourceswhen defining templates for a MenuFlyoutPresenter control.
	/// </summary>
	public MenuFlyoutPresenterTemplateSettings TemplateSettings { get; private set; }
}
