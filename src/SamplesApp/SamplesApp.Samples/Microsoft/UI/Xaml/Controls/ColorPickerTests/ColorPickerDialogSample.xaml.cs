using System;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace UITests.Microsoft_UI_Xaml_Controls.ColorPickerTests
{
	[Sample("ColorPicker", "MUX", Name = "ColorPickerDialogSample")]
	public sealed partial class ColorPickerDialogSample : UserControl
	{
		public ColorPickerDialogSample()
		{
			this.InitializeComponent();
		}

		private async void ShowDialogClick(object sender, RoutedEventArgs args)
		{
			ColorPickerDialog colorPickerDialog = new ColorPickerDialog();
			colorPickerDialog.XamlRoot = this.XamlRoot;
			await colorPickerDialog.ShowAsync();
		}
	}
}
