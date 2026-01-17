using System;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Private.Infrastructure;

namespace Uno.UI.Samples.Content.UITests.TextBoxControl
{
	[Sample(category: "TextBox")]
	public sealed partial class PasswordBox_Reveal_Scroll : Page
	{
		public PasswordBox_Reveal_Scroll()
		{
			this.InitializeComponent();
#if __ANDROID__
			// When we open yhe keyboard on Anbdroid, it will display the status bar
			// As this test focus the password, it's easier to always make it visible
			// and then make sure to compare only well-known parts of the screen.
			var statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
			_ = UnitTestDispatcherCompat.From(this).RunAsync(
				UnitTestDispatcherCompat.Priority.Normal,
				async () =>
				{
					await statusBar.ShowAsync();
				});
#endif
		}
	}
}
