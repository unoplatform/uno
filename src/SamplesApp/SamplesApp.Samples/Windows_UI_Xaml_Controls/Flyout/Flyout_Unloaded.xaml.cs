using System;
using System.Collections.Generic;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.Flyout
{
	[Sample("Flyouts", Name = "Flyout_Unloaded")]
	public sealed partial class Flyout_Unloaded : UserControl
	{
		public Flyout_Unloaded()
		{
			this.InitializeComponent();
		}

		private void OnUnloadParent(object sender, object args)
		{
			outerPanel.Children.Remove(outerButton);
		}
	}
}
