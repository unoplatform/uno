using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace Uno.UI.Samples.Content.UITests.CommandBar
{
#if __WASM__
	[Sample("CommandBar", Name = "Flyouts", IgnoreInSnapshotTests = true)]
#else
	[Sample("CommandBar", Name = "Flyouts")]
#endif
	public sealed partial class CommandBar_Flyout : UserControl
	{
		public CommandBar_Flyout()
		{
			this.InitializeComponent();
		}
	}
}
