using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using SamplesApp.Wasm.Windows_UI_Xaml_Controls.ComboBox;
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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.CheckBoxTests
{
	[Sample("Buttons")]
	public sealed partial class CheckBox_Automated : UserControl
	{
		public CheckBox_Automated()
		{
			this.InitializeComponent();
		}

		private void OnChecked(object sender, object args)
		{
			if (sender is CheckBox cb)
			{
				result.Text = $"Checked {cb.Name} {cb.IsChecked}";
			}
		}

		private void OnUnchecked(object sender, object args)
		{
			if (sender is CheckBox cb)
			{
				result.Text = $"Unchecked {cb.Name} {cb.IsChecked}";
			}
		}

		private void OnIndeterminate(object sender, object args)
		{
			if (sender is CheckBox cb)
			{
				result.Text = $"Indeterminate {cb.Name} {cb.IsChecked}";
			}
		}
	}
}
