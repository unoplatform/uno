namespace Windows.UI.Xaml.Controls;

public partial class Canvas
{
	static partial void OnZIndexChangedPartial(UIElement element, int? zindex)
	{
		element.Visual.ZIndex = (int)zindex;
		element._children.ClearCachedReverseSortedList();
	}
}
