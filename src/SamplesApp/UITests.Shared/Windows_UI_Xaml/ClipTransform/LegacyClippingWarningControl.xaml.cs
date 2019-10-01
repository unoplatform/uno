using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SamplesApp.Windows_UI_Xaml.ClipTransform
{
    public sealed partial class LegacyClippingWarningControl : UserControl
    {
		public LegacyClippingWarningControl()
		{
			this.InitializeComponent();
        }

		public void OnControlLoaded(object sender, RoutedEventArgs args)
		{
#if __IOS__ || __ANDROID__
			if (global::Uno.UI.FeatureConfiguration.UIElement.UseLegacyClipping)
			{
				clippingWarning.Text = "FeatureConfiguration.UIElement.UseLegacyClipping is enabled, this sample will not show proper results.";
			}
#endif
		}
	}
}
