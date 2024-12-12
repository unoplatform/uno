#region Assembly Windows.Foundation.UniversalApiContract, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null, ContentType=WindowsRuntime
// C:\Program Files (x86)\Windows Kits\10\References\Windows.Foundation.UniversalApiContract\2.0.0.0\Windows.Foundation.UniversalApiContract.winmd
#endregion

using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.UI.Xaml.Controls
{
	public sealed partial class ItemClickEventArgs : RoutedEventArgs
	{
		public ItemClickEventArgs() { }

		public object ClickedItem { get; set; }
	}
}
