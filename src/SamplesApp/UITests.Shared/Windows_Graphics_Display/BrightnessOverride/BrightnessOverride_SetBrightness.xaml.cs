using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
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

namespace UITests.Shared.Windows_Graphics_Display.BrightnessOverride
{
	[SampleControlInfoAttribute("BrightnessOverride", "SetBrightness", null, false, "Change the screen brightness")]
	public sealed partial class BrightnessOverride_SetBrightness : UserControl
	{
		public BrightnessOverride_SetBrightness()
		{
			this.InitializeComponent();
#if XAMARIN
			SetBrightness.Click += SetBrightness_OnClick;
			Reset.Click += Reset_OnClick;
#endif
		}

#if XAMARIN
		private void SetBrightness_OnClick(object sender, RoutedEventArgs e)
		{
			var bo = Windows.Graphics.Display.BrightnessOverride.GetForCurrentView();

			bo.SetBrightnessLevel(0.5, Windows.Graphics.Display.DisplayBrightnessOverrideOptions.None);
			bo.StartOverride();
		}

		private void Reset_OnClick(object sender, RoutedEventArgs e)
		{
			var bo = Windows.Graphics.Display.BrightnessOverride.GetForCurrentView();

			bo.StopOverride();
		}
#endif
	}
}
