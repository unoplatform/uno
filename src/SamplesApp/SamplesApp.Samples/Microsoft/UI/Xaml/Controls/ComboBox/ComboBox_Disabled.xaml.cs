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
	[Sample("ComboBox", Name = "ComboBox_Disabled", ViewModelType = typeof(ListViewViewModel), Description = "ComboBox should be disabled by default.")]
	public sealed partial class ComboBox_Disabled : UserControl
	{
		public ComboBox_Disabled()
		{
			this.InitializeComponent();
		}
	}
}
