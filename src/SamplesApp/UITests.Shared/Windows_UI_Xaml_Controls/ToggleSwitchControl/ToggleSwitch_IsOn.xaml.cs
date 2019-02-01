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
using System.Globalization;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Uno.UI.Samples.Content.UITests.ToggleSwitchControl
{
	[SampleControlInfoAttribute("ToggleSwitchControl", "ToggleSwitch_IsOn", typeof(Uno.UI.Samples.Presentation.SamplePages.ToggleSwitchViewModel))]
	public sealed partial class ToggleSwitch_IsOn : UserControl
	{
		public ToggleSwitch_IsOn()
		{
			this.InitializeComponent();
		}
	}
}
