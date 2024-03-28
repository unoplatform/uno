using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Input.PointersTests;

[Sample("Pointers", "Nested_Exit")]
public sealed partial class Nested_Exit : Page
{
	private bool _case1_exitedBlue;

	public Nested_Exit()
	{
		this.InitializeComponent();
	}

	private void Case1_InitTest(object sender, RoutedEventArgs e)
	{
		Case1_out.Text = "--init--";
		_case1_exitedBlue = false;
	}

	private void Case1_OnPointerExited(object sender, PointerRoutedEventArgs e)
	{
		switch ((sender as FrameworkElement)?.Name)
		{
			case "Case1_Blue":
				_case1_exitedBlue = true;
				break;

			case "Case1_Pink" when _case1_exitedBlue:
				Case1_out.Text = "Success";
				break;

			case "Case1_Pink":
				Case1_out.Text = "Failed"; // Note: common failure case is to remain in "--init--" !
				break;
		}
	}
}
