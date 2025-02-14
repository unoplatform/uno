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

namespace Uno.UI.Samples.Content.UITests.CommandBar
{
#if __WASM__
	[SampleControlInfo("CommandBar", "Flyouts", ignoreInSnapshotTests: true)]
#else
	[SampleControlInfo("CommandBar", "Flyouts")]
#endif
	public sealed partial class CommandBar_Flyout : UserControl
	{
		public CommandBar_Flyout()
		{
			this.InitializeComponent();
		}
	}
}
