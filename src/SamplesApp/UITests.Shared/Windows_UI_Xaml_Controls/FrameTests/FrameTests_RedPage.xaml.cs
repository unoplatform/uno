using System;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace UITests.Windows_UI_Xaml_Controls.FrameTests;

public sealed partial class FrameTests_RedPage : Page
{
	public FrameTests_RedPage()
	{
		this.InitializeComponent();
	}

	/// <inheritdoc />
	protected override void OnTapped(TappedRoutedEventArgs e)
		=> e.Handled = true;
}
