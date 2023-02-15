namespace Windows.UI.Xaml.Controls;

partial class Border
{
	internal override void OnArrangeVisual(Rect rect, Rect? clip)
	{
		UpdateBorder();

		base.OnArrangeVisual(rect, clip);
	}
}
