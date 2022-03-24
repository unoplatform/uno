namespace Windows.UI.Xaml;

public partial class UIElement
{
	partial void UnsetShadow(UIElement uiElement) => uiElement.Visual.HasThemeShadow = false;

	partial void SetShadow(UIElement uiElement) => uiElement.Visual.HasThemeShadow = true;
}
