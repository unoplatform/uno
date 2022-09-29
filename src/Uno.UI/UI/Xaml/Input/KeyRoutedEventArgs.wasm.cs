#nullable disable

using Uno.UI.Xaml.Input;

namespace Windows.UI.Xaml.Input
{
	public partial class KeyRoutedEventArgs : IPreventDefaultHandling
	{
		bool IPreventDefaultHandling.DoNotPreventDefault { get; set; }
	}
}
