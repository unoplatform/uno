#if __ANDROID__ || __APPLE_UIKIT__ || IS_UNIT_TESTS || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__

namespace Microsoft.UI.Xaml;

public partial class Application
{
	private ApplicationHighContrastAdjustment _highContrastAdjustment = ApplicationHighContrastAdjustment.Auto;

	/// <summary>
	/// Gets or sets whether the framework automatically adjusts application visuals when high contrast is enabled.
	/// </summary>
#if __ANDROID__ || __APPLE_UIKIT__ || IS_UNIT_TESTS || __WASM__ || __NETSTD_REFERENCE__
	[global::Uno.NotImplemented("__ANDROID__", "__APPLE_UIKIT__", "IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
#endif
	public ApplicationHighContrastAdjustment HighContrastAdjustment
	{
		get => _highContrastAdjustment;
		set
		{
			if (_highContrastAdjustment != value)
			{
				_highContrastAdjustment = value;
				OnHighContrastAdjustmentChanged();
			}
		}
	}

	private void OnHighContrastAdjustmentChanged()
	{
#if __SKIA__
		foreach (var contentRoot in Uno.UI.Xaml.Core.CoreServices.Instance.ContentRootCoordinator.ContentRoots)
		{
			if (contentRoot.VisualTree?.RootElement is UIElement root)
			{
				root.NotifyApplicationHighContrastAdjustmentChangedCore();
			}
		}

#endif
	}
}

#endif
