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
	/// <summary>
	/// ignoreInAutomatedTests set to true because progressrings animate.
	/// </summary>
	[SampleControlInfo("Progress", nameof(ProgressRing_Active), ignoreInAutomatedTests: true, description: "Four active progress rings")]
	public sealed partial class ProgressRing_Active : Page
	{
		public ProgressRing_Active()
		{
			this.InitializeComponent();
		}
	}
}
