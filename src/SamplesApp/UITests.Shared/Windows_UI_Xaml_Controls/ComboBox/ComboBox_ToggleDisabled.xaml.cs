using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using SamplesApp.Windows_UI_Xaml_Controls.Models;
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

namespace UITests.Windows_UI_Xaml_Controls.ComboBox
{
	[Sample("ComboBox", Name = "ComboBox_ToggleDisabled", Description = "Toggling button to disable the ComboBox should disable it.")]
	public sealed partial class ComboBox_ToggleDisabled : UserControl
	{
		public ComboBox_ToggleDisabled()
		{
			this.InitializeComponent();
		}


		private void ToggleDisabled(object sender, RoutedEventArgs args)
		{
			var currentlyDisabled = DisablingComboBox.IsEnabled;
			DisablingComboBox.IsEnabled = !currentlyDisabled;
		}
	}
}
