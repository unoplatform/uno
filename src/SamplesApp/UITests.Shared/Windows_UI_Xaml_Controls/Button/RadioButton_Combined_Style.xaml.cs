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

namespace UITests.Shared.Windows_UI_Xaml_Controls.Button
{
	[SampleControlInfo("Buttons", "RadioButton_Combined_Style", description: "RadioButtons where checked and pressed states are in the same VisualStateGroup. The buttons should always read Checked or Unchecked.")]
	public sealed partial class RadioButton_Combined_Style : UserControl
	{
		public RadioButton_Combined_Style()
		{
			this.InitializeComponent();
		}
	}
}
