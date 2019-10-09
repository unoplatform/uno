using System;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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
