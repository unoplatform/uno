using SamplesApp.Samples.NavigationViewSample;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace SamplesApp.Samples.Windows_UI_Xaml_Controls.NavigationViewTests
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	[Sample("NavigationView", Name = "NavigationView_BasicNavigation")]
	public sealed partial class NavigationView_BasicNavigation : Page
	{
		public NavigationView_BasicNavigation()
		{
			this.InitializeComponent();
		}

		private void BasicNavigation_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
		{
			if (args.IsSettingsInvoked)
			{
				contentFrame.Navigate(typeof(SettingsPage));
			}
			else if (args.InvokedItemContainer is NavigationViewItem item)
			{
				switch (item.Tag)
				{
					case "First":
						contentFrame.Navigate(typeof(Item1Page));
						break;
					case "Second":
						contentFrame.Navigate(typeof(Item2Page));
						break;
				}
			}
		}

		private void BasicNavigation_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
		{
			contentFrame.GoBack();
		}
	}
}
