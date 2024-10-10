using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.UI.Samples.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Microsoft_UI_Xaml_Controls.ProgressRing
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	[Sample("Progress", "MUX")]
	public sealed partial class WinUIDeterminateProgressRing : Page
	{
		public WinUIDeterminateProgressRing()
		{
			this.InitializeComponent();
		}

		private void ProgressValue_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ProgressRing is { })
			{
				ProgressRing.Value = double.Parse(ProgressValue.SelectedValue as string);
			}
		}
	}
}
