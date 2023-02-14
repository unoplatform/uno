using Uno.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Input
{
	public partial class KeyRoutedEventArgs : IPreventDefaultHandling
	{
		bool IPreventDefaultHandling.DoNotPreventDefault { get; set; }
	}
}
