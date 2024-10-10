using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Uno.UI.Samples.Controls;
using Uno.UI;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace UITests.Windows_UI_Xaml_Controls.ToolTip
{
	[SampleControlInfo(nameof(ToolTip), nameof(ToolTip_CollapsedHost), description: SampleDescription)]
	public sealed partial class ToolTip_CollapsedHost : Page
	{
		private const string SampleDescription =
			"This is used to simulate tooltip linger issue when the host control is no longer visible.";

		public ToolTip_CollapsedHost()
		{
			this.InitializeComponent();
		}
	}
}
