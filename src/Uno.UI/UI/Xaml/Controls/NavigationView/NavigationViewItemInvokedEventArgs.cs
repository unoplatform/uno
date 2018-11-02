#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	public  partial class NavigationViewItemInvokedEventArgs 
	{
		public object InvokedItem { get; internal set; }

		public  bool IsSettingsInvoked { get; internal set; }

		public NavigationViewItemInvokedEventArgs() 
		{
		}
	}
}
