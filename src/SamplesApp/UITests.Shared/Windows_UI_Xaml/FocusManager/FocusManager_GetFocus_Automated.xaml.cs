using System;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.FocusTests
{
	[Sample("Focus", Name = "GetFocus")]
	public sealed partial class FocusManager_GetFocus_Automated : UserControl
	{
		public FocusManager_GetFocus_Automated()
		{
			this.InitializeComponent();

			Microsoft.UI.Xaml.Input.FocusManager.GotFocus += FocusManager_GotFocus;
		}

		private void FocusManager_GotFocus(object sender, Microsoft.UI.Xaml.Input.FocusManagerGotFocusEventArgs e)
		{
			this.TxtCurrentFocused.Text = (e.NewFocusedElement as FrameworkElement)?.Name ?? "<none>";
		}
	}
}
