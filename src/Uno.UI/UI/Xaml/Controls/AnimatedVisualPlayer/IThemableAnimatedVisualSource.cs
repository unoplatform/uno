#nullable enable
namespace Windows.UI.Xaml.Controls
{
	public partial interface IThemableAnimatedVisualSource : IAnimatedVisualSource
	{
		void SetColorThemeProperty(string propertyName, Windows.UI.Color? color);
		Windows.UI.Color? GetColorThemeProperty(string propertyName);
	}
}
