using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml.ClipTransform
{
	public sealed partial class LegacyClippingWarningControl : UserControl
	{
		public LegacyClippingWarningControl()
		{
			this.InitializeComponent();
		}

		public void OnControlLoaded(object sender, RoutedEventArgs args)
		{
#if HAS_UNO
			if (global::Uno.UI.FeatureConfiguration.UIElement.UseLegacyClipping)
			{
				clippingWarning.Text = "FeatureConfiguration.UIElement.UseLegacyClipping is enabled, this sample will not show proper results.";
			}
#endif
		}
	}
}
