using System;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Uno.UI.Samples.Content.UITests.FocusTests
{
	[Sample("Focus", Name = "GetFocus")]
	public sealed partial class FocusManager_GetFocus_Automated : UserControl
	{
		public FocusManager_GetFocus_Automated()
		{
			this.InitializeComponent();

			this.Loaded += (_, _) => FocusManager.GotFocus += FocusManager_GotFocus;
			this.Unloaded += (_, _) => FocusManager.GotFocus -= FocusManager_GotFocus;
		}

		private void FocusManager_GotFocus(object sender, FocusManagerGotFocusEventArgs e)
		{
			this.TxtCurrentFocused.Text = (e.NewFocusedElement as FrameworkElement)?.Name ?? "<none>";
		}
	}
}
