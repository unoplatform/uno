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

namespace Uno.UI.Samples.Content.UITests.ToggleSwitchControl
{
	[SampleControlInfoAttribute("ToggleSwitchControl", "ToggleSwitch_Default_Style", typeof(Uno.UI.Samples.Presentation.SamplePages.ToggleSwitchViewModel))]
	public sealed partial class ToggleSwitch_Default_Style : UserControl
	{
		public ToggleSwitch_Default_Style()
		{
			this.InitializeComponent();
		}
	}
}
