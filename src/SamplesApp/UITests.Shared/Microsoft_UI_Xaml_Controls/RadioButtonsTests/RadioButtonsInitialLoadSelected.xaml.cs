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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Microsoft_UI_Xaml_Controls.RadioButtonsTests
{
	[Sample("Buttons", "MUX")]
	public sealed partial class RadioButtonsInitialLoadSelected : Page
	{
		public RadioButtonsInitialLoadSelected()
		{
			this.InitializeComponent();

			ThemeRadioButtons.SelectedIndex = 0;
		}
	}
}
