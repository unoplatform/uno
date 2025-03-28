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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Uno.UI.Samples.Content.UITests.CommandBar
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
#if __WASM__
	[SampleControlInfo("CommandBar", "Long Title", ignoreInSnapshotTests: true)]
#else
	[SampleControlInfo("CommandBar", "Long Title")]
#endif
	public sealed partial class CommandBar_LongTitle : Page
	{
		public CommandBar_LongTitle()
		{
			this.InitializeComponent();
		}
	}
}
