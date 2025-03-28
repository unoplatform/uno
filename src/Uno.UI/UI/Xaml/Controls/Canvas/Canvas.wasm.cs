namespace Windows.UI.Xaml.Controls
{
	partial class Canvas
	{
		static partial void OnZIndexChangedPartial(UIElement element, int? zindex)
		{
			if (zindex is { } d)
			{
				element.SetStyle("z-index", d);
			}
			else
			{
				element.ResetStyle("z-index");
			}
		}
	}
}
