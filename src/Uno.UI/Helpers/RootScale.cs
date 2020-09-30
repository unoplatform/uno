using Windows.UI.Xaml;

namespace Uno.UI.Helpers
{
	internal static class RootScale
	{

		internal static double GetRasterizationScaleForElement(DependencyObject element)
		{
			var rootScale = GetRootScaleForElement(element);
			return rootScale ?? 1.0d;
		}

		internal static double? GetRootScaleForElement(DependencyObject element)
		{
			if(element is FrameworkElement fe)
			{
				return fe.GetScaleFactorForLayoutRounding();
			}
			return null;
		}
	}
}
