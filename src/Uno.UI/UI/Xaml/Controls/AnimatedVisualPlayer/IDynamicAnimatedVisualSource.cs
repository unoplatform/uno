namespace Windows.UI.Xaml.Controls
{
	public partial interface IDynamicAnimatedVisualSource : IAnimatedVisualSource
	{
		void SetColorProperty(string propertyName, Windows.UI.Color? color);
	}
}
