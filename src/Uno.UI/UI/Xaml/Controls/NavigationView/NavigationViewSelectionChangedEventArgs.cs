#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	public  partial class NavigationViewSelectionChangedEventArgs 
	{
		public  bool IsSettingsSelected
		{
			get; internal set;
		}

		public  object SelectedItem
		{
			get; internal set;
		}
	}
}
