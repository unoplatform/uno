using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml_Controls.SplitView
{
	[Sample("SplitView")]
	public sealed partial class SplitViewClip : Page
	{
		public SplitViewClip()
		{
			this.InitializeComponent();
		}

		public void Button_Click(object sender, RoutedEventArgs args)
		{
			Split.IsPaneOpen = !Split.IsPaneOpen;
		}
	}
}
