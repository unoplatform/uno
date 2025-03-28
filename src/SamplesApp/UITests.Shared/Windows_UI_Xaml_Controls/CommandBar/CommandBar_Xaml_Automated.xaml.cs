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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.CommandBar
{
	[SampleControlInfo("CommandBar", nameof(CommandBar_Xaml_Automated))]
	public sealed partial class CommandBar_Xaml_Automated : UserControl
	{
		public CommandBar_Xaml_Automated()
		{
			this.InitializeComponent();
		}

		public void Button_Click(object sender, object args)
		{
			if (sender is AppBarButton abb)
			{
				clickResult.Text = abb.Label;
			}
			else if (sender is AppBarToggleButton abtb)
			{
				clickResult.Text = abtb.Label;
			}
			else if (sender is Windows.UI.Xaml.Controls.Button b)
			{
				clickResult.Text = b.Content?.ToString();
			}
		}
	}
}
