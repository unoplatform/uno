using System;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ComboBox
{
	[SampleControlInfo("ComboBox")]
	public sealed partial class ComboBox_DropDownPlacement : Page
	{
		public ComboBox_DropDownPlacement()
		{
			this.InitializeComponent();

#if __ANDROID__
			// When we open a PopupWindow on Anbdroid, it will display the status bar
			// As this test heavily uses PopupWindow, it's easier to always make it visible
			// and then make sure to compare only well-known parts of the screen.
			var statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
			Windows.UI.Xaml.Window.Current.Dispatcher.RunAsync(
				Windows.UI.Core.CoreDispatcherPriority.Normal,
				async () =>
				{
					await statusBar.ShowAsync();
				});
#endif
		}
	}
}
