#nullable enable

using View = Microsoft.UI.Xaml.UIElement;


namespace Microsoft.UI.Xaml;

internal interface IFrameworkTemplateInternal
{
	View? LoadContent(DependencyObject? templatedParent);
}
