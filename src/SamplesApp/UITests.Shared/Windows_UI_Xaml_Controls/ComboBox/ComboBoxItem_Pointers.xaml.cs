using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.ComboBox
{
	[Sample("ComboBox", "Pointers",
		IgnoreInSnapshotTests = true,
		Description = "The PointerOver visual state shouldn't be delayed")]
	public sealed partial class ComboBoxItem_Pointers : Page
	{
		public ComboBoxItem_Pointers()
		{
			this.InitializeComponent();
		}
	}
}
