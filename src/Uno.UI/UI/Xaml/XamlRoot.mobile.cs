#if !__CROSSRUNTIME__
namespace Microsoft.UI.Xaml;

public sealed partial class XamlRoot
{
	internal void InvalidateMeasure()
	{
		global::System.Diagnostics.Debug.WriteLine("Requesting layout on XamlRoot");
		if (VisualTree.RootVisual is { } rootVisual)
		{
#if __ANDROID__
			rootVisual.RequestLayout();
#elif __IOS__
			rootVisual.SetNeedsLayout();
#elif __MACOS__
			rootVisual.NeedsLayout = true;
#elif IS_UNIT_TESTS
			// Nothing
#else
#error Unrecognized platform
#endif
		}
	}

	internal void InvalidateArrange()
	{
		if (VisualTree.RootVisual is { } rootVisual)
		{
#if __ANDROID__
			rootVisual.RequestLayout();
#elif __IOS__
			rootVisual.SetNeedsLayout();
#elif __MACOS__
			rootVisual.NeedsLayout = true;
#elif IS_UNIT_TESTS
			// Nothing
#else
#error Unrecognized platform
#endif
		}
	}
}
#endif
