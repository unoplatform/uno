using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.Disposables;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Presentation.SamplePages;
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

namespace Uno.UI.Samples.Content.UITests.FocusManager
{
	[SampleControlInfo("FocusManager", "GetFocus")]
	public sealed partial class FocusManager_GetFocus_Automated : UserControl
	{
		public FocusManager_GetFocus_Automated()
		{
			this.InitializeComponent();

			var myTimer = new DispatcherTimer();
			myTimer.Interval = TimeSpan.FromSeconds(1);
			myTimer.Tick += UpdateFocusedElement;
			myTimer.Start();
		}

		private void UpdateFocusedElement(object sender, object e)
		{
			var myElement = Windows.UI.Xaml.Input.FocusManager.GetFocusedElement();
			var myFrameworkElement = myElement as FrameworkElement;
			var elementName = myFrameworkElement?.Name ?? "";

			this.TxtCurrentFocused.Text = elementName;
		}
	}
}
