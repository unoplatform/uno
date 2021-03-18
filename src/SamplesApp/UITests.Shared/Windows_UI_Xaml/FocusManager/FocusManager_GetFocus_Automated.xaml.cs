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

			Windows.UI.Xaml.Input.FocusManager.GotFocus += FocusManager_GotFocus;
		}

		private void FocusManager_GotFocus(object sender, Windows.UI.Xaml.Input.FocusManagerGotFocusEventArgs e)
		{
			this.TxtCurrentFocused.Text = (e.NewFocusedElement as FrameworkElement)?.Name ?? "<none>";
		}
	}
}
