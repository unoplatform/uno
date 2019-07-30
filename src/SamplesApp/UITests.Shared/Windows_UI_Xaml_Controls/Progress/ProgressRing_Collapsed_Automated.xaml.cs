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

namespace UITests.Shared.Windows_UI_Xaml_Controls.Progress
{
	[SampleControlInfo("Progress", nameof(ProgressRing_Collapsed_Automated), description: "Four active progress rings that are collapsed")]
	public sealed partial class ProgressRing_Collapsed_Automated : Page
	{
		public ProgressRing_Collapsed_Automated()
		{
			this.InitializeComponent();
		}
	}
}
