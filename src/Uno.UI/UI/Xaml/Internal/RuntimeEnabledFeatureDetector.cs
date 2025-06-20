namespace Uno.UI.Xaml.Controls;

internal struct RuntimeEnabledFeatureDetector
{
	internal readonly bool IsFeatureEnabled(RuntimeEnabledFeature feature)
	{
		switch (feature)
		{
			case RuntimeEnabledFeature.DoNotSetRootScrollViewerBackground:
				return false;
			default:
				return false;
		}
	}
}
