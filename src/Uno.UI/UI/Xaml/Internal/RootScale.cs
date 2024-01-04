#nullable enable

using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Core
{
	internal static class RootScale
	{
		[NotImplemented]
		internal static float GetRasterizationScaleForContentRoot(ContentRoot? contentRoot) => 1f;

		internal static double GetRasterizationScaleForElement(DependencyObject element)
		{
			var rootScale = GetRootScaleForElement(element);
			return rootScale ?? 1.0d;
		}

		internal static double? GetRootScaleForElement(DependencyObject element)
		{
			if (element is FrameworkElement fe)
			{
				return fe.GetScaleFactorForLayoutRounding();
			}
			return null;
		}
	}
}
