using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml_Controls.SplitView
{
	[SampleControlInfo("SplitView")]
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
